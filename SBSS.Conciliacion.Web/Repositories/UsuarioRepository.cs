using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbConnectionFactory _factory;

    public UsuarioRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<UsuarioLogin?> ObtenerParaLoginAsync(string usuario)
    {
        using var conn = _factory.CreateConnection();
        // Solo trae el hash: la comparación se hace en la app (BCrypt).
        return await conn.QueryFirstOrDefaultProcAsync<UsuarioLogin>("sp_Usuario_ObtenerParaLogin", new { usuario });
    }

    public async Task<int?> ObtenerIdUsuarioPorTrabajadorAsync(int idTrabajador)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultProcAsync<int?>("sp_Usuario_ObtenerIdPorTrabajador", new { idTrabajador });
    }

    public async Task<int> InsertarAsync(int idTrabajador, string usuario, string contraseniaHash, string rol)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>("sp_Usuario_Insertar", new { idTrabajador, usuario, contraseniaHash, rol });
    }

    public async Task ActualizarContraseniaAsync(int idUsuario, string contraseniaHash)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync("sp_Usuario_ActualizarContrasenia", new { idUsuario, contraseniaHash });
    }

    public async Task GenerarTokenResetAsync(int idUsuario, string token, int minutosValidez)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync("sp_PasswordResetToken_Generar", new { idUsuario, token, minutosValidez });
    }

    public async Task<int?> ValidarTokenResetAsync(string token)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultProcAsync<int?>("sp_PasswordResetToken_Validar", new { token });
    }

    public async Task MarcarTokenResetUsadoAsync(string token)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync("sp_PasswordResetToken_MarcarUsado", new { token });
    }
}
