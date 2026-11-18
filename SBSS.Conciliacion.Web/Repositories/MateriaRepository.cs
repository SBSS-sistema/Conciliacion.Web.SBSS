using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public class MateriaRepository : IMateriaRepository
{
    private readonly IDbConnectionFactory _factory;

    public MateriaRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> InsertarTipoAsync(CrearTipoMateriaVm vm)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>(
            "sp_TipoMateria_Insertar",
            new { nombreTipoMateria = vm.NombreTipoMateria, descripcionTipoMateria = vm.DescripcionTipoMateria });
    }

    public async Task<IEnumerable<TipoMateria>> ListarTiposAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<TipoMateria>("sp_TipoMateria_Listar");
    }

    public async Task<int> InsertarMateriaAsync(CrearMateriaVm vm)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>(
            "sp_Materia_Insertar",
            new
            {
                idTipoMateria = vm.IdTipoMateria,
                nombreMateria = vm.NombreMateria,
                descripcionMateria = vm.DescripcionMateria
            });
    }

    public async Task ActualizarMateriaAsync(EditarMateriaVm vm)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync(
            "sp_Materia_Actualizar",
            new
            {
                idMateria = vm.IdMateria,
                idTipoMateria = vm.IdTipoMateria,
                nombreMateria = vm.NombreMateria,
                descripcionMateria = vm.DescripcionMateria
            });
    }

    public async Task<Materia?> ObtenerMateriaPorIdAsync(int idMateria)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultProcAsync<Materia>("sp_Materia_ObtenerPorId", new { idMateria });
    }

    public async Task<IEnumerable<Materia>> ListarMateriasAsync(int? idTipoMateria = null)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<Materia>("sp_Materia_Listar", new { idTipoMateria });
    }

    public async Task<bool> EliminarMateriaAsync(int id)
    {
        using var connection = _factory.CreateConnection();
        // La función devuelve (respuesta, mensaje); la primera columna es el bool.
        return await connection.ScalarProcAsync<bool>("sp_EliminarMateria", new { idMateria = id });
    }
}
