using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace VRLCRM.Infrastructure.Data;

internal static class DatabaseConnection
{
    public static string Build(IConfiguration configuration)
    {
        var password = configuration["Database:Password"]
            ?? configuration["MSSQL_SA_PASSWORD"];

        if (!string.IsNullOrWhiteSpace(password))
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = configuration["Database:Server"] ?? "localhost,1433",
                InitialCatalog = configuration["Database:Name"] ?? "VRLCRM",
                UserID = configuration["Database:User"] ?? "sa",
                Password = password,
                TrustServerCertificate = true,
                Encrypt = false
            };

            return builder.ConnectionString;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database password is not configured. Set MSSQL_SA_PASSWORD or Database:Password in environment.");
        }

        return connectionString;
    }
}
