# ADR 0017 - Docker Compose para Serviços de Apoio em Desenvolvimento Local

## Status
Aceito

## Contexto
O ecossistema EstrelaClick depende de serviços de infraestrutura para sua execução completa:
1. **Banco de Dados Relacional & Identity:** Microsoft SQL Server 2022 para persistência de entidades (`Empresa`, `Placa`, `AcessoMetrica`), usuários e migrações do EF Core 10.
2. **Cache Distribuído de Baixa Latência:** Redis 7 para suporte ao HybridCache (.NET 10) de nível L2 no motor de redirecionamento (`GET /r/{slug}`), garantindo tempos de resposta sub-25ms e resiliência em cenários de alta concorrência.

Exigir a instalação manual desses serviços diretamente no sistema operacional do desenvolvedor gera atrito de onboarding, inconsistências de versão entre ambientes (Windows, Linux, macOS) e risco de poluição de portas e serviços no host.

## Decisão
1. **Docker Compose na Raiz da Solução (`docker-compose.yml`):**
   - Configurado arquivo `docker-compose.yml` declarativo na raiz do repositório, focado exclusivamente nos serviços de infraestrutura/terceiros necessários para desenvolvimento local.
   - Aplicações .NET (API, Admin Dashboard, Client Dashboard e MAUI) continuam executando diretamente via SDK no host para suportar Hot Reload, depuração interativa e ciclos rápidos de desenvolvimento.

2. **Serviços Provisionados:**
   - **`sqlserver`:**
     - Imagem: `mcr.microsoft.com/mssql/server:2022-latest`
     - Porta exposta: `1433:1433`
     - Edição: `Developer` (`MSSQL_PID=Developer`)
     - Senha `sa`: Parametrizada via variável `${MSSQL_SA_PASSWORD:-Your_strong_password123}`, com fallback compatível com o `.env.example`.
     - Healthcheck integrado: Executa `sqlcmd` com suporte às ferramentas modernas (`mssql-tools18` com `-C` para TrustServerCertificate) e fallback para `mssql-tools` legado.
     - Volume persistente: `sqlserver_data` mapeado em `/var/opt/mssql`.
   - **`redis`:**
     - Imagem: `redis:7-alpine` (enxuta e veloz)
     - Porta exposta: `6379:6379`
     - Persistência AOF: Executa com `--appendonly yes`.
     - Healthcheck integrado: Executa `redis-cli ping`.
     - Volume persistente: `redis_data` mapeado em `/data`.

3. **Integração com o Ecossistema de Variáveis (`.env`):**
   - O Docker Compose consome automaticamente as variáveis do arquivo `.env` localizado na raiz do projeto.
   - Atualizados os arquivos `.env.example` e `.env` com as chaves `MSSQL_SA_PASSWORD` e `Redis__ConnectionString` para total sincronia entre contêineres e aplicações .NET.

4. **Isolamento de Rede e Persistência:**
   - Declarada a rede bridge `estrelaclick-network` com nome `estrelaclick_dev_network` para comunicação isolada.
   - Volumes nomeados independentes (`estrelaclick_sqlserver_data` e `estrelaclick_redis_data`) garantem que os dados não sejam perdidos entre reinicializações dos contêineres (`docker compose down` / `up`).

## Consequências
- **Setup em Comando Único:** O ambiente de serviços terceiros pode ser inicializado com um simples `docker compose up -d`.
- **Reprodutibilidade:** Garantia de paridade de versões entre máquinas de desenvolvimento e agentes de CI.
- **Isolamento de Host:** Nenhuma necessidade de instalar instâncias locais de SQL Server ou Redis diretamente no Windows/Linux/macOS.
- **Conformidade com o AGENTS.md:** Respeito integral às regras de documentação viva, ADRs e configuração desacoplada.
