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
                fechaRecepcion = vm.FechaRecepcion,
                plazoDias = vm.PlazoDias
            });
    }

    public async Task<int> AsignarConciliadorAleatorioAsync(int idExpediente)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>("sp_Expediente_AsignarConciliadorAleatorio", new { idExpediente });
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
