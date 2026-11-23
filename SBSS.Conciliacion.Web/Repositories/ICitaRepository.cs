using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public interface ICitaRepository
{
    Task<long> CrearAsync(CrearCitaVm vm, int idUsuarioCreador);
    Task<IEnumerable<CitaListItemVm>> ListarAsync(int? idConciliador = null);
    Task<IEnumerable<CitaListItemVm>> ListarPorRangoAsync(DateTime desde, DateTime hasta, int? idConciliador = null);
    Task<IEnumerable<Trabajador>> ListarConciliadoresAsync();
    Task<Cita?> ObtenerPorIdAsync(long idCita);
    Task ActualizarAgendaAsync(long idCita, DateTime fechaHora, string? observaciones, string estado);
    Task RegistrarAtencionAsync(long idCita, string estado, string? observaciones);
    Task<int> ConvertirEnExpedienteAsync(long idCita, int idConciliador, string numeroExpediente, int plazoDias);
}
