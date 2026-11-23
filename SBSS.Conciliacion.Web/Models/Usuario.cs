using System.ComponentModel.DataAnnotations;

namespace SBSS.Conciliacion.Web.Models;

/// <summary>Resultado de sp_Usuario_ObtenerParaLogin.</summary>
public class UsuarioLogin
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Contrasenia { get; set; } = string.Empty; // hash BCrypt
    public string? Rol { get; set; }
    public int IdTrabajador { get; set; }
    public string NombreTrabajador { get; set; } = string.Empty;
    public string ApePaterTrabajador { get; set; } = string.Empty;
    public string? EmailTrabajador { get; set; }
    public string? EstadoTrabajador { get; set; }
}

public class UsuarioAdminVm
{
    public int IdUsuario { get; set; }
    public int IdTrabajador { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? NombreTrabajador { get; set; }
    public string? Rol { get; set; }
}

public class CambiarRolUsuarioVm
{
    [Required] public int IdUsuario { get; set; }
    [Required] public string Rol { get; set; } = string.Empty;
}

public class LoginVm
{
    [Required(ErrorMessage = "Ingresa tu usuario.")]
    [Display(Name = "Usuario o correo")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasenia { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class VerificarCodigoLoginVm
{
    [Required]
    public Guid DesafioId { get; set; }

    [Required(ErrorMessage = "Ingresa el código de seis dígitos.")]
    [RegularExpression("^\\d{6}$", ErrorMessage = "El código debe tener seis dígitos.")]
    [Display(Name = "Código de verificación")]
    public string Codigo { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class OlvideContraseniaVm
{
    [Required(ErrorMessage = "Ingresa tu usuario.")]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;
}

public class RestablecerContraseniaVm
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa la nueva contraseña.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string NuevaContrasenia { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    [Compare(nameof(NuevaContrasenia), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarContrasenia { get; set; } = string.Empty;
}
