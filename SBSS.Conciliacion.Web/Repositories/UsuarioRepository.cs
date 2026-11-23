using Dapper;
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
        var porUsuario = await conn.QueryFirstOrDefaultProcAsync<UsuarioLogin>("sp_Usuario_ObtenerParaLogin", new { usuario });
        if (porUsuario is not null) return porUsuario;

        return await conn.QueryFirstOrDefaultAsync<UsuarioLogin>("""
            SELECT u.idusuario AS IdUsuario, u.usuario AS Usuario, u.contrasenia AS Contrasenia,
                   u.rol AS Rol, u.idtrabajador AS IdTrabajador,
                   t.nombretrabajador AS NombreTrabajador,
                   t.apepatertrabajador AS ApePaterTrabajador,
                   t.estadotrabajador AS EstadoTrabajador
            FROM public.usuario u
            JOIN public.trabajador t ON t.idtrabajador = u.idtrabajador
            WHERE lower(t.emailtrabajador) = lower(@usuario)
            LIMIT 1;
            """, new { usuario });
    }

    public async Task<string?> ObtenerRolActivoAsync(int idUsuario)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<string>("""
            SELECT upper(u.rol) FROM public.usuario u
            JOIN public.trabajador t ON t.idtrabajador = u.idtrabajador
            WHERE u.idusuario = @idUsuario AND upper(coalesce(t.estadotrabajador, '')) = 'ACTIVO';
            """, new { idUsuario });
    }

    public async Task<IEnumerable<UsuarioAdminVm>> ListarParaAdministracionAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<UsuarioAdminVm>("""
            SELECT u.idusuario AS IdUsuario, u.idtrabajador AS IdTrabajador,
                   u.usuario AS Usuario, u.rol AS Rol, t.emailtrabajador AS Correo,
                   concat_ws(' ', t.nombretrabajador, t.apepatertrabajador, t.apematertrabajador) AS NombreTrabajador
            FROM public.usuario u JOIN public.trabajador t ON t.idtrabajador = u.idtrabajador
            ORDER BY t.nombretrabajador, u.usuario;
            """);
    }

    public async Task<bool> CambiarRolAsync(int idUsuario, string rol)
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        // Serialize role changes so two simultaneous updates cannot remove the last two administrators.
        await conn.ExecuteAsync("SELECT pg_advisory_xact_lock(715203011);", transaction: tx);
        const string sql = """
            UPDATE public.usuario u SET rol = @rol
            WHERE u.idusuario = @idUsuario
              AND @rol IN ('ADMINISTRADOR', 'CONCILIADOR', 'MESA_DE_PARTES')
              AND (upper(coalesce(u.rol, '')) <> 'ADMINISTRADOR'
                   OR @rol = 'ADMINISTRADOR'
                   OR (SELECT count(*) FROM public.usuario WHERE upper(coalesce(rol, '')) = 'ADMINISTRADOR') > 1);
            """;
        var updated = await conn.ExecuteAsync(sql, new { idUsuario, rol }, tx) == 1;
        tx.Commit();
        return updated;
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
