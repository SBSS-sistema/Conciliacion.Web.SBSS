namespace SBSS.Conciliacion.Web.Repositories;

public enum CrearCodigoLoginEstado
{
    Creado,
    LimiteEnvios
}

public sealed record CrearCodigoLoginResultado(CrearCodigoLoginEstado Estado, Guid DesafioId);

public interface ILoginEmailCodeRepository
{
    Task<CrearCodigoLoginResultado> CrearAsync(int idUsuario, byte[] hash, DateTimeOffset expiraEn, int maxEnviosEnVentana);
    Task InvalidarAsync(Guid desafioId);
    Task<int?> ValidarYConsumirAsync(Guid desafioId, byte[] hashPresentado, int maxIntentos);
}
