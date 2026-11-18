using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public class NotificacionRepository : INotificacionRepository
{
    private readonly IDbConnectionFactory _factory;

    public NotificacionRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> CrearAsync(int idUsuario, int? idExpediente, string tipoNotificacion, string mensaje)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>(
            "sp_Notificacion_Crear",
            new { idUsuario, idExpediente, tipoNotificacion, mensaje });
    }

    public async Task MarcarLeidaAsync(int idNotificacion)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync("sp_Notificacion_MarcarLeida", new { idNotificacion });
    }

    public async Task MarcarTodasLeidasAsync(int idUsuario)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync("sp_Notificacion_MarcarTodasLeidas", new { idUsuario });
    }

    public async Task<IEnumerable<Notificacion>> ListarPorUsuarioAsync(int idUsuario, bool soloNoLeidas = false)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<Notificacion>("sp_Notificacion_ListarPorUsuario", new { idUsuario, soloNoLeidas });
    }

    public async Task<IEnumerable<Notificacion>> GenerarAlertasVencimientoAsync(int diasAnticipacion)
    {
        using var conn = _factory.CreateConnection();
        // La función devuelve las filas recién insertadas (INSERT ... RETURNING).
        return await conn.QueryProcAsync<Notificacion>("sp_Notificacion_GenerarAlertasVencimiento", new { diasAnticipacion });
    }
}
