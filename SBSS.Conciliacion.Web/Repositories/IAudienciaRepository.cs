using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public interface IAudienciaRepository
{
    Task<int> ProgramarAsync(ProgramarAudienciaVm vm);
    Task ReprogramarAsync(ReprogramarAudienciaVm vm);
    Task RegistrarResultadoAsync(RegistrarResultadoVm vm);
    Task<IEnumerable<AudienciaListItemVm>> ListarPorExpedienteAsync(int idExpediente);
    Task<AudienciaDetalleVm?> ObtenerPorIdAsync(int idDocumento);

    // Calendario
    Task<IEnumerable<AudienciaCalendarioVm>> ListarPorRangoAsync(DateTime desde, DateTime hasta, int? idConciliador);
    Task<IEnumerable<VencimientoCalendarioVm>> ListarVencimientosPorRangoAsync(DateTime desde, DateTime hasta, int? idConciliador);
}
