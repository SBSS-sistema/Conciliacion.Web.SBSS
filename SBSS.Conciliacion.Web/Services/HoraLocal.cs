namespace SBSS.Conciliacion.Web.Services;

/// <summary>Hora del centro (Perú), independiente de la zona horaria del contenedor Render.</summary>
public static class HoraLocal
{
    private static readonly TimeZoneInfo Zona = ObtenerZona();

    public static DateTime AhoraPeru => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);
    public static DateTime FechaPeru => AhoraPeru.Date;

    private static TimeZoneInfo ObtenerZona()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Lima"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); }
    }
}
