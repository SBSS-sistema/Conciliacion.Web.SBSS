namespace SBSS.Conciliacion.Web.Models;

public class AudienciaCalendarioVm
{
    public int IdDocumento { get; set; }
    public int IdExpediente { get; set; }
    public string NumeroExpediente { get; set; } = string.Empty;
    public int NumeroSesion { get; set; }
    public DateTime Fecha { get; set; }
    public TimeSpan? Hora { get; set; }
    public string? Estado { get; set; }
    public string? Resultado { get; set; }
    public string? Conciliador { get; set; }
}

public class VencimientoCalendarioVm
{
    public int IdExpediente { get; set; }
    public string NumeroExpediente { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public string? EstadoProceso { get; set; }
}
