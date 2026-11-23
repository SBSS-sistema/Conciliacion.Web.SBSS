using System.Data;
using Dapper;
using Npgsql;
using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;
using SBSS.Conciliacion.Web.Services;

namespace SBSS.Conciliacion.Web.Repositories;

public class CitaRepository : ICitaRepository
{
    private readonly IDbConnectionFactory _factory;

    public CitaRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<long> CrearAsync(CrearCitaVm vm, int idUsuarioCreador)
    {
        using var conn = _factory.CreateConnection();
        const string sql = """
            INSERT INTO public.cita
                (id_persona, id_materia, id_sede, nombre_cliente, apellido_paterno, apellido_materno, dni_cliente,
                 telefono, correo, motivo, id_conciliador, id_usuario_creador, fecha_hora, observaciones)
            VALUES
                (@IdPersona, @IdMateria, @IdSede, @NombreCliente, @ApellidoPaterno, @ApellidoMaterno, @DniCliente,
                 @Telefono, @Correo, @Motivo, @IdConciliador, @IdUsuarioCreador, @FechaHora, @Observaciones)
            RETURNING idcita;
            """;
        return await conn.ExecuteScalarAsync<long>(sql, new
        {
            vm.IdPersona, vm.IdMateria, vm.IdSede, vm.NombreCliente, vm.ApellidoPaterno, vm.ApellidoMaterno,
            vm.DniCliente, vm.Telefono, vm.Correo, vm.Motivo, vm.IdConciliador,
            IdUsuarioCreador = idUsuarioCreador,
            FechaHora = DateTime.SpecifyKind(vm.FechaHora, DateTimeKind.Unspecified),
            vm.Observaciones
        });
    }

    public async Task<IEnumerable<CitaListItemVm>> ListarAsync(int? idConciliador = null)
    {
        using var conn = _factory.CreateConnection();
        const string sql = """
            SELECT c.idcita AS IdCita, c.nombre_cliente AS NombreCliente,
                   c.apellido_paterno AS ApellidoPaterno, c.apellido_materno AS ApellidoMaterno,
                   c.dni_cliente AS DniCliente, c.telefono AS Telefono, c.fecha_hora AS FechaHora,
                   c.estado AS Estado, c.motivo AS Motivo, c.id_conciliador AS IdConciliador,
                   c.id_expediente AS IdExpediente,
                   concat_ws(' ', t.nombretrabajador, t.apepatertrabajador) AS Conciliador
            FROM public.cita c
            JOIN public.trabajador t ON t.idtrabajador = c.id_conciliador
            WHERE (@IdConciliador IS NULL OR c.id_conciliador = @IdConciliador)
            ORDER BY c.fecha_hora DESC, c.idcita DESC;
            """;
        return await conn.QueryAsync<CitaListItemVm>(sql, new { IdConciliador = idConciliador });
    }

    public async Task<IEnumerable<CitaListItemVm>> ListarPorRangoAsync(DateTime desde, DateTime hasta, int? idConciliador = null)
    {
        using var conn = _factory.CreateConnection();
        const string sql = """
            SELECT c.idcita AS IdCita, c.nombre_cliente AS NombreCliente,
                   c.apellido_paterno AS ApellidoPaterno, c.apellido_materno AS ApellidoMaterno,
                   c.dni_cliente AS DniCliente, c.telefono AS Telefono, c.fecha_hora AS FechaHora,
                   c.estado AS Estado, c.motivo AS Motivo, c.id_conciliador AS IdConciliador,
                   c.id_expediente AS IdExpediente,
                   concat_ws(' ', t.nombretrabajador, t.apepatertrabajador) AS Conciliador
            FROM public.cita c JOIN public.trabajador t ON t.idtrabajador = c.id_conciliador
            WHERE c.fecha_hora >= @Desde AND c.fecha_hora < @Hasta
              AND (@IdConciliador IS NULL OR c.id_conciliador = @IdConciliador)
            ORDER BY c.fecha_hora;
            """;
        return await conn.QueryAsync<CitaListItemVm>(sql, new
        {
            Desde = DateTime.SpecifyKind(desde, DateTimeKind.Unspecified),
            Hasta = DateTime.SpecifyKind(hasta, DateTimeKind.Unspecified),
            IdConciliador = idConciliador
        });
    }

