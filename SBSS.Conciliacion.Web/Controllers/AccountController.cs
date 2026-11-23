using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SBSS.Conciliacion.Web.Models;
using SBSS.Conciliacion.Web.Repositories;
using SBSS.Conciliacion.Web.Services;
using Microsoft.Extensions.Options;

namespace SBSS.Conciliacion.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IUsuarioRepository _usuarios;
    private readonly ILoginEmailCodeRepository _codigos;
    private readonly IEmailSender _email;
    private readonly EmailLoginCodeOptions _emailCodeOptions;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IUsuarioRepository usuarios, ILoginEmailCodeRepository codigos,
        IEmailSender email, IOptions<EmailLoginCodeOptions> emailCodeOptions,
        ILogger<AccountController> logger)
    {
        _usuarios = usuarios;
        _codigos = codigos;
        _email = email;
        _emailCodeOptions = emailCodeOptions.Value;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity is { IsAuthenticated: true })
            return User.IsInRole("ADMINISTRADOR")
                ? RedirectToAction("Index", "Dashboard")
                : RedirectToAction("Index", "Citas");

        return View(new LoginVm { ReturnUrl = returnUrl });
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = await _usuarios.ObtenerParaLoginAsync(vm.Usuario);

        var credencialesValidas = usuario is not null
            && BCrypt.Net.BCrypt.Verify(vm.Contrasenia, usuario.Contrasenia);

        if (!credencialesValidas)
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(vm);
        }

        if (!string.Equals(usuario!.EstadoTrabajador, "ACTIVO", StringComparison.OrdinalIgnoreCase)
            && usuario.EstadoTrabajador is not null)
        {
            ModelState.AddModelError(string.Empty, "Tu usuario no está activo. Contacta al administrador del sistema.");
            return View(vm);
        }

        var rol = (usuario.Rol ?? "").Trim().ToUpperInvariant();
        if (rol is not ("ADMINISTRADOR" or "CONCILIADOR" or "MESA_DE_PARTES"))
        {
            ModelState.AddModelError(string.Empty, "El usuario no tiene un rol habilitado. Contacta al administrador.");
            return View(vm);
        }

        if (_emailCodeOptions.Enabled)
        {
            if (!_email.IsConfigured || _emailCodeOptions.HashKey.Length < 32)
            {
                _logger.LogError("Email login code is enabled but the sender or hash key is not configured.");
                ModelState.AddModelError(string.Empty, "La verificación por correo no está disponible. Contacta al administrador.");
                return View(vm);
            }

            if (string.IsNullOrWhiteSpace(usuario.EmailTrabajador))
            {
                ModelState.AddModelError(string.Empty, "Tu cuenta no tiene un correo registrado. Contacta al administrador.");
                return View(vm);
            }

            var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var desafio = await _codigos.CrearAsync(usuario.IdUsuario, HashCodigo(codigo),
                DateTimeOffset.UtcNow.AddMinutes(_emailCodeOptions.ExpirationMinutes),
                _emailCodeOptions.MaxSendsPer15Minutes);
            if (desafio.Estado == CrearCodigoLoginEstado.LimiteEnvios)
            {
                ModelState.AddModelError(string.Empty, "Se alcanzó el límite de envíos. Espera 15 minutos e inténtalo de nuevo.");
                return View(vm);
            }

            try
            {
                await _email.SendLoginCodeAsync(usuario.EmailTrabajador, codigo, _emailCodeOptions.ExpirationMinutes);
            }
            catch (Exception ex)
            {
                await _codigos.InvalidarAsync(desafio.DesafioId);
                _logger.LogError(ex, "Could not deliver a login verification email.");
                ModelState.AddModelError(string.Empty, "No se pudo enviar el código. Verifica tu correo con el administrador e inténtalo de nuevo.");
                return View(vm);
            }

            return RedirectToAction(nameof(VerificarCodigo), new { desafioId = desafio.DesafioId, returnUrl = vm.ReturnUrl });
        }

        return await CompletarInicioSesionAsync(usuario, vm.ReturnUrl);
    }

    [HttpGet]
    public IActionResult VerificarCodigo(Guid desafioId, string? returnUrl = null)
    {
        if (desafioId == Guid.Empty) return RedirectToAction(nameof(Login));
        return View(new VerificarCodigoLoginVm { DesafioId = desafioId, ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerificarCodigo(VerificarCodigoLoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (!_emailCodeOptions.Enabled || _emailCodeOptions.HashKey.Length < 32)
            return RedirectToAction(nameof(Login));

        var idUsuario = await _codigos.ValidarYConsumirAsync(vm.DesafioId, HashCodigo(vm.Codigo), _emailCodeOptions.MaxAttempts);
        if (!idUsuario.HasValue)
        {
            ModelState.AddModelError(string.Empty, "El código no es válido, venció o superó el límite de intentos.");
            return View(vm);
        }

        var usuario = await _usuarios.ObtenerParaAutenticacionAsync(idUsuario.Value);
        if (usuario is null || !string.Equals(usuario.EstadoTrabajador, "ACTIVO", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "La cuenta ya no está activa. Contacta al administrador.");
            return View(vm);
        }

        var rol = (usuario.Rol ?? string.Empty).Trim().ToUpperInvariant();
        if (rol is not ("ADMINISTRADOR" or "CONCILIADOR" or "MESA_DE_PARTES"))
        {
            ModelState.AddModelError(string.Empty, "El usuario no tiene un rol habilitado. Contacta al administrador.");
            return View(vm);
        }

        return await CompletarInicioSesionAsync(usuario, vm.ReturnUrl);
    }

    private byte[] HashCodigo(string codigo)
    {
        var key = Encoding.UTF8.GetBytes(_emailCodeOptions.HashKey);
        return HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(codigo));
    }

    private async Task<IActionResult> CompletarInicioSesionAsync(UsuarioLogin usuario, string? returnUrl)
    {
        var rol = (usuario.Rol ?? string.Empty).Trim().ToUpperInvariant();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, $"{usuario.NombreTrabajador} {usuario.ApePaterTrabajador}"),
            new("idTrabajador", usuario.IdTrabajador.ToString()),
            new(ClaimTypes.Role, rol)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return rol == "ADMINISTRADOR"
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Citas");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // ---------------------------------------------------------------
    // Recuperación de contraseña.
    //
    // El sistema no tiene un servicio de correo configurado (no estaba
    // en el alcance original), así que en vez de "enviar un email" que
    // en realidad no llegaría a ningún lado, se muestra el enlace de
    // restablecimiento en pantalla para que el administrador se lo
    // haga llegar al usuario por el medio que corresponda. El único
    // cambio para conectarlo a un correo real sería reemplazar
    // MostrarEnlace() por una llamada a un IEmailSender.
    // ---------------------------------------------------------------

    [HttpGet]
    public IActionResult OlvideContrasenia() => View(new OlvideContraseniaVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OlvideContrasenia(OlvideContraseniaVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        // El correo saliente no está configurado. El enlace solo lo genera un
        // administrador desde Usuarios; esta pantalla pública nunca revela enlaces.
        TempData["Ok"] = "Solicita al administrador que genere un enlace de restablecimiento.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> RestablecerContrasenia(string token)
    {
        var idUsuario = await _usuarios.ValidarTokenResetAsync(token);
        if (idUsuario is null)
        {
            TempData["Error"] = "El enlace de restablecimiento no es válido o ya expiró.";
            return RedirectToAction(nameof(Login));
        }

        return View(new RestablecerContraseniaVm { Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerContrasenia(RestablecerContraseniaVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var idUsuario = await _usuarios.ValidarTokenResetAsync(vm.Token);
        if (idUsuario is null)
        {
            TempData["Error"] = "El enlace de restablecimiento no es válido o ya expiró.";
            return RedirectToAction(nameof(Login));
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(vm.NuevaContrasenia);
        await _usuarios.ActualizarContraseniaAsync(idUsuario.Value, hash);
        await _usuarios.MarcarTokenResetUsadoAsync(vm.Token);

        TempData["Ok"] = "Tu contraseña se actualizó correctamente. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

}
