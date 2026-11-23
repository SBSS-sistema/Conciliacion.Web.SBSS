using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace SBSS.Conciliacion.Web.Services;

/// <summary>Envía códigos mediante Gmail SMTP usando una contraseña de aplicación.</summary>
public sealed class GmailSmtpEmailSender : IEmailSender
{
    private const string SmtpHost = "smtp.gmail.com";
    private const int SmtpPort = 587;
    private readonly string _address;
    private readonly string _appPassword;
    private readonly string _fromName;

    public GmailSmtpEmailSender(IConfiguration configuration)
    {
        _address = configuration["Email:Gmail:Address"]?.Trim() ?? string.Empty;
        // Google muestra la contraseña de aplicación con espacios; SMTP espera los 16 caracteres juntos.
        _appPassword = (configuration["Email:Gmail:AppPassword"] ?? string.Empty).Replace(" ", string.Empty);
        _fromName = configuration["Email:Gmail:FromName"]?.Trim() ?? "Gestión de Conciliación SBSS";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_address)
        && !string.IsNullOrWhiteSpace(_appPassword);

    public async Task SendLoginCodeAsync(string destination, string code, int expirationMinutes, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) throw new InvalidOperationException("El remitente Gmail no está configurado.");

        using var message = new MailMessage
        {
            From = new MailAddress(_address, _fromName),
            Subject = "Código para ingresar a Gestión de Conciliación SBSS",
            Body = $"Tu código de verificación es {code}. Vence en {expirationMinutes} minutos. Si no solicitaste este código, ignora este correo.",
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(destination));

        using var client = new SmtpClient(SmtpHost, SmtpPort)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_address, _appPassword),
            Timeout = 15000
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
