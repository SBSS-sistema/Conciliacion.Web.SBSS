using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Services;

public class ArchivoAlmacenamientoService : IArchivoAlmacenamientoService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(10) };

    private readonly string _carpetaBase;
    private readonly string? _supabaseUrl;
    private readonly string? _supabaseKey;
    private readonly string _bucket;

    private bool UsaSupabase => !string.IsNullOrWhiteSpace(_supabaseUrl) && !string.IsNullOrWhiteSpace(_supabaseKey);

    public ArchivoAlmacenamientoService(IWebHostEnvironment env, IConfiguration config)
    {
        var carpetaConfigurada = config["Almacenamiento:CarpetaArchivos"] ?? "App_Data/uploads";
        _carpetaBase = Path.Combine(env.ContentRootPath, carpetaConfigurada);

        _supabaseUrl = config["Supabase:Url"]?.TrimEnd('/');
        _supabaseKey = config["Supabase:ServiceKey"];
        _bucket = config["Supabase:Bucket"] ?? "archivos";
    }

    public async Task<ResultadoGuardarArchivo> GuardarAsync(int idExpediente, string tipoArchivo, IFormFile archivo)
    {
        if (!TiposArchivoAdjunto.ExtensionesPermitidas.ContainsKey(tipoArchivo))
            return new ResultadoGuardarArchivo(false, "Tipo de archivo no reconocido.", null, 0);

        if (archivo is null || archivo.Length == 0)
            return new ResultadoGuardarArchivo(false, "Selecciona un archivo.", null, 0);

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var extensionesValidas = TiposArchivoAdjunto.ExtensionesPermitidas[tipoArchivo];
        if (!extensionesValidas.Contains(extension))
        {
            return new ResultadoGuardarArchivo(
                false,
                $"Extensión no permitida para {TiposArchivoAdjunto.Etiquetas[tipoArchivo]}. Formatos válidos: {string.Join(", ", extensionesValidas)}.",
                null, 0);
        }

        var tamanioMaximo = TiposArchivoAdjunto.TamanioMaximoBytes[tipoArchivo];
        if (archivo.Length > tamanioMaximo)
        {
            return new ResultadoGuardarArchivo(
                false,
                $"El archivo supera el tamaño máximo permitido ({tamanioMaximo / (1024 * 1024)} MB).",
                null, 0);
        }

        // Nombre aleatorio: nunca se usa el nombre original en la ruta,
        // así se evita path traversal y colisiones de nombres.
        var nombreFisico = $"{Guid.NewGuid():N}{extension}";
        var rutaRelativa = $"{idExpediente}/{nombreFisico}";
        var tamanioKB = (int)Math.Ceiling(archivo.Length / 1024.0);

        if (UsaSupabase)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, UrlObjeto(rutaRelativa));
            AgregarAuth(req);
            await using var stream = archivo.OpenReadStream();
            var contenido = new StreamContent(stream);
            contenido.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(archivo.ContentType) ? "application/octet-stream" : archivo.ContentType);
            req.Content = contenido;

            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var detalle = await resp.Content.ReadAsStringAsync();
                return new ResultadoGuardarArchivo(false,
                    $"No se pudo guardar el archivo en el almacenamiento ({(int)resp.StatusCode}). {detalle}", null, 0);
            }

            return new ResultadoGuardarArchivo(true, null, rutaRelativa, tamanioKB);
        }

        // Modo local (sin Supabase configurado): disco
        var carpetaExpediente = Path.Combine(_carpetaBase, idExpediente.ToString());
        Directory.CreateDirectory(carpetaExpediente);
        var rutaFisica = Path.Combine(carpetaExpediente, nombreFisico);
        await using (var destino = File.Create(rutaFisica))
        {
            await archivo.CopyToAsync(destino);
        }

        return new ResultadoGuardarArchivo(true, null, rutaRelativa, tamanioKB);
    }

    public string ObtenerRutaFisica(string rutaRelativa) => Path.Combine(_carpetaBase, rutaRelativa);

    public async Task<byte[]?> LeerAsync(string rutaRelativa)
    {
        if (UsaSupabase)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, UrlObjeto(rutaRelativa));
            AgregarAuth(req);
            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsByteArrayAsync();
        }

        var rutaFisica = ObtenerRutaFisica(rutaRelativa);
        return File.Exists(rutaFisica) ? await File.ReadAllBytesAsync(rutaFisica) : null;
    }

    private string UrlObjeto(string rutaRelativa) =>
        $"{_supabaseUrl}/storage/v1/object/{_bucket}/{rutaRelativa.TrimStart('/')}";

    private void AgregarAuth(HttpRequestMessage req)
    {
        req.Headers.TryAddWithoutValidation("apikey", _supabaseKey);
        // Las claves antiguas (service_role) son JWT y van también como Bearer;
        // las nuevas (sb_secret_...) solo van en "apikey".
        if (_supabaseKey!.StartsWith("eyJ"))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseKey);
    }
}
