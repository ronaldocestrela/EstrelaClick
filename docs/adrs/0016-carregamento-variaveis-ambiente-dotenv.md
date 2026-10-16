# ADR 0016 - Carregamento de Variáveis de Ambiente via Arquivo .env

## Status
Aceito

## Contexto
O ecossistema EstrelaClick é composto por múltiplos projetos (Backend Web API, AdminDashboard em Blazor Server, ClientDashboard em Blazor Server e ferramentas móveis/testes). Cada aplicação requer configurações sensíveis e parametrizáveis, tais como strings de conexão do SQL Server, segredos JWT de bancada, chaves de API industriais (`X-Api-Key`), salts de anonimização de IP (LGPD) e URLs base de redirecionamento e microsserviços.

Anteriormente, tais configurações estavam dispersas em arquivos `appsettings.json` ou dependiam de variáveis de ambiente configuradas manualmente no sistema operacional do desenvolvedor. Para simplificar a experiência de desenvolvimento local (DevEx), padronizar implantações com contêineres Docker e alinhar o projeto às boas práticas do Twelve-Factor App (seção III: Configuração), fez-se necessário estabelecer um mecanismo nativo, resiliente e seguro para fornecimento de variáveis de ambiente através de um arquivo `.env`.

## Decisão
1. **Utilitário Centralizado no SharedKernel (`DotEnv`):**
   - Implementada a classe estática `DotEnv` no namespace `EstrelaClick.Shared.SharedKernel.Configuration`.
   - Como `SharedKernel` já é referenciado por todos os projetos da solution (`Api`, `AdminDashboard`, `ClientDashboard`, `Infrastructure`, `UnitTests`), nenhuma dependência de terceiros ou acoplamento externo foi introduzido.
   - Retornos estritamente padronizados com o padrão funcional `Result<T>` (`Result<int>`, `Result<IReadOnlyDictionary<string, string>>`), eliminando exceções de fluxo de controle conforme a regra 3 do `AGENTS.md`.

2. **Funcionalidades do Carregador (`DotEnv.Load`):**
   - **Busca Recursiva Ascendente:** Se nenhum caminho for especificado, procura pelo arquivo `.env` no diretório de execução atual e sobe recursivamente pelos diretórios ancestrais (até 10 níveis), localizando o `.env` na raiz do repositório mesmo quando o projeto é executado de subpastas (ex.: `dotnet run --project src/Backend/Api`).
   - **Parsing Robusto:** Suporta comentários com `#`, remoção de prefixo `export `, quebras de linha e tabulações escapadas (`\n`, `\t`), valores envolvidos por aspas simples ou duplas, e valores contendo múltiplos caracteres `=`.
   - **Interoperabilidade com ASP.NET Core Configuration:** Ao injetar variáveis no processo (`Environment.SetEnvironmentVariable`), mapeia chaves com `:` para o formato com duplo sublinhado `__` (ex.: `ConnectionStrings:DefaultConnection` -> `ConnectionStrings__DefaultConnection`) e vice-versa, garantindo que o `Microsoft.Extensions.Configuration` do .NET mapeie as seções hierárquicas perfeitamente.
   - **Preservação de Variáveis Existentes:** Não sobrescreve variáveis de ambiente já definidas no ambiente de produção/contêiner a menos que explicitamente solicitado via `overrideExisting: true`.

3. **Integração no Arranque das Aplicações (`Program.cs`):**
   - Adicionada a chamada `DotEnv.Load();` na primeira linha de execução dos seguintes pontos de entrada:
     - `src/Backend/Api/Program.cs`
     - `src/Frontend/AdminDashboard/Program.cs`
     - `src/Frontend/ClientDashboard/Program.cs`
   - O `WebApplication.CreateBuilder(args)` do ASP.NET Core automaticamente lê todas as variáveis de ambiente carregadas no processo pelo `DotEnv`.

4. **Templates e Segurança (.gitignore):**
   - Criado o arquivo modelo `.env.example` na raiz do repositório contendo todas as variáveis necessárias devidamente documentadas com valores de exemplo seguros.
   - Criado o arquivo `.env` local para desenvolvimento imediato.
   - O arquivo `.gitignore` já isola arquivos `.env` e `.env.*` (exceto `.env.example`), impedindo o vazamento inadvertido de credenciais reais no controle de versão.

5. **Validação por TDD:**
   - Criada a suíte `DotEnvTests` em `tests/UnitTests/SharedKernel/Configuration/` cobrindo 100% dos cenários de parsing, escape, normalização de chaves, busca ascendente e comportamento de override.

## Consequências
- **Portabilidade:** Desenvolvedores podem clonar o repositório, copiar `.env.example` para `.env` e executar toda a solução localmente sem configurações manuais de sistema operacional.
- **Segurança Reforçada:** Segredos e chaves de produção ficam desacoplados do código-fonte e dos arquivos `appsettings.json`.
- **Zero Dependências:** Implementação enxuta em C# 14 / .NET 10 sem risco de vulnerabilidades em pacotes externos transitivos.
- **Conformidade com o AGENTS.md:** Respeito absoluto ao TDD, ADR viva, padrão Result<T> e localização em biblioteca compartilhada (`SharedKernel`).
