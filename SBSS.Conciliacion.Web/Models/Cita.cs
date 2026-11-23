using System.ComponentModel.DataAnnotations;

namespace SBSS.Conciliacion.Web.Models;

public class Cita
{
    public long IdCita { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string ApellidoMaterno { get; set; } = string.Empty;
    public string DniCliente { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public int IdConciliador { get; set; }
    public int IdUsuarioCreador { get; set; }
    public DateTime FechaHora { get; set; }
    public string Estado { get; set; } = "PROGRAMADA";
    public string? Observaciones { get; set; }
    public int? IdExpediente { get; set; }
    public string? Conciliador { get; set; }
}

public class CrearCitaVm
{
    [Required, StringLength(100)] public string NombreCliente { get; set; } = string.Empty;
    [Required, StringLength(100)] public string ApellidoPaterno { get; set; } = string.Empty;
    [Required, StringLength(100)] public string ApellidoMaterno { get; set; } = string.Empty;
    [Required, StringLength(15)] public string DniCliente { get; set; } = string.Empty;
    [Phone, StringLength(20)] public string? Telefono { get; set; }
    [EmailAddress, StringLength(100)] public string? Correo { get; set; }
    [Required, StringLength(1000)] public string Motivo { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int IdConciliador { get; set; }
    [Required] public DateTime FechaHora { get; set; }
    [StringLength(2000)] public string? Observaciones { get; set; }
}

public class CitaListItemVm
{
    public long IdCita { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string ApellidoMaterno { get; set; } = string.Empty;
    public string DniCliente { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public DateTime FechaHora { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public int IdConciliador { get; set; }
    public string? Conciliador { get; set; }
    public int? IdExpediente { get; set; }
}

public class ConvertirCitaVm
{
    [Required] public long IdCita { get; set; }
    [Required, StringLength(50)] public string NumeroExpediente { get; set; } = string.Empty;
    [Range(1, 90)] public int PlazoDias { get; set; } = 30;
}
