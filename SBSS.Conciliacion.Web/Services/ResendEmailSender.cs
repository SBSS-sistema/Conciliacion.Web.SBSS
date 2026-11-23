using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace SBSS.Conciliacion.Web.Services;

/// <summary>Envía códigos usando la API HTTP de Resend. La clave se toma de Email:Resend:ApiKey.</summary>
public sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _from;

    public ResendEmailSender(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _apiKey = configuration["Email:Resend:ApiKey"] ?? string.Empty;
        _from = configuration["Email:Resend:From"] ?? string.Empty;
        _http.BaseAddress = new Uri("https://api.resend.com/");
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey) && !string.IsNullOrWhiteSpace(_from);

    public async Task SendLoginCodeAsync(string destination, string code, int expirationMinutes, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) throw new InvalidOperationException("El proveedor de correo no está configurado.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(new
        {
            from = _from,
            to = new[] { destination },
            subject = "Código para ingresar a Gestión de Conciliación SBSS",
            text = $"Tu código de verificación es {code}. Vence en {expirationMinutes} minutos. Si no solicitaste este código, ignora este correo.",
            html = $"<p>Tu código de verificación es:</p><p style=\"font-size:28px;font-weight:bold;letter-spacing:6px\">{code}</p><p>Vence en {expirationMinutes} minutos. Si no solicitaste este código, ignora este correo.</p>"
        });

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("El servicio de correo rechazó el envío del código.");
    }
}
