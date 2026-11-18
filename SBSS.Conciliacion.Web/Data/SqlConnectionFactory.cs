using System.Data;
using Npgsql;

namespace SBSS.Conciliacion.Web.Data;

// Se mantiene el nombre de la clase para no tocar Program.cs; ahora conecta a PostgreSQL (Supabase).
public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
