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
}
