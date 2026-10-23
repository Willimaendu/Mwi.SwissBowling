using System.Data.Common;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Mwi.SwissBowling.Core.Persistence;

public static class SchemaDeployment
{
    public static async Task ApplyAsync(DbContext database, Assembly assembly, string resourcePrefix, CancellationToken cancellationToken = default)
    {
        var connection = database.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await ExecuteAsync(connection, transaction, "SELECT pg_advisory_xact_lock(834179025);", cancellationToken);
            await ExecuteAsync(connection, transaction, "CREATE TABLE IF NOT EXISTS \"SchemaVersions\" (\"Name\" text PRIMARY KEY, \"Checksum\" text NOT NULL, \"AppliedAt\" timestamptz NOT NULL);", cancellationToken);
            foreach (var resource in assembly.GetManifestResourceNames().Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal) && name.EndsWith(".sql", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
            {
                await using var stream = assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException("Schema resource is missing.");
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var sql = await reader.ReadToEndAsync(cancellationToken);
                var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
                await using var lookup = connection.CreateCommand();
                lookup.Transaction = transaction;
                lookup.CommandText = "SELECT \"Checksum\" FROM \"SchemaVersions\" WHERE \"Name\" = @name";
                AddParameter(lookup, "name", resource);
                var previous = await lookup.ExecuteScalarAsync(cancellationToken) as string;
                if (previous is not null)
                {
                    if (!string.Equals(previous, checksum, StringComparison.Ordinal))
                        throw new InvalidOperationException("An applied schema migration was modified. Add a new migration instead.");
                    continue;
                }
                await ExecuteAsync(connection, transaction, sql, cancellationToken);
                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO \"SchemaVersions\" (\"Name\", \"Checksum\", \"AppliedAt\") VALUES (@name, @checksum, now())";
                AddParameter(insert, "name", resource);
                AddParameter(insert, "checksum", checksum);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        finally { await connection.CloseAsync(); }
    }

    private static async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
