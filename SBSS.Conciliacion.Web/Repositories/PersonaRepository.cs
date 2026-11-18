using SBSS.Conciliacion.Web.Data;
using SBSS.Conciliacion.Web.Models;

namespace SBSS.Conciliacion.Web.Repositories;

public class PersonaRepository : IPersonaRepository
{
    private readonly IDbConnectionFactory _factory;

    public PersonaRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> InsertarNaturalAsync(CrearPersonaNaturalVm vm)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>(
            "sp_PersonaNatural_Insertar",
            new
            {
                idSede = vm.IdSede,
                celularP = vm.CelularP,
                correoP = vm.CorreoP,
                whatsappP = vm.WhatsappP,
                dniPersonaN = vm.DniPersonaN,
                nombrePersonaN = vm.NombrePersonaN,
                apellidoPaternoPersonaN = vm.ApellidoPaternoPersonaN,
                apellidoMaternoPersonaN = vm.ApellidoMaternoPersonaN
            });
    }

    public async Task<int> InsertarJuridicaAsync(CrearPersonaJuridicaVm vm)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>(
            "sp_PersonaJuridica_Insertar",
            new
            {
                idSede = vm.IdSede,
                celularP = vm.CelularP,
                correoP = vm.CorreoP,
                whatsappP = vm.WhatsappP,
                ruc = vm.Ruc,
                razonSocial = vm.RazonSocial
            });
    }

    public async Task<PersonaDetalle?> ObtenerPorIdAsync(int idPersona)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultProcAsync<PersonaDetalle>("sp_Persona_ObtenerPorId", new { idPersona });
    }

    public async Task<IEnumerable<PersonaListItem>> ListarAsync(string? busqueda = null)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryProcAsync<PersonaListItem>("sp_Persona_Listar", new { busqueda });
    }

    public async Task CambiarEstadoAsync(int idPersona, string estadoPersona)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync("sp_Persona_CambiarEstado", new { idPersona, estadoPersona });
    }

    public async Task<int> InsertarDireccionAsync(DireccionVm direccion)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ScalarProcAsync<int>(
            "sp_Direccion_Insertar",
            new
            {
                idDistrito = direccion.IdDistrito,
                tipoVia = direccion.TipoVia,
                nombreVia = direccion.NombreVia,
                numero = direccion.Numero,
                manzana = (string?)null,
                lote = (string?)null,
                urbanizacion = direccion.Urbanizacion,
                referencia = direccion.Referencia,
                codigoPostal = (string?)null
            });
    }

    public async Task VincularDireccionAsync(int idPersona, int idDireccion, bool esPrincipal = true)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecProcAsync("sp_DetalleDireccion_Insertar", new { idPersona, idDireccion, esPrincipal });
    }

    public async Task<DireccionDetalleVm?> ObtenerDireccionPrincipalAsync(int idPersona)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultProcAsync<DireccionDetalleVm>("sp_Persona_ObtenerDireccionPrincipal", new { idPersona });
    }
}
