using System.Security.Cryptography;
using Dapper;
using Npgsql;
using SBSS.Conciliacion.Web.Data;

namespace SBSS.Conciliacion.Web.Repositories;

public sealed class LoginEmailCodeRepository : ILoginEmailCodeRepository
{
    private readonly IDbConnectionFactory _factory;

    public LoginEmailCodeRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<CrearCodigoLoginResultado> CrearAsync(int idUsuario, byte[] hash, DateTimeOffset expiraEn, int maxEnviosEnVentana)
    {
        await using var conn = AbrirConexion();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        await conn.ExecuteAsync("SELECT pg_advisory_xact_lock(73021, @IdUsuario);", new { IdUsuario = idUsuario }, tx);
        var envios = await conn.ExecuteScalarAsync<int>("""
            SELECT count(*) FROM public.login_email_code
            WHERE idusuario = @IdUsuario AND creado_en >= now() - interval '15 minutes';
            """, new { IdUsuario = idUsuario }, tx);

        if (envios >= maxEnviosEnVentana)
        {
            await tx.CommitAsync();
            return new(CrearCodigoLoginEstado.LimiteEnvios, Guid.Empty);
        }

        await conn.ExecuteAsync("""
            UPDATE public.login_email_code SET consumido_en = now()
            WHERE idusuario = @IdUsuario AND consumido_en IS NULL;
            """, new { IdUsuario = idUsuario }, tx);

        var desafioId = Guid.NewGuid();
        await conn.ExecuteAsync("""
            INSERT INTO public.login_email_code
                (desafio_id, idusuario, codigo_hash, expira_en, intentos)
            VALUES (@DesafioId, @IdUsuario, @Hash, @ExpiraEn, 0);
            """, new { DesafioId = desafioId, IdUsuario = idUsuario, Hash = hash, ExpiraEn = expiraEn }, tx);

        await tx.CommitAsync();
        return new(CrearCodigoLoginEstado.Creado, desafioId);
    }

    public async Task InvalidarAsync(Guid desafioId)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("""
            UPDATE public.login_email_code SET consumido_en = now()
            WHERE desafio_id = @DesafioId AND consumido_en IS NULL;
            """, new { DesafioId = desafioId });
    }

    public async Task<int?> ValidarYConsumirAsync(Guid desafioId, byte[] hashPresentado, int maxIntentos)
    {
        await using var conn = AbrirConexion();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var desafio = await conn.QuerySingleOrDefaultAsync<DesafioRow>("""
            SELECT idusuario AS IdUsuario, codigo_hash AS Hash, expira_en AS ExpiraEn,
                   consumido_en AS ConsumidoEn, intentos AS Intentos
            FROM public.login_email_code
            WHERE desafio_id = @DesafioId
            FOR UPDATE;
            """, new { DesafioId = desafioId }, tx);

        if (desafio is null || desafio.ConsumidoEn.HasValue || desafio.ExpiraEn <= DateTimeOffset.UtcNow || desafio.Intentos >= maxIntentos)
        {
            await tx.CommitAsync();
            return null;
        }

        if (!CryptographicOperations.FixedTimeEquals(desafio.Hash, hashPresentado))
        {
            var intentos = desafio.Intentos + 1;
            await conn.ExecuteAsync("""
                UPDATE public.login_email_code SET intentos = @Intentos,
                    consumido_en = CASE WHEN @Intentos >= @MaxIntentos THEN now() ELSE consumido_en END
                WHERE desafio_id = @DesafioId;
                """, new { Intentos = intentos, MaxIntentos = maxIntentos, DesafioId = desafioId }, tx);
            await tx.CommitAsync();
            return null;
        }

        await conn.ExecuteAsync("""
            UPDATE public.login_email_code SET consumido_en = now()
            WHERE desafio_id = @DesafioId;
            """, new { DesafioId = desafioId }, tx);
        await tx.CommitAsync();
        return desafio.IdUsuario;
    }

    private NpgsqlConnection AbrirConexion() => _factory.CreateConnection() as NpgsqlConnection
        ?? throw new InvalidOperationException("La verificación por correo requiere PostgreSQL.");

    private sealed class DesafioRow
    {
        public int IdUsuario { get; init; }
        public byte[] Hash { get; init; } = [];
        public DateTimeOffset ExpiraEn { get; init; }
        public DateTimeOffset? ConsumidoEn { get; init; }
        public int Intentos { get; init; }
    }
}
