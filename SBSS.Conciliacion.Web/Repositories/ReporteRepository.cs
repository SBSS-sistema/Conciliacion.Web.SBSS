using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public class ReporteRepository : IReporteRepository
{
    private readonly IDbConnectionFactory _factory;

    public ReporteRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<ReporteExpedienteVm>> ObtenerPorRangoAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<ReporteExpedienteVm>("sp_Reporte_ExpedientesPorRango", new { fechaInicio, fechaFin });
    }

    public async Task<IEnumerable<ReporteMateriaVm>> ObtenerPorMateriaAsync(int? idTipoMateria)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<ReporteMateriaVm>("sp_Reporte_ExpedientesPorMateria", new { idTipoMateria });
    }
}
