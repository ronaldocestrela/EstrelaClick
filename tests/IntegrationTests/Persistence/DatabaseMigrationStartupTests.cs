using EstrelaClick.Backend.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace EstrelaClick.Tests.IntegrationTests.Persistence;

public sealed class DatabaseMigrationStartupTests : IAsyncLifetime
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
    public async Task ArranqueDaAplicacao_DeveExecutarMigrationsAutomaticamente_QuandoAutoMigrateNaoForDesabilitado()
    {
        // Arrange: Banco limpo, sem nenhuma tabela previamente criada
        var connectionString = _container.GetConnectionString();

        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        });

        // Act: Dispara a inicialização da aplicação Web API no arranque
        using var client = factory.CreateClient();

        // Assert: As migrações devem ter sido aplicadas automaticamente no arranque
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        (await ExisteSchemaAsync(connection, "estrelaclick")).Should().BeTrue("o schema estrelaclick deve ter sido criado no arranque");
        (await ExisteTabelaAsync(connection, "Empresas")).Should().BeTrue("a tabela Empresas deve existir após arranque");
        (await ExisteTabelaAsync(connection, "Placas")).Should().BeTrue("a tabela Placas deve existir após arranque");
        (await ExisteTabelaAsync(connection, "AcessosMetricas")).Should().BeTrue("a tabela AcessosMetricas deve existir após arranque");
        (await ExisteTabelaAsync(connection, "AspNetUsers")).Should().BeTrue("as tabelas de Identity devem existir após arranque");
    }

    [Fact]
    public async Task ArranqueDaAplicacao_NaoDeveExecutarMigrations_QuandoAutoMigrateForFalse()
    {
        // Arrange: Banco limpo com Database:AutoMigrate = false
        var connectionString = _container.GetConnectionString();

        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            builder.UseSetting(DatabaseMigrationExtensions.AutoMigrateConfigKey, "false");
        });

        // Act: Dispara o arranque com a flag desativada
        using var client = factory.CreateClient();

        // Assert: Nenhuma tabela deve ter sido criada
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        (await ExisteSchemaAsync(connection, "estrelaclick")).Should().BeFalse("o schema não deve ter sido criado quando AutoMigrate for false");
        (await ExisteTabelaAsync(connection, "Placas")).Should().BeFalse("a tabela Placas não deve ter sido criada quando AutoMigrate for false");
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
}
