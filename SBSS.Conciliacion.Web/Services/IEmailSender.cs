namespace SBSS.Conciliacion.Web.Services;

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendLoginCodeAsync(string destination, string code, int expirationMinutes, CancellationToken cancellationToken = default);
}
