using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public class UbigeoRepository : IUbigeoRepository
{
    private readonly IDbConnectionFactory _factory;

    public UbigeoRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<Departamento>> ListarDepartamentosAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<Departamento>("sp_Departamento_Listar");
    }

    public async Task<IEnumerable<Provincia>> ListarProvinciasAsync(int idDepartamento)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<Provincia>("sp_Provincia_ListarPorDepartamento", new { idDepartamento });
    }

    public async Task<IEnumerable<Distrito>> ListarDistritosAsync(int idProvincia)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<Distrito>("sp_Distrito_ListarPorProvincia", new { idProvincia });
    }

    public async Task<(int IdDepartamento, int IdProvincia, int IdDistrito)?> BuscarDistritoPorNombresAsync(
        string nombreDistrito, string nombreProvincia, string nombreDepartamento)
    {
        using var conn = _factory.CreateConnection();
        var fila = await conn.QueryFirstOrDefaultProcAsync<DistritoSugeridoRow>(
            "sp_Distrito_BuscarPorNombres",
            new { nombreDistrito, nombreProvincia, nombreDepartamento });

        if (fila is null) return null;

        return (fila.IdDepartamento, fila.IdProvincia, fila.IdDistrito);
    }

    private class DistritoSugeridoRow
    {
        public int IdDepartamento { get; set; }
        public int IdProvincia { get; set; }
        public int IdDistrito { get; set; }
    }
}
