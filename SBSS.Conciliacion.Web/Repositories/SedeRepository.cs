using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public class SedeRepository : ISedeRepository
{
    private readonly IDbConnectionFactory _factory;

    public SedeRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> InsertarAsync(CrearSedeVm vm)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>("sp_Sede_Insertar", new { nombre = vm.Nombre, descripcion = vm.Descripcion });
    }

    public async Task ActualizarAsync(EditarSedeVm vm)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync("sp_Sede_Actualizar", new { idSede = vm.IdSede, nombre = vm.Nombre, descripcion = vm.Descripcion });
    }

    public async Task CambiarEstadoAsync(int idSede, string estadoSede)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync("sp_Sede_CambiarEstado", new { idSede, estadoSede });
    }

    public async Task<Sede?> ObtenerPorIdAsync(int idSede)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultProcAsync<Sede>("sp_Sede_ObtenerPorId", new { idSede });
    }

    public async Task<IEnumerable<Sede>> ListarAsync(bool soloActivas = false)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<Sede>("sp_Sede_Listar", new { soloActivas });
    }
}
