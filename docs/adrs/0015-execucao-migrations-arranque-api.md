# ADR 0015 - Execução Automática de Migrações do EF Core no Arranque da API

## Status
Aceito

## Contexto
O ecossistema EstrelaClick adota um monólito modular com persistência em SQL Server gerenciada pelo Entity Framework Core 10. Em ambientes de desenvolvimento, testes de integração e implantação contínua em nuvem/containers, a sincronização manual de migrações (`dotnet ef database update`) pode gerar descompasso entre o código em execução e o esquema relacional, causando falhas silenciosas ou retrabalho operacional.

Fazia-se necessário garantir que, durante a inicialização (startup/arranque) da aplicação Backend Web API, as migrações pendentes sejam verificadas e aplicadas de forma segura, determinística e observável antes que a API comece a receber tráfego HTTP.

## Decisão
1. **Método de Extensão em Infrastructure:**
   - Criado o método de extensão `ApplyDatabaseMigrationsAsync(this IHost host, CancellationToken cancellationToken = default)` na camada `Infrastructure` (`EstrelaClick.Backend.Infrastructure.Persistence.DatabaseMigrationExtensions`).
   - O método obtém o serviço `EstrelaClickDbContext` através de um escopo isolado de injeção de dependência (`CreateScope`).
   - Verifica se existem migrações pendentes via `dbContext.Database.GetPendingMigrationsAsync()`. Se existirem, executa `dbContext.Database.MigrateAsync()`.
   - Inclui logs estruturados em cada etapa (início da verificação, lista de migrations pendentes, conclusão com sucesso ou falha crítica com `LogCritical`).

2. **Chamada no Bootstrap da Web API (`Program.cs`):**
   - No `src/Backend/Api/Program.cs`, logo após a criação do `app` (`builder.Build()`) e antes da configuração do pipeline de requisições e de `app.Run()`, é invocado:
     ```csharp
     await app.ApplyDatabaseMigrationsAsync();
     ```
   - Garante que antes da primeira requisição HTTP (redirecionador, endpoints de fábrica, onboarding de clientes), o banco já possui todas as tabelas, índices e schemas aplicados.

3. **Chave de Configuração para Controle Granular:**
   - Adicionada a chave `Database:AutoMigrate` no `appsettings.json`, com valor padrão `true`.
   - Permite desabilitar a execução automática (ex.: em ambientes corporativos com migração estrita via pipeline de CI/CD ou DBAs) definindo `Database:AutoMigrate = false`.

4. **Garantia por Testes de Integração (TDD):**
   - Criada a suíte `DatabaseMigrationStartupTests` em `tests/IntegrationTests/Persistence/` utilizando `Testcontainers.MsSql` e `WebApplicationFactory<Program>`.
   - Valida que ao iniciar a API contra um banco vazio, as tabelas de domínio (`Empresas`, `Placas`, `AcessosMetricas`) e do Identity (`AspNetUsers`) são criadas sem necessidade de migração manual.
   - Valida que ao definir `Database:AutoMigrate = false`, nenhuma migração é executada no arranque.

## Consequências
- **Consistência Automática:** Ambientes locais e contêineres de homologação/produção inicializam sempre com o esquema de banco perfeitamente alinhado com a versão da aplicação.
- **Fail Fast:** Caso ocorra falha de conectividade ou conflito de migration com o banco, a aplicação aborta a inicialização no arranque com stacktrace detalhado no log, impedindo que requisições sejam processadas em um banco em estado inconsistente.
- **Respeito à Clean Architecture:** A camada de API apenas consome o método de extensão fornecido pela Infrastructure; a API não referencia detalhes internos de provedores de dados.
- **Flexibilidade Operacional:** Ambientes que exigem scripts SQL manuais podem desativar a auto-migração via variável de ambiente `Database__AutoMigrate=false`.
