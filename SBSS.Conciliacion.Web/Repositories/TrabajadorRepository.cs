using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public class TrabajadorRepository : ITrabajadorRepository
{
    private readonly IDbConnectionFactory _factory;

    public TrabajadorRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> InsertarAsync(CrearTrabajadorVm vm)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>(
            "sp_Trabajador_Insertar",
            new
            {
                idSedeRef = vm.IdSedeRef,
                dniTrabajador = vm.DniTrabajador,
                nombreTrabajador = vm.NombreTrabajador,
                apePaterTrabajador = vm.ApePaterTrabajador,
                apeMaterTrabajador = vm.ApeMaterTrabajador,
                emailTrabajador = vm.EmailTrabajador,
                telefonoTrabajador = vm.TelefonoTrabajador,
                registroCivilComercial = vm.RegistroCivilComercial
            });
    }

    public async Task ActualizarAsync(EditarTrabajadorVm vm)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync(
            "sp_Trabajador_Actualizar",
            new
            {
                idTrabajador = vm.IdTrabajador,
                idSedeRef = vm.IdSedeRef,
                nombreTrabajador = vm.NombreTrabajador,
                apePaterTrabajador = vm.ApePaterTrabajador,
                apeMaterTrabajador = vm.ApeMaterTrabajador,
                emailTrabajador = vm.EmailTrabajador,
                telefonoTrabajador = vm.TelefonoTrabajador
            });
    }

    public async Task CambiarEstadoAsync(int idTrabajador, string estadoTrabajador)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync("sp_Trabajador_CambiarEstado", new { idTrabajador, estadoTrabajador });
    }

    public async Task<Trabajador?> ObtenerPorIdAsync(int idTrabajador)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultProcAsync<Trabajador>("sp_Trabajador_ObtenerPorId", new { idTrabajador });
    }

    public async Task<IEnumerable<Trabajador>> ListarAsync(int? idSedeRef = null, bool soloActivos = false)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<Trabajador>("sp_Trabajador_Listar", new { idSedeRef, soloActivos });
    }

    public async Task<IEnumerable<Trabajador>> ListarConciliadoresActivosPorSedeAsync(int idSede)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<Trabajador>("""
            SELECT DISTINCT t.idtrabajador AS IdTrabajador, t.idsedeRef AS IdSedeRef,
                   t.nombretrabajador AS NombreTrabajador, t.apepatertrabajador AS ApePaterTrabajador,
                   t.apematertrabajador AS ApeMaterTrabajador
            FROM public.trabajador t
            JOIN public.usuario u ON u.idtrabajador = t.idtrabajador
            JOIN public.sede s ON s.idsede = t.idsedeRef
            WHERE t.idsedeRef = @idSede
              AND upper(coalesce(t.estadotrabajador, '')) = 'ACTIVO'
              AND upper(coalesce(s.estadosede, '')) = 'ACTIVO'
              AND upper(trim(coalesce(u.rol, ''))) = 'CONCILIADOR'
            ORDER BY t.nombretrabajador, t.apepatertrabajador, t.apematertrabajador;
            """, new { idSede });
    }
}
