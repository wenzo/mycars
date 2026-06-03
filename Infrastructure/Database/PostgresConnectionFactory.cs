using Npgsql;

namespace MyCars.Infrastructure.Database;

public sealed class PostgresConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresConnectionFactory(IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("SupabaseDb");

        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "ConnectionStrings:SupabaseDb non configurata. " +
                "Imposta il valore via User Secrets:\n" +
                "  dotnet user-secrets set \"ConnectionStrings:SupabaseDb\" \"<connection string>\"");

        var builder = new NpgsqlDataSourceBuilder(cs);

        // Permette a Npgsql di leggere i tipi PostgreSQL non mappati (enum di dominio)
        // come stringhe, senza richiedere mappature esplicite.
        builder.EnableUnmappedTypes();

        _dataSource = builder.Build();
    }

    public IDbConnection CreateConnection() => _dataSource.CreateConnection();
}
