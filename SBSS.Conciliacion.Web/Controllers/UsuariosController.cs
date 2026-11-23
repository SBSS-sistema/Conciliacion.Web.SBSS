using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SBSS.Conciliacion.Web.Models;
using SBSS.Conciliacion.Web.Repositories;

namespace SBSS.Conciliacion.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR")]
public class UsuariosController : Controller
{
    private static readonly string[] RolesPermitidos = ["ADMINISTRADOR", "CONCILIADOR", "MESA_DE_PARTES"];
    private readonly IUsuarioRepository _usuarios;

    public UsuariosController(IUsuarioRepository usuarios) => _usuarios = usuarios;

    public async Task<IActionResult> Index() => View(await _usuarios.ListarParaAdministracionAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(CambiarRolUsuarioVm vm)
    {
        if (!RolesPermitidos.Contains(vm.Rol, StringComparer.Ordinal))
        {
            TempData["Error"] = "El rol seleccionado no es válido.";
            return RedirectToAction(nameof(Index));
        }
        var idActual = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        if (idActual == vm.IdUsuario)
        {
            TempData["Error"] = "No puedes cambiar tu propio rol.";
            return RedirectToAction(nameof(Index));
        }
        if (!await _usuarios.CambiarRolAsync(vm.IdUsuario, vm.Rol))
        {
            TempData["Error"] = "No se aplicó el cambio. Verifica que el usuario exista y que quede al menos un administrador.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Ok"] = "Rol actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerarRestablecimiento(int idUsuario)
    {
        var usuario = (await _usuarios.ListarParaAdministracionAsync()).FirstOrDefault(x => x.IdUsuario == idUsuario);
        if (usuario is null) return NotFound();
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        await _usuarios.GenerarTokenResetAsync(idUsuario, token, minutosValidez: 30);
        var enlace = Url.Action("RestablecerContrasenia", "Account", new { token }, Request.Scheme);
        ViewBag.EnlaceGenerado = enlace;
        ViewBag.UsuarioEnlace = usuario.Usuario;
        ViewBag.ExpiraEnMinutos = 30;
        return View("Index", await _usuarios.ListarParaAdministracionAsync());
    }
}
