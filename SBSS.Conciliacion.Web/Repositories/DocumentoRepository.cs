using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

// Versión PostgreSQL/Supabase. Ya no usa DbType/ParameterDirection ni CommandType.StoredProcedure.
public class DocumentoRepository : IDocumentoRepository
{
    private readonly IDbConnectionFactory _factory;

    public DocumentoRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> InsertarDocumentoGeneradoAsync(int idTipoDocumentos, int idExpediente)
    {
        using var conn = _factory.CreateConnection();
        // El parámetro OUTPUT ahora llega como columna "iddocumentonuevo"
        return await conn.ScalarProcAsync<int>(
            "sp_DocumentoGenerado_Insertar",
            new { idTipoDocumentos, idExpediente });
    }

    public async Task RegistrarActaConciliacionAsync(ActaConciliacionVm vm)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync(
            "sp_ActaConciliacion_Registrar",
            new
            {
                idDocumento = vm.IdDocumento,
                descripcionControversia = vm.DescripcionControversia,
                acuerdoConciliatorio = vm.AcuerdoConciliatorio
            });
    }

    public async Task RegistrarActaNotificacionAsync(int idDocumento, DateTime fechaNotificacion, TimeSpan horaNotificacion)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync(
            "sp_ActaNotificacion_Registrar",
            new { idDocumento, fechaNotificacion, horaNotificacion });
    }

    public async Task InsertarAnexoAsync(int idDocumento, string nombreArchivo, string rutaArchivo)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync(
            "sp_Anexo_Insertar",
            new { idDocumento, nombreArchivo, rutaArchivo });
    }

    public async Task InsertarArchivoAdjuntoAsync(int idExpediente, int idTrabajador, string tipoArchivo, string nombreArchivo, string rutaArchivo, int? tamanioKB)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync(
            "sp_ArchivoAdjunto_Insertar",
            new { idExpediente, idTrabajador, tipoArchivo, nombreArchivo, rutaArchivo, tamanioKB });
    }

    public async Task<IEnumerable<ArchivoAdjuntoVm>> ListarArchivosPorExpedienteAsync(int idExpediente, string? tipoArchivo = null)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<ArchivoAdjuntoVm>(
            "sp_ArchivoAdjunto_ListarPorExpediente",
            new { idExpediente, tipoArchivo });
    }

    public async Task<ArchivoAdjuntoVm?> ObtenerArchivoPorIdAsync(int idArchivo)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultProcAsync<ArchivoAdjuntoVm>(
            "sp_ArchivoAdjunto_ObtenerPorId",
            new { idArchivo });
    }

    public async Task<IEnumerable<TipoDocumento>> ListarTiposDocumentoAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<TipoDocumento>("sp_Documentos_Listar");
    }
}
