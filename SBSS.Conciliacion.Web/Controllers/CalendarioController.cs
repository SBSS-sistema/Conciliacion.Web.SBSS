using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SBSS.Conciliacion.Web.Repositories;

namespace SBSS.Conciliacion.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR,CONCILIADOR")]
public class CalendarioController : Controller
{
    private readonly IAudienciaRepository _audiencias;
    private readonly ICitaRepository _citas;

    public CalendarioController(IAudienciaRepository audiencias, ICitaRepository citas)
    {
        _audiencias = audiencias;
        _citas = citas;
    }

    // GET: /Calendario
    public IActionResult Index() => View();

    // GET: /Calendario/Eventos?start=...&end=...&soloMias=false&vencimientos=true
    // Lo consume FullCalendar; devuelve audiencias (y opcionalmente vencimientos) del rango visible.
    [HttpGet]
    public async Task<IActionResult> Eventos(string start, string end, bool soloMias = false, bool vencimientos = true)
    {
        if (!TryFecha(start, out var desde) || !TryFecha(end, out var hasta))
            return BadRequest();

        int? idConciliador = null;
        if (User.IsInRole("CONCILIADOR") || soloMias)
        {
            var idTrabajador = int.Parse(User.FindFirstValue("idTrabajador") ?? "0");
            idConciliador = idTrabajador;
        }

        var eventos = new List<object>();

        foreach (var a in await _audiencias.ListarPorRangoAsync(desde, hasta, idConciliador))
        {
            var inicio = a.Fecha.Date + (a.Hora ?? TimeSpan.Zero);
            eventos.Add(new
            {
                id = $"aud-{a.IdDocumento}",
                title = $"{a.NumeroExpediente} · sesión {a.NumeroSesion}",
                start = inicio.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                end = a.Hora.HasValue ? inicio.AddHours(1).ToString("yyyy-MM-dd'T'HH:mm:ss") : null,
                allDay = !a.Hora.HasValue,
                url = Url.Action("Details", "Audiencias", new { id = a.IdDocumento }),
                color = ColorAudiencia(a.Estado, a.Resultado),
                extendedProps = new
                {
                    tipo = "audiencia",
                    estado = a.Estado,
                    resultado = a.Resultado,
                    conciliador = a.Conciliador
                }
            });
        }

        if (vencimientos)
        {
            foreach (var v in await _audiencias.ListarVencimientosPorRangoAsync(desde, hasta, idConciliador))
            {
                eventos.Add(new
                {
                    id = $"venc-{v.IdExpediente}",
                    title = $"Vence {v.NumeroExpediente}",
                    start = v.FechaVencimiento.ToString("yyyy-MM-dd"),
                    allDay = true,
                    url = Url.Action("Details", "Expedientes", new { id = v.IdExpediente }),
                    color = "#a4302f",
                    extendedProps = new { tipo = "vencimiento", estado = v.EstadoProceso, resultado = (string?)null, conciliador = (string?)null }
                });
            }
        }

        foreach (var cita in await _citas.ListarPorRangoAsync(desde, hasta, idConciliador))
        {
            var inicio = DateTime.SpecifyKind(cita.FechaHora, DateTimeKind.Unspecified);
            eventos.Add(new
            {
                id = $"cita-{cita.IdCita}",
                title = $"Cita · {cita.NombreCliente} {cita.ApellidoPaterno}",
                start = inicio.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                end = inicio.AddMinutes(45).ToString("yyyy-MM-dd'T'HH:mm:ss"),
                allDay = false,
                url = Url.Action("Details", "Citas", new { id = cita.IdCita }),
                color = cita.Estado == "CANCELADA" ? "#5b6a70" : "#b3893a",
                extendedProps = new { tipo = "cita", estado = cita.Estado, conciliador = cita.Conciliador }
            });
        }

        return Json(eventos);
    }

    // FullCalendar manda fechas ISO ("2026-11-01T00:00:00-05:00"); solo interesa el día.
    private static bool TryFecha(string? valor, out DateTime fecha)
    {
        fecha = default;
        if (string.IsNullOrEmpty(valor) || valor.Length < 10) return false;
        return DateTime.TryParseExact(valor[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
    }

    private static string ColorAudiencia(string? estado, string? resultado)
    {
        if (!string.IsNullOrWhiteSpace(resultado)) return "#1f6b3a";                 // ya tiene resultado
        var e = (estado ?? string.Empty).ToUpperInvariant();
        if (e.Contains("CANCEL")) return "#5b6a70";
        if (e.Contains("REPROGRAM")) return "#b3893a";
        return "#0c3855";                                                           // programada
    }
}
