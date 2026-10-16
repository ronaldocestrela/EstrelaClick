# ADR 0004 - Persistência EF Core 10 com Identity e Migration Inicial

## Status
Superado pelo ADR 0005

## Contexto
A fase 1.3 do roadmap exige a implementação da persistência relacional com EF Core 10 para o domínio (`Empresa`, `Placa`, `AcessoMetrica`), inclusão de tabelas de ASP.NET Core Identity, criação de índices obrigatórios e geração/validação da migration inicial em PostgreSQL.

> Histórico: este ADR representa a decisão inicial com PostgreSQL. A direção atual de arquitetura foi revisada para SQL Server no `ADR 0005`.

## Decisão
- Adotar `EstrelaClickDbContext` na camada `Infrastructure`, herdando de `IdentityDbContext<IdentityUser, IdentityRole, string>` para manter domínio + Identity no mesmo modelo.
- Definir schema padrão PostgreSQL como `estrelaclick`.
- Implementar mapeamentos explícitos via `IEntityTypeConfiguration<T>` para todas as entidades de domínio.
- Garantir os índices mandatórios:
  - único em `Placa.Slug` (`IX_Placas_Slug`)
  - composto em `AcessosMetricas(PlacaId, DataHoraUtc)` (`IX_AcessosMetricas_PlacaId_DataHoraUtc`)
- Gerar migration `InitialCreate` no projeto `Infrastructure`.
- Validar a migration por teste de integração com `Testcontainers.PostgreSql`, cobrindo schema, tabelas de domínio, tabelas de Identity e índices.

## Consequências
- A camada Domain permanece sem dependência de EF Core.
- A configuração de persistência fica centralizada e explícita na Infrastructure.
- A execução local e em CI pode validar o banco de forma reproduzível via contêiner efêmero.
- Como o provider estável para EF Core 10 ainda não está disponível no NuGet no momento da implementação, foi adotado `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.0-preview.7` com alinhamento de versões EF Core/Identity para a mesma linha preview.
