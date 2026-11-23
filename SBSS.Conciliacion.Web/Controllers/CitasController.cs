using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SBSS.Conciliacion.Web.Models;
using SBSS.Conciliacion.Web.Repositories;
using SBSS.Conciliacion.Web.Services;

namespace SBSS.Conciliacion.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR,MESA_DE_PARTES,CONCILIADOR")]
public class CitasController : Controller
{
    private readonly ICitaRepository _citas;
    private readonly IUsuarioRepository _usuarios;
    private readonly INotificacionRepository _notificaciones;

    public CitasController(ICitaRepository citas,
        IUsuarioRepository usuarios, INotificacionRepository notificaciones)
    {
        _citas = citas;
        _usuarios = usuarios;
        _notificaciones = notificaciones;
    }

    public async Task<IActionResult> Index()
    {
        int? conciliador = User.IsInRole("CONCILIADOR") ? IdTrabajadorActual() : null;
        return View(await _citas.ListarAsync(conciliador));
    }

    public async Task<IActionResult> Details(long id)
    {
        var cita = await ObtenerCitaPermitidaAsync(id);
        return cita is null ? NotFound() : View(cita);
    }

    [Authorize(Roles = "ADMINISTRADOR,MESA_DE_PARTES")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await CargarConciliadoresAsync();
        return View(new CrearCitaVm { FechaHora = HoraLocal.AhoraPeru.AddDays(1) });
    }

    [Authorize(Roles = "ADMINISTRADOR,MESA_DE_PARTES")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CrearCitaVm vm)
    {
        if (vm.FechaHora <= HoraLocal.AhoraPeru)
            ModelState.AddModelError(nameof(vm.FechaHora), "La cita debe programarse para una fecha futura.");
        var conciliador = (await _citas.ListarConciliadoresAsync()).FirstOrDefault(t => t.IdTrabajador == vm.IdConciliador);
        if (conciliador is null)
            ModelState.AddModelError(nameof(vm.IdConciliador), "Selecciona un conciliador activo.");

        if (!ModelState.IsValid)
        {
            await CargarConciliadoresAsync();
            return View(vm);
        }

        var idUsuario = IdUsuarioActual();
        var idCita = await _citas.CrearAsync(vm, idUsuario);
        var idUsuarioConciliador = await _usuarios.ObtenerIdUsuarioPorTrabajadorAsync(vm.IdConciliador);
        if (idUsuarioConciliador.HasValue)
        {
            await _notificaciones.CrearAsync(idUsuarioConciliador.Value, null, "SISTEMA",
                $"Tienes una nueva cita con {vm.NombreCliente} {vm.ApellidoPaterno} el {vm.FechaHora:dd/MM/yyyy HH:mm}.");
        }

        TempData["Ok"] = "La cita quedó registrada y se notificó al conciliador.";
        return RedirectToAction(nameof(Details), new { id = idCita });
    }

    [Authorize(Roles = "ADMINISTRADOR,MESA_DE_PARTES")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reprogramar(long id, DateTime fechaHora, string? observaciones)
    {
        var cita = await _citas.ObtenerPorIdAsync(id);
        if (cita is null) return NotFound();
        if (fechaHora <= HoraLocal.AhoraPeru)
        {
            TempData["Error"] = "La nueva fecha debe estar en el futuro.";
            return RedirectToAction(nameof(Details), new { id });
        }
        try
        {
            await _citas.ActualizarAgendaAsync(id, fechaHora, observaciones, "REPROGRAMADA");
            var idUsuario = await _usuarios.ObtenerIdUsuarioPorTrabajadorAsync(cita.IdConciliador);
            if (idUsuario.HasValue)
                await _notificaciones.CrearAsync(idUsuario.Value, null, "SISTEMA",
                    $"La cita con {cita.NombreCliente} fue reprogramada al {fechaHora:dd/MM/yyyy HH:mm}.");
            TempData["Ok"] = "Cita reprogramada.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "ADMINISTRADOR,MESA_DE_PARTES")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(long id, string? observaciones)
    {
        try
        {
            var cita = await _citas.ObtenerPorIdAsync(id);
            if (cita is null) return NotFound();
            await _citas.ActualizarAgendaAsync(id, cita.FechaHora, observaciones, "CANCELADA");
            TempData["Ok"] = "Cita cancelada.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "ADMINISTRADOR,CONCILIADOR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarAtencion(long id, string estado, string? observaciones)
    {
        if (estado is not ("ATENDIDA" or "NO_ASISTIO")) return BadRequest();
        var cita = await ObtenerCitaPermitidaAsync(id);
        if (cita is null) return NotFound();
        try
        {
            await _citas.RegistrarAtencionAsync(id, estado, observaciones);
            TempData["Ok"] = estado == "ATENDIDA" ? "La cita quedó marcada como atendida." : "Se registró que la persona no asistió.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "ADMINISTRADOR,CONCILIADOR")]
    [HttpGet]
    public async Task<IActionResult> Convertir(long id)
    {
        var cita = await ObtenerCitaPermitidaAsync(id);
        if (cita is null) return NotFound();
        if (cita.Estado != "ATENDIDA" || cita.IdExpediente.HasValue)
        {
            TempData["Error"] = "Solo una cita atendida y aún no convertida puede iniciar un expediente.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(new ConvertirCitaVm { IdCita = id, PlazoDias = 30 });
    }

    [Authorize(Roles = "ADMINISTRADOR,CONCILIADOR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Convertir(ConvertirCitaVm vm)
    {
        var cita = await ObtenerCitaPermitidaAsync(vm.IdCita);
        if (cita is null) return NotFound();
        if (!ModelState.IsValid) return View(vm);
        try
        {
            var idTrabajador = User.IsInRole("ADMINISTRADOR") ? cita.IdConciliador : IdTrabajadorActual();
            var idExpediente = await _citas.ConvertirEnExpedienteAsync(vm.IdCita, idTrabajador, vm.NumeroExpediente, vm.PlazoDias);
            TempData["Ok"] = "Se creó el expediente desde la cita; los datos de la persona quedaron vinculados.";
            return RedirectToAction("Details", "Expedientes", new { id = idExpediente });
        }
        catch (Exception ex) when (ex is InvalidOperationException or Npgsql.PostgresException)
        {
            TempData["Error"] = ex is InvalidOperationException ? ex.Message : "No se pudo convertir la cita. Verifica que el número de expediente no esté repetido y que la migración esté aplicada.";
            return RedirectToAction(nameof(Details), new { id = vm.IdCita });
        }
    }

    private async Task<Cita?> ObtenerCitaPermitidaAsync(long id)
    {
        var cita = await _citas.ObtenerPorIdAsync(id);
        if (cita is null) return null;
        return User.IsInRole("CONCILIADOR") && cita.IdConciliador != IdTrabajadorActual() ? null : cita;
    }

    private async Task CargarConciliadoresAsync() =>
        ViewBag.Conciliadores = (await _citas.ListarConciliadoresAsync()).ToList();

    private int IdUsuarioActual() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int IdTrabajadorActual() => int.Parse(User.FindFirstValue("idTrabajador") ?? "0");
}
