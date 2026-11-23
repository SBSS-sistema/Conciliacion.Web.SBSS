using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public interface IExpedienteRepository
{
    Task<int> InsertarAsync(CrearExpedienteVm vm);

    /// <summary>Asigna al azar un trabajador activo como conciliador y devuelve su idTrabajador.</summary>
    Task<int> AsignarConciliadorAleatorioAsync(int idExpediente);
    Task<int> InsertarComoConciliadorAsync(CrearExpedienteVm vm, int idConciliador);
    Task CambiarEstadoAsync(int idExpediente, string nuevoEstado);
    Task TrasladarAsync(TrasladarExpedienteVm vm, int idTrabajadorActor, bool esAdministrador);
    Task<ExpedienteDetalleVm?> ObtenerPorIdAsync(int idExpediente);
    Task<IEnumerable<ExpedienteListItemVm>> ListarAsync(ExpedienteFiltroVm filtro);
    Task AgregarPersonaAsync(AgregarPersonaExpedienteVm vm);
    Task AgregarMateriaAsync(AgregarMateriaExpedienteVm vm);
    Task EliminarAsync(int idExpediente);
}

