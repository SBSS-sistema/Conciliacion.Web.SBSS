using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory _factory;

    public DashboardRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<DashboardResumenVm> ObtenerResumenAsync()
    {
        using var conn = _factory.CreateConnection();
        var resumen = await conn.QueryFirstOrDefaultProcAsync<DashboardResumenVm>("sp_Dashboard_ResumenGeneral");
        return resumen ?? new DashboardResumenVm();
    }

    public async Task<IEnumerable<ExpedientesPorMesVm>> ObtenerPorMesAsync(int anio)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<ExpedientesPorMesVm>("sp_Dashboard_ExpedientesPorMes", new { anio });
    }

    public async Task<IEnumerable<ExpedientesPorConciliadorVm>> ObtenerPorConciliadorAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<ExpedientesPorConciliadorVm>("sp_Dashboard_ExpedientesPorConciliador");
    }

    public async Task<IEnumerable<TasaExitoVm>> ObtenerTasaExitoAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<TasaExitoVm>("sp_Dashboard_TasaExitoConciliacion");
    }
}
