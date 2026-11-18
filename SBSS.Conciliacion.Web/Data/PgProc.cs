using System.Data;
using System.Reflection;
using Dapper;

namespace SBSS.Conciliacion.Web.Data;

/// <summary>
/// Puente SQL Server -> PostgreSQL. Traduce una llamada estilo
/// "EXEC sp_x @idA, @idB" a "SELECT * FROM sp_x(p_ida => @idA, p_idb => @idB)",
/// que es como se invocan las funciones migradas a Supabase.
/// Ruta sugerida: Data/PgProc.cs
/// </summary>
public static class PgProc
{
    private static (string Sql, DynamicParameters Params) Build(string proc, object? param)
    {
        var dp = new DynamicParameters();
        var args = new List<string>();

        if (param is not null)
        {
            foreach (var prop in param.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var name = prop.Name;
                var value = prop.GetValue(param);
                args.Add($"p_{name.ToLowerInvariant()} => @{name}");

                switch (value)
                {
                    // timestamp sin zona horaria (las columnas son "timestamp")
                    case DateTime dt:
                        dp.Add(name, DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), DbType.DateTime2);
                        break;
                    case TimeSpan ts:
                        dp.Add(name, ts, DbType.Time);
                        break;
                    default:
                        dp.Add(name, value);
                        break;
                }
            }
        }

        var sql = $"SELECT * FROM {proc.ToLowerInvariant()}({string.Join(", ", args)})";
        return (sql, dp);
    }

    /// <summary>Funciones que no devuelven datos (INSERT/UPDATE/DELETE).</summary>
    public static Task<int> ExecProcAsync(this IDbConnection conn, string proc, object? param = null)
    {
        var (sql, p) = Build(proc, param);
        return conn.ExecuteAsync(sql, p);
    }

    /// <summary>Lista de filas (reemplaza QueryAsync con CommandType.StoredProcedure).</summary>
    public static Task<IEnumerable<T>> QueryProcAsync<T>(this IDbConnection conn, string proc, object? param = null)
    {
        var (sql, p) = Build(proc, param);
        return conn.QueryAsync<T>(sql, p);
    }

    public static Task<T?> QueryFirstOrDefaultProcAsync<T>(this IDbConnection conn, string proc, object? param = null)
    {
        var (sql, p) = Build(proc, param);
        return conn.QueryFirstOrDefaultAsync<T>(sql, p);
    }

    /// <summary>
    /// Para los SP que tenían un parámetro OUTPUT: la función devuelve ese valor
    /// como columna (por ejemplo idDocumentoNuevo). Reemplaza ParameterDirection.Output.
    /// </summary>
    public static async Task<T> ScalarProcAsync<T>(this IDbConnection conn, string proc, object? param = null)
    {
        var (sql, p) = Build(proc, param);
        return (await conn.ExecuteScalarAsync<T>(sql, p))!;
    }
}
