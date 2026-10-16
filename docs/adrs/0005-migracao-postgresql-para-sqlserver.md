# ADR 0005 - Migração de PostgreSQL para SQL Server

## Status
Aceito

## Contexto
A solução foi iniciada com `Npgsql`/PostgreSQL na camada de persistência. A direção atual do projeto é padronizar o banco relacional em SQL Server, removendo dependências de PostgreSQL no código, testes, pipelines e documentação.

## Decisão
Executar a migração para SQL Server em fases, com remoção completa dos acoplamentos PostgreSQL ao final:

1. **Preparação da infraestrutura de persistência**
   - Trocar provider EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL` -> `Microsoft.EntityFrameworkCore.SqlServer`).
   - Substituir `UseNpgsql(...)` por `UseSqlServer(...)` em registro de DI e factory de design-time.
   - Atualizar connection strings para formato SQL Server.

2. **Migrations e modelo**
   - Gerar baseline de migrations para SQL Server no projeto de `Infrastructure`.
   - Eliminar artefatos de migration/snapshot dependentes de `Npgsql`.
   - Revisar mapeamentos para tipos e comportamentos específicos de provider.

3. **Testes de integração**
   - Migrar `Testcontainers.PostgreSql` para contêiner SQL Server.
   - Trocar SQLs de inspeção específicos de PostgreSQL por consultas compatíveis com SQL Server.
   - Manter validações de schema, tabelas e índices obrigatórios.

4. **Entrega e operação**
   - Atualizar pipelines/ambientes para provisionamento SQL Server.
   - Revisar scripts de deploy, backup/restore e observabilidade.
   - Validar rollout com ambiente de homologação e smoke tests.

## Checklist de remoção de dependências PostgreSQL
- [ ] Remover pacote `Npgsql.EntityFrameworkCore.PostgreSQL` de `src/Backend/Infrastructure/EstrelaClick.Backend.Infrastructure.csproj`.
- [ ] Remover pacote `Npgsql` de `tests/IntegrationTests/EstrelaClick.Tests.IntegrationTests.csproj`.
- [ ] Remover pacote `Testcontainers.PostgreSql` de `tests/IntegrationTests/EstrelaClick.Tests.IntegrationTests.csproj`.
- [ ] Substituir `UseNpgsql` em:
  - `src/Backend/Infrastructure/DependencyInjection.cs`
  - `src/Backend/Infrastructure/Persistence/EstrelaClickDbContextFactory.cs`
- [ ] Ajustar `DefaultConnection` em `src/Backend/Api/appsettings.json` para SQL Server.
- [ ] Reescrever `tests/IntegrationTests/Persistence/InitialCreateMigrationTests.cs` para SQL Server.
- [ ] Regenerar migrations/snapshot em `src/Backend/Infrastructure/Persistence/Migrations/` sem referências Npgsql.
- [ ] Atualizar documentação (`README.md`, `inidial_roadmap.md`, `AGENTS.md`) removendo PostgreSQL como alvo.

## Consequências
- A stack de persistência fica alinhada ao novo padrão em SQL Server.
- A migração exige revisão de migrations e testes de integração, com impacto direto em CI.
- O corte final só é concluído quando não existirem referências a `Npgsql`, `UseNpgsql` e `Testcontainers.PostgreSql` no repositório (exceto histórico de ADRs antigos).