    public async Task<IEnumerable<Trabajador>> ListarConciliadoresAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<Trabajador>("""
            SELECT DISTINCT t.idtrabajador AS IdTrabajador, t.idsederef AS IdSedeRef,
                   t.dnitrabajador AS DniTrabajador, t.nombretrabajador AS NombreTrabajador,
                   t.apepatertrabajador AS ApePaterTrabajador, t.apematertrabajador AS ApeMaterTrabajador,
                   t.emailtrabajador AS EmailTrabajador, t.telefonotrabajador AS TelefonoTrabajador,
                   t.estadotrabajador AS EstadoTrabajador
            FROM public.trabajador t JOIN public.usuario u ON u.idtrabajador = t.idtrabajador
            WHERE upper(coalesce(t.estadotrabajador, '')) = 'ACTIVO'
              AND upper(coalesce(u.rol, '')) = 'CONCILIADOR'
            ORDER BY t.nombretrabajador, t.apepatertrabajador;
            """);
    }

    public async Task<Cita?> ObtenerPorIdAsync(long idCita)
    {
        using var conn = _factory.CreateConnection();
        const string sql = """
            SELECT c.idcita AS IdCita, c.id_persona AS IdPersona, c.id_materia AS IdMateria,
                   c.id_sede AS IdSede, c.nombre_cliente AS NombreCliente,
                   c.apellido_paterno AS ApellidoPaterno, c.apellido_materno AS ApellidoMaterno,
                   c.dni_cliente AS DniCliente, c.telefono AS Telefono, c.correo AS Correo,
                   c.motivo AS Motivo, c.id_conciliador AS IdConciliador,
                   c.id_usuario_creador AS IdUsuarioCreador, c.fecha_hora AS FechaHora,
                   c.estado AS Estado, c.observaciones AS Observaciones,
                   c.id_expediente AS IdExpediente, m.nombremateria AS NombreMateria, s.nombresede AS NombreSede,
                   concat_ws(' ', t.nombretrabajador, t.apepatertrabajador) AS Conciliador
            FROM public.cita c
            JOIN public.trabajador t ON t.idtrabajador = c.id_conciliador
            LEFT JOIN public.materia m ON m.idmateria = c.id_materia
            LEFT JOIN public.sede s ON s.idsede = c.id_sede
            WHERE c.idcita = @IdCita;
            """;
        try
        {
            return await conn.QuerySingleOrDefaultAsync<Cita>(sql, new { IdCita = idCita });
        }
        catch (PostgresException ex) when (ex.SqlState is "42703" or "42P01")
        {
            // Compatibilidad temporal para citas antiguas mientras se aplica la migración 09.
            const string sqlAnterior = """
                SELECT c.idcita AS IdCita, c.nombre_cliente AS NombreCliente,
                       c.apellido_paterno AS ApellidoPaterno, c.apellido_materno AS ApellidoMaterno,
                       c.dni_cliente AS DniCliente, c.telefono AS Telefono, c.correo AS Correo,
                       c.motivo AS Motivo, c.id_conciliador AS IdConciliador,
                       c.id_usuario_creador AS IdUsuarioCreador, c.fecha_hora AS FechaHora,
                       c.estado AS Estado, c.observaciones AS Observaciones,
                       c.id_expediente AS IdExpediente,
                       concat_ws(' ', t.nombretrabajador, t.apepatertrabajador) AS Conciliador
                FROM public.cita c
                JOIN public.trabajador t ON t.idtrabajador = c.id_conciliador
                WHERE c.idcita = @IdCita;
                """;
            return await conn.QuerySingleOrDefaultAsync<Cita>(sqlAnterior, new { IdCita = idCita });
        }
    }

    public async Task ActualizarAgendaAsync(long idCita, DateTime fechaHora, string? observaciones, string estado)
    {
        using var conn = _factory.CreateConnection();
        const string sql = """
            UPDATE public.cita
               SET fecha_hora = @FechaHora, observaciones = @Observaciones,
                   estado = @Estado, actualizado_en = CURRENT_TIMESTAMP
             WHERE idcita = @IdCita AND id_expediente IS NULL
               AND estado IN ('PROGRAMADA', 'REPROGRAMADA');
            """;
        var n = await conn.ExecuteAsync(sql, new
        {
            IdCita = idCita,
            FechaHora = DateTime.SpecifyKind(fechaHora, DateTimeKind.Unspecified),
            Observaciones = observaciones,
            Estado = estado
        });
        if (n == 0) throw new InvalidOperationException("La cita ya no se puede modificar.");
    }

    public async Task RegistrarAtencionAsync(long idCita, string estado, string? observaciones)
    {
        using var conn = _factory.CreateConnection();
        const string sql = """
            UPDATE public.cita
               SET estado = @Estado, observaciones = @Observaciones,
                   actualizado_en = CURRENT_TIMESTAMP
             WHERE idcita = @IdCita AND id_expediente IS NULL
               AND estado IN ('PROGRAMADA', 'REPROGRAMADA', 'ATENDIDA', 'NO_ASISTIO');
            """;
        var n = await conn.ExecuteAsync(sql, new { IdCita = idCita, Estado = estado, Observaciones = observaciones });
        if (n == 0) throw new InvalidOperationException("La cita ya no se puede actualizar.");
    }

    public async Task<int> ConvertirEnExpedienteAsync(long idCita, int idConciliador, string numeroExpediente, int plazoDias)
    {
        using var conn = _factory.CreateConnection();
        if (conn is not NpgsqlConnection pg)
            throw new InvalidOperationException("La conversión de cita requiere la conexión PostgreSQL configurada.");

        await pg.OpenAsync();
        await using var tx = await pg.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            const string citaSql = """
                SELECT idcita AS IdCita, id_persona AS IdPersona, id_materia AS IdMateria,
                       id_sede AS IdSede, nombre_cliente AS NombreCliente,
                       apellido_paterno AS ApellidoPaterno, apellido_materno AS ApellidoMaterno,
                       dni_cliente AS DniCliente, telefono AS Telefono, correo AS Correo,
                       motivo AS Motivo, id_conciliador AS IdConciliador, estado AS Estado,
                       id_expediente AS IdExpediente
                FROM public.cita WHERE idcita = @IdCita FOR UPDATE;
                """;
            var cita = await pg.QuerySingleOrDefaultAsync<Cita>(citaSql, new { IdCita = idCita }, tx);
            if (cita is null || cita.IdConciliador != idConciliador)
                throw new InvalidOperationException("No tienes acceso a esta cita.");
            if (cita.Estado != "ATENDIDA" || cita.IdExpediente.HasValue)
                throw new InvalidOperationException("Solo se puede convertir una cita atendida y aún no convertida.");

            int? idSede = cita.IdSede;
            if (!idSede.HasValue)
            {
                idSede = await pg.ExecuteScalarAsync<int?>(
                    "SELECT idsederef FROM public.trabajador WHERE idtrabajador = @IdTrabajador AND upper(estadotrabajador) = 'ACTIVO';",
                    new { IdTrabajador = idConciliador }, tx);
            }
            if (!idSede.HasValue)
                throw new InvalidOperationException("El conciliador no tiene una sede activa asignada.");

            int? idPersona = cita.IdPersona;
            if (!idPersona.HasValue)
            {
                idPersona = await pg.QueryFirstOrDefaultAsync<int?>("""
                    SELECT idpersonanatural FROM public."natural" WHERE dnipersonan = @Dni;
                    """, new { Dni = cita.DniCliente }, tx);
            }
            if (!idPersona.HasValue)
            {
                idPersona = await pg.QuerySingleAsync<int>("""
                    SELECT * FROM sp_personanatural_insertar(
                        p_idsede => @idSede, p_celularp => @telefono, p_correop => @correo,
                        p_whatsappp => @telefono, p_dnipersonan => @dni,
                        p_nombrepersonan => @nombre, p_apellidopaternopersonan => @apellidoPaterno,
                        p_apellidomaternopersonan => @apellidoMaterno)
                    """, new
                {
                    idSede, telefono = cita.Telefono, correo = cita.Correo,
                    dni = cita.DniCliente, nombre = cita.NombreCliente,
                    apellidoPaterno = cita.ApellidoPaterno, apellidoMaterno = cita.ApellidoMaterno
                }, tx);
            }

            var idExpediente = await pg.QuerySingleAsync<int>("""
                SELECT * FROM sp_expediente_insertar(
                    p_numeroexpediente => @numeroExpediente, p_idtrabajador => @idTrabajador,
                    p_idsede => @idSede, p_fecharecepcion => @fechaRecepcion, p_plazodias => @plazoDias)
                """, new
            {
                numeroExpediente, idTrabajador = idConciliador, idSede,
                fechaRecepcion = DateTime.SpecifyKind(HoraLocal.FechaPeru, DateTimeKind.Unspecified), plazoDias
            }, tx);

            await pg.ExecuteAsync("""
                UPDATE public.expediente
                   SET idconciliador = @IdConciliador, estadoproceso = 'ASIGNADO'
                 WHERE idexpediente = @IdExpediente;
                """, new { IdConciliador = idConciliador, IdExpediente = idExpediente }, tx);

            await pg.ExecuteAsync("""
                SELECT * FROM sp_detallepersonaexpediente_agregar(
                    p_idexpediente => @idExpediente, p_idpersona => @idPersona,
                    p_rolparticipante => 'SOLICITANTE')
                """, new { idExpediente, idPersona }, tx);

            if (cita.IdMateria.HasValue)
            {
                await pg.ExecuteAsync("""
                    SELECT * FROM sp_detallemateriaexpediente_agregar(
                        p_idexpediente => @idExpediente, p_idmateria => @idMateria,
                        p_descripciondtmateria => @descripcion)
                    """, new { idExpediente, idMateria = cita.IdMateria, descripcion = cita.Motivo }, tx);
            }

            var updated = await pg.ExecuteAsync("""
                UPDATE public.cita SET estado = 'CONVERTIDA', id_expediente = @IdExpediente,
                       actualizado_en = CURRENT_TIMESTAMP
                 WHERE idcita = @IdCita AND id_expediente IS NULL;
                """, new { IdCita = idCita, IdExpediente = idExpediente }, tx);
            if (updated != 1) throw new InvalidOperationException("La cita ya fue convertida en expediente.");

            await tx.CommitAsync();
            return idExpediente;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
