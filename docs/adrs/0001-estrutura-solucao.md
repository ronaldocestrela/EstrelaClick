# ADR 0001 - Estrutura inicial da solução

## Status
Aceito

## Contexto
A plataforma EstrelaClick exige monólito modular em .NET 10 com separação explícita de camadas (Backend, Frontend, Mobile, Shared e Tests), conforme AGENTS.md e roadmap da Fase 1.1.

## Decisão
Adotar `EstrelaClick.slnx` como solução oficial e organizar os projetos em:
- `src/Backend`: Domain, Application, Infrastructure, Api
- `src/Frontend`: AdminDashboard, ClientDashboard, Shared.UI
- `src/Mobile`: FactoryMobileApp
- `src/Shared`: SharedKernel
- `tests`: UnitTests, IntegrationTests, ArchitectureTests

## Consequências
- Fronteiras arquiteturais ficam explícitas desde o bootstrap.
- Todos os módulos são evoluídos dentro da mesma solução.
- Testes arquiteturais validam dependências permitidas já no início do projeto.
