namespace SBSS.Conciliacion.Web.Models;

public sealed class EmailLoginCodeOptions
{
    public bool Enabled { get; set; }
    public int ExpirationMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public int MaxSendsPer15Minutes { get; set; } = 3;
    public string HashKey { get; set; } = string.Empty;
}
