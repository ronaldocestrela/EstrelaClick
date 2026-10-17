# ADR 0018 - Padronização de Logging Estruturado com Serilog

## Status
Aceito

## Contexto
O ecossistema EstrelaClick processa operações industriais e web de alta criticidade:
1. **Redirecionador de Alta Performance (`/r/{slug}`):** Exige tempos de resposta inferiores a 25 ms com telemetria e rastreamento de acessos via fila sem bloquear a requisição.
2. **Esteira Fabril Mobile NFC:** Comunicação contínua entre o app .NET MAUI e a Web API para reserva, gravação de NDEF e confirmação de placas em lote.
3. **Painéis Administrativos (Admin & Cliente):** Onboarding de placas virgens e analytics analítico para lojistas.

Anteriormente, o backend dependia unicamente do provedor padrão de logging do ASP.NET Core (`Microsoft.Extensions.Logging.Console`), o qual gera saídas em texto plano não estruturadas, sem contextualização de ambiente, máquina, thread ou enriquecimento HTTP padronizado. Além disso, logs excessivamente verbosos de comandos EF Core e do pipeline do ASP.NET poluiam a saída padrão e dificultavam a observabilidade e auditoria operacional em produção.

Conforme a Regra 2 do `AGENTS.md`, toda decisão estrutural deve ser registrada via Architectural Decision Record (ADR) mantida junto ao código.

## Decisão

1. **Adoção do Serilog no Host da Web API (`EstrelaClick.Backend.Api`):**
   - Integrado o pacote `Serilog.AspNetCore` (versão 10.0.0 para .NET 10) e os pacotes de enriquecimento `Serilog.Enrichers.Environment` e `Serilog.Enrichers.Thread`.
   - As camadas internas de negócio (`Domain` e `Application`) permanecem estritamente isoladas e desacopladas de bibliotecas de terceiros, consumindo apenas as abstrações nativas do .NET (`ILogger<T>`).
   - Adicionadas regras formais em `tests/ArchitectureTests/LayeringRulesTests.cs` garantindo por teste automatizado (`NetArchTest`) que `Domain` e `Application` não possuem dependência de assemblies do `Serilog`.

2. **Padrão de Inicialização com Bootstrap Logger:**
   - Implementado `SerilogConfigurationExtensions.CreateBootstrapLogger()` no início do `Program.cs`.
   - Todo o ciclo de vida do host da aplicação é encapsulado em bloco `try / catch / finally` com `Log.CloseAndFlush()`, assegurando captura de exceções fatais prematuras no arranque e liberação ordenada de buffers de arquivo.

3. **Enriquecimento Estruturado e Contextual:**
   - Adicionadas propriedades globais essenciais em todos os eventos:
     - `Application`: Identificador do componente (`EstrelaClick.Backend.Api`).
     - `Environment`: Nome do ambiente de execução (ex.: `Development`, `Production`).
     - `MachineName`: Hostname da máquina / contêiner.
     - `ThreadId`: Identificador da thread de processamento.
     - `FromLogContext`: Escopo contextual dinâmico.

4. **Middleware de Requisições HTTP (`UseSerilogRequestLogging`):**
   - Configurado o middleware de request logging para telemetria estruturada das requisições com o template:
     `"HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000} ms"`.
   - Enriquecimento customizado em `EnrichDiagnosticContext`:
     - `RequestHost`, `RequestScheme`, `TraceIdentifier`.
     - `UserAgent` e `ClientIp`.
     - `UserId` do operador de bancada ou cliente quando a requisição estiver autenticada.

5. **Sinks Padronizados (Console & Rolling File):**
   - **Console:** Saída colorida estruturada otimizada para terminais de desenvolvimento e visualização de contêineres Docker (`docker compose logs -f`).
   - **Arquivo Rotativo (Rolling File):** Armazenamento em `logs/estrelaclick-api-.log` com rotação diária (`RollingInterval.Day`), retenção automática de 14 dias (`retainedFileCountLimit: 14`) e limite de 10 MB por arquivo (`fileSizeLimitBytes: 10485760`).

6. **Hierarquia de Níveis e Configuração Flexível:**
   - Declarada a seção `"Serilog"` nos arquivos `appsettings.json` e `appsettings.Development.json`.
   - Em produção: nível mínimo `Information`, suprimindo logs excessivos de frameworks (`Microsoft.AspNetCore` e `Microsoft.EntityFrameworkCore.Database.Command` em `Warning`).
   - Em desenvolvimento: nível `Debug` e logs do EF Core em `Information` para facilitar diagnóstico de queries e migrações.
   - Suporte completo a substituição via variáveis de ambiente (`.env` / `DotEnv`), documentado no `.env.example`.

7. **Validação por TDD:**
   - Implementada a suíte `SerilogConfigurationTests` em `tests/UnitTests/Api/Logging/` validando a geração da configuração do Serilog e a injeção adequada de propriedades no contexto diagnóstico HTTP.

## Consequências

- **Observabilidade Robusta:** Rastreabilidade ponta a ponta com correlação de traces HTTP, usuários autenticados e latências em milissegundos.
- **Zero Poluição em Produção:** Frameworks ruidosos são filtrados para Warning, preservando os logs de negócio e auditoria.
- **Isolamento Arquitetural Preservado:** Camadas `Domain` e `Application` mantêm total independência e pureza arquitetural.
- **Compatibilidade Docker e Cloud:** Sinks de console stdout e arquivos rotativos prontos para agregação em ferramentas como Seq, OpenTelemetry, Loki ou CloudWatch.
- **Conformidade com o AGENTS.md:** Respeito absoluto a TDD, NetArchTest, isolamento em monólito modular e documentação viva.
