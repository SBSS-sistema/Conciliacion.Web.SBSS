using Microsoft.AspNetCore.Http;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Services;

public record ResultadoGuardarArchivo(bool Exito, string? Error, string? RutaRelativa, int TamanioKB);

/// <summary>
/// Guarda archivos/videos adjuntos a un expediente, validando
/// extensión y tamaño según el tipo declarado (ver TiposArchivoAdjunto).
/// Si Supabase:Url y Supabase:ServiceKey están configurados, los archivos viven en
/// Supabase Storage (bucket privado); si no, se usa el disco local (desarrollo).
/// Nunca confía en el nombre original del archivo para la ruta: genera un nombre
/// aleatorio y conserva el original solo para mostrarlo (columna nombreArchivo en la BD).
/// </summary>
public interface IArchivoAlmacenamientoService
{
    Task<ResultadoGuardarArchivo> GuardarAsync(int idExpediente, string tipoArchivo, IFormFile archivo);
    string ObtenerRutaFisica(string rutaRelativa);

    /// <summary>Devuelve el contenido del archivo, o null si no existe.</summary>
    Task<byte[]?> LeerAsync(string rutaRelativa);
}
