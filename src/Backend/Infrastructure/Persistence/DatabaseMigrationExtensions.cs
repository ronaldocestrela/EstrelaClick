using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EstrelaClick.Backend.Infrastructure.Persistence;

/// <summary>
/// Fornece métodos de extensão para gerenciamento e execução de migrações no arranque da aplicação.
/// </summary>
public static class DatabaseMigrationExtensions
{
    public const string AutoMigrateConfigKey = "Database:AutoMigrate";

    /// <summary>
    /// Verifica e aplica todas as migrações pendentes do banco de dados relacional (SQL Server).
    /// Executa por padrão, mas pode ser desabilitado configurando a chave "Database:AutoMigrate" para "false".
    /// </summary>
    /// <param name="host">Host da aplicação em inicialização.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O mesmo host para permitir encadeamento.</returns>
    public static async Task<IHost> ApplyDatabaseMigrationsAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseMigrationExtensions));

        var autoMigrate = configuration.GetValue(AutoMigrateConfigKey, true);
        if (!autoMigrate)
        {
            logger.LogInformation("Execução automática de migrations no arranque desativada via configuração '{ConfigKey}'.", AutoMigrateConfigKey);
            return host;
        }

        try
        {
            var dbContext = services.GetRequiredService<EstrelaClickDbContext>();

            logger.LogInformation("Iniciando verificação de migrations pendentes no banco de dados...");
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            if (pendingMigrations.Count != 0)
            {
                logger.LogInformation("Aplicando {Count} migration(s) pendente(s): {Migrations}",
                    pendingMigrations.Count,
                    string.Join(", ", pendingMigrations));

                await dbContext.Database.MigrateAsync(cancellationToken);

                logger.LogInformation("Todas as migrações foram aplicadas com sucesso no arranque da aplicação.");
            }
            else
            {
                logger.LogInformation("Nenhuma migração pendente encontrada. O banco de dados já está atualizado.");
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Falha crítica ao aplicar as migrações do banco de dados durante o arranque da aplicação.");
            throw;
        }

        return host;
    }
}
