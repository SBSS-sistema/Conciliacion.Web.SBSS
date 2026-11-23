using Dapper;
using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public class ExpedienteRepository : IExpedienteRepository
{
    private readonly IDbConnectionFactory _factory;

    public ExpedienteRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> InsertarAsync(CrearExpedienteVm vm)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>(
            "sp_Expediente_Insertar",
            new
            {
                numeroExpediente = vm.NumeroExpediente,
                idTrabajador = vm.IdTrabajador,
                idSede = vm.IdSede,
                fechaRecepcion = DateTime.SpecifyKind(vm.FechaRecepcion, DateTimeKind.Unspecified),
                plazoDias = vm.PlazoDias
            });
    }

    public async Task<int> InsertarComoConciliadorAsync(CrearExpedienteVm vm, int idConciliador)
    {
        using var conn = _factory.CreateConnection();
        using var transaction = conn.BeginTransaction();

        var idExpediente = await conn.ExecuteScalarAsync<int>(
            "SELECT * FROM public.sp_expediente_insertar(p_numeroexpediente => @numeroExpediente, p_idtrabajador => @idTrabajador, p_idsede => @idSede, p_fecharecepcion => @fechaRecepcion, p_plazodias => @plazoDias)",
            new
            {
                numeroExpediente = vm.NumeroExpediente,
                idTrabajador = vm.IdTrabajador,
                idSede = vm.IdSede,
                fechaRecepcion = vm.FechaRecepcion,
                plazoDias = vm.PlazoDias
            }, transaction);

        var filas = await conn.ExecuteAsync(
            "UPDATE public.expediente SET idconciliador = @idConciliador, estadoproceso = 'ASIGNADO' WHERE idexpediente = @idExpediente",
            new { idExpediente, idConciliador }, transaction);
        if (filas != 1)
            throw new InvalidOperationException("No se pudo asignar el expediente al conciliador actual.");

        transaction.Commit();
        return idExpediente;
    }

    public async Task<int> AsignarConciliadorAleatorioAsync(int idExpediente)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>("sp_Expediente_AsignarConciliadorAleatorio", new { idExpediente });
    }

    public async Task TrasladarAsync(TrasladarExpedienteVm vm, int idTrabajadorActor, bool esAdministrador)
    {
        using var conn = _factory.CreateConnection();
        using var transaction = conn.BeginTransaction();

        var actual = await conn.QuerySingleOrDefaultAsync<ExpedienteAsignacionActual>("""
            SELECT idsede AS IdSede, idconciliador AS IdConciliador
            FROM public.expediente
            WHERE idexpediente = @IdExpediente
            FOR UPDATE;
            """, new { vm.IdExpediente }, transaction);

        if (actual is null)
            throw new InvalidOperationException("El expediente no existe.");
        if (!esAdministrador && actual.IdConciliador != idTrabajadorActor)
            throw new UnauthorizedAccessException("Solo puedes trasladar un expediente que tienes asignado.");
        if (actual.IdSede == vm.IdSedeDestino && actual.IdConciliador == vm.IdConciliadorNuevo)
            throw new InvalidOperationException("El expediente ya está en esa sede con ese conciliador.");

        var destinoValido = await conn.ExecuteScalarAsync<bool>("""
            SELECT EXISTS (
                SELECT 1
                FROM public.trabajador t
                JOIN public.usuario u ON u.idtrabajador = t.idtrabajador
                WHERE t.idtrabajador = @IdConciliadorNuevo
                  AND t.idsedeRef = @IdSedeDestino
                  AND upper(coalesce(t.estadotrabajador, '')) = 'ACTIVO'
                  AND upper(trim(coalesce(u.rol, ''))) = 'CONCILIADOR'
            );
            """, new { vm.IdConciliadorNuevo, vm.IdSedeDestino }, transaction);

        if (!destinoValido)
            throw new InvalidOperationException("El conciliador seleccionado no está activo en la sede de destino.");

        var filas = await conn.ExecuteAsync("""
            UPDATE public.expediente
            SET idsede = @IdSedeDestino,
                idconciliador = @IdConciliadorNuevo,
                estadoproceso = CASE WHEN estadoproceso = 'REGISTRADO' THEN 'ASIGNADO' ELSE estadoproceso END
            WHERE idexpediente = @IdExpediente;
            """, new { vm.IdExpediente, vm.IdSedeDestino, vm.IdConciliadorNuevo }, transaction);

        if (filas != 1)
            throw new InvalidOperationException("No se pudo actualizar la sede y el conciliador del expediente.");

        await conn.ExecuteAsync("""
            INSERT INTO public.historial_traslado_expediente
                (idexpediente, idsede_origen, idsede_destino, idconciliador_anterior,
                 idconciliador_nuevo, idtrabajador_actor, motivo)
            VALUES
                (@IdExpediente, @IdSedeOrigen, @IdSedeDestino, @IdConciliadorAnterior,
                 @IdConciliadorNuevo, @IdTrabajadorActor, @Motivo);
            """, new
        {
            vm.IdExpediente,
            IdSedeOrigen = actual.IdSede,
            vm.IdSedeDestino,
            IdConciliadorAnterior = actual.IdConciliador,
            vm.IdConciliadorNuevo,
            IdTrabajadorActor = idTrabajadorActor,
            vm.Motivo
        }, transaction);

        transaction.Commit();
    }

    private sealed class ExpedienteAsignacionActual
    {
        public int? IdSede { get; set; }
        public int? IdConciliador { get; set; }
    }

    public async Task CambiarEstadoAsync(int idExpediente, string nuevoEstado)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync("sp_Expediente_CambiarEstado", new { idExpediente, nuevoEstado });
    }

    public async Task<ExpedienteDetalleVm?> ObtenerPorIdAsync(int idExpediente)
    {
        using var conn = _factory.CreateConnection();

        // En PostgreSQL el SP de 3 result sets se separó en 3 funciones.
        var expediente = await conn.QueryFirstOrDefaultProcAsync<Expediente>(
            "sp_Expediente_ObtenerPorId", new { idExpediente });
        if (expediente is null) return null;

        var personas = (await conn.QueryProcAsync<PersonaExpedienteVm>(
            "sp_Expediente_ObtenerParticipantes", new { idExpediente })).ToList();
        var materias = (await conn.QueryProcAsync<MateriaExpedienteVm>(
            "sp_Expediente_ObtenerMaterias", new { idExpediente })).ToList();

        return new ExpedienteDetalleVm
        {
            Expediente = expediente,
            Personas = personas,
            Materias = materias
        };
    }

    public async Task<IEnumerable<ExpedienteListItemVm>> ListarAsync(ExpedienteFiltroVm filtro)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<ExpedienteListItemVm>(
            "sp_Expediente_Listar",
            new
            {
                estado = filtro.Estado,
                idConciliador = filtro.IdConciliador,
                fechaInicio = filtro.FechaInicio,
                fechaFin = filtro.FechaFin,
                pagina = filtro.Pagina,
                tamanioPagina = filtro.TamanioPagina,
                idMateria = filtro.IdMateria
            });
    }

    public async Task AgregarPersonaAsync(AgregarPersonaExpedienteVm vm)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync(
            "sp_DetallePersonaExpediente_Agregar",
            new { idExpediente = vm.IdExpediente, idPersona = vm.IdPersona, rolParticipante = vm.RolParticipante });
    }

    public async Task AgregarMateriaAsync(AgregarMateriaExpedienteVm vm)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync(
            "sp_DetalleMateriaExpediente_Agregar",
            new { idExpediente = vm.IdExpediente, idMateria = vm.IdMateria, descripcionDtMateria = vm.DescripcionDtMateria });
    }

    public async Task EliminarAsync(int idExpediente)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync("sp_Expediente_Eliminar", new { idExpediente });
    }
}

