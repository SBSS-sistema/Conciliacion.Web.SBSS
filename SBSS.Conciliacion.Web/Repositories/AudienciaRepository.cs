using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public class AudienciaRepository : IAudienciaRepository
{
    private readonly IDbConnectionFactory _factory;

    public AudienciaRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> ProgramarAsync(ProgramarAudienciaVm vm)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>(
            "sp_Audiencia_Programar",
            new
            {
                idExpediente = vm.IdExpediente,
                idTipoDocumentos = vm.IdTipoDocumentos,
                idConciliador = vm.IdConciliador,
                fecha = vm.Fecha,
                hora = vm.Hora,
                numeroSesion = vm.NumeroSesion
            });
    }

    public async Task ReprogramarAsync(ReprogramarAudienciaVm vm)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync(
            "sp_Audiencia_Reprogramar",
            new
            {
                idDocumento = vm.IdDocumento,
                nuevaFecha = vm.NuevaFecha,
                nuevaHora = vm.NuevaHora,
                motivo = vm.Motivo
            });
    }

    public async Task RegistrarResultadoAsync(RegistrarResultadoVm vm)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync(
            "sp_Audiencia_RegistrarResultado",
            new
            {
                idDocumento = vm.IdDocumento,
                resultado = vm.Resultado,
                observaciones = vm.Observaciones
            });
    }

    public async Task<IEnumerable<AudienciaListItemVm>> ListarPorExpedienteAsync(int idExpediente)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<AudienciaListItemVm>("sp_Audiencia_ListarPorExpediente", new { idExpediente });
    }

    public async Task<AudienciaDetalleVm?> ObtenerPorIdAsync(int idDocumento)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultProcAsync<AudienciaDetalleVm>("sp_Audiencia_ObtenerPorId", new { idDocumento });
    }
}
