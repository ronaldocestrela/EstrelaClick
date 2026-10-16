using EstrelaClick.Backend.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace EstrelaClick.Tests.IntegrationTests.Persistence;

public sealed class InitialCreateMigrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Your_strong_password123!")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task InitialCreate_DeveCriarSchemaETabelas_DeDominioEIdentity()
    {
        await AplicarMigrationsAsync();

        await using var connection = new SqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();

        (await ExisteSchemaAsync(connection, "estrelaclick")).Should().BeTrue();

        (await ExisteTabelaAsync(connection, "Empresas")).Should().BeTrue();
        (await ExisteTabelaAsync(connection, "Placas")).Should().BeTrue();
        (await ExisteTabelaAsync(connection, "AcessosMetricas")).Should().BeTrue();

        (await ExisteTabelaAsync(connection, "AspNetUsers")).Should().BeTrue();
        (await ExisteTabelaAsync(connection, "AspNetRoles")).Should().BeTrue();
        (await ExisteTabelaAsync(connection, "AspNetUserRoles")).Should().BeTrue();
    }

    [Fact]
    public async Task InitialCreate_DeveCriarIndicesObrigatorios()
    {
        await AplicarMigrationsAsync();

        await using var connection = new SqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();

        (await ExisteIndiceAsync(connection, "Placas", "IX_Placas_Slug")).Should().BeTrue();
        (await ExisteIndiceAsync(connection, "AcessosMetricas", "IX_AcessosMetricas_PlacaId_DataHoraUtc")).Should().BeTrue();
    }

    private async Task AplicarMigrationsAsync()
    {
        var options = new DbContextOptionsBuilder<EstrelaClickDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        await using var dbContext = new EstrelaClickDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    private static async Task<bool> ExisteSchemaAsync(SqlConnection connection, string schema)
    {
        await using var command = new SqlCommand(
            """
            SELECT CASE WHEN SCHEMA_ID(@schema) IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END
            """,
            connection);

        command.Parameters.AddWithValue("@schema", schema);

        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<bool> ExisteTabelaAsync(SqlConnection connection, string table)
    {
        await using var command = new SqlCommand(
            """
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM sys.tables t
                INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                WHERE s.name = 'estrelaclick'
                  AND t.name = @table
            ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
            """,
            connection);

        command.Parameters.AddWithValue("@table", table);

        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<bool> ExisteIndiceAsync(SqlConnection connection, string table, string index)
    {
        await using var command = new SqlCommand(
            """
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM sys.indexes i
                INNER JOIN sys.tables t ON t.object_id = i.object_id
                INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                WHERE s.name = 'estrelaclick'
                  AND t.name = @table
                  AND i.name = @index
            ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
            """,
            connection);

        command.Parameters.AddWithValue("@table", table);
        command.Parameters.AddWithValue("@index", index);

        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
