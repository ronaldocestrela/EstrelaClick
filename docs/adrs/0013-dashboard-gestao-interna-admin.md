# ADR 0013 - Dashboard de Gestão Interna (AdminDashboard) e Integração com Backend

## Status
Aceito

## Contexto
O item 4.2 do roadmap técnico demanda a implementação do portal administrativo da fábrica e retaguarda da EstrelaClick (`AdminDashboard`). O painel tem como objetivos:
1. Apresentar o painel gerencial da fábrica com KPIs de placas criadas, gravadas em estoque e vinculadas a clientes;
2. Fornecer módulo de geração em massa de novos lotes de placas com especificação de quantidade e prefixo;
3. Oferecer módulo de exportação unificada para impressão (.zip contendo PNG a 300 DPI, SVGs vetoriais e manifesto de conferência);
4. Permitir o gerenciamento de empresas clientes e consulta unificada de auditoria/rastreabilidade por chip UID NFC ou slug de placa.

Além disso, as regras inegociáveis do `AGENTS.md` exigem:
- Segregação de dashboards (aplicações Blazor Server distintas para administração interna e lojistas);
- Limites estritos de camadas (o Blazor Server não referencia EF Core diretamente, comunicando-se através de cliente de API tipado com retorno `Result<T>`);
- Design Tokens estruturados centralizados em `Shared.UI` (`tokens.css`), com suporte transparente a Dark Mode e proibição de classes CSS arbitrárias ou cores hardcoded;
- TDD obrigatório em todas as camadas.

## Decisões

1. **Comunicação Desacoplada com Web API (`AdminApiClient`):**
   - O projeto `AdminDashboard` utiliza um cliente HTTP tipado (`IAdminApiClient` / `AdminApiClient`) para consumir os endpoints da API backend (`/api/admin/...` e `/api/placas/...`).
   - Todos os métodos retornam estritamente `Result<T>`, convertendo respostas de erro ProblemDetails (RFC 7807) em instâncias de `Error`, sem lançamento de exceções.
   - Isso garante independência de deploy entre o front-end web e a API, preservando o modelo de monólito modular.

2. **Autenticação Segura via Cookies e Identity:**
   - O acesso administrativo é protegido por autenticação por cookies ASP.NET Core Identity.
   - Rotas administrativas são resguardadas por `<AuthorizeRouteView>` e componentes de autorização, com página de autenticação corporativa (`Login.razor`).

3. **Extensão dos Serviços de Backend (Application e Api):**
   - Casos de uso específicos para atender aos requisitos gerenciais da fábrica:
     - `ObterMetricasFabricaUseCase`: computa estatísticas consolidadas da fábrica (total de placas criadas, gravadas, vinculadas e contagem de lotes).
     - `ListarLotesPaginadosUseCase`: lista os lotes de placas gerados com totalizadores e paginação.
     - `ConsultarAuditoriaPlacaUseCase`: busca ficha completa da placa por Slug ou Chip UID com histórico de eventos e status.
     - `ListarEmpresasUseCase` e `CadastrarEmpresaUseCase`: gestão do catálogo de clientes lojistas.
   - Mapeamento no endpoint primário `AdminEndpoints.cs` exposto e documentado via Scalar OpenAPI em `/scalar/v1`.

4. **Estilização Canônica com Design Tokens (`tokens.css`):**
   - O layout raiz do `AdminDashboard` elimina dependências de frameworks terceiros arbitrários e adota a arquitetura de tokens de `Shared.UI`.
   - Implementado layout corporativo com Sidebar retrátil, TopBar com alternador dinâmico de Dark/Light Mode e área de conteúdo responsiva.
   - Reutilização dos componentes canônicos: `MetricCard`, `DataTable<TItem>`, `PaginationControl`, `PlacaStatusBadge`, `ConfirmationModal` e `FormInputText`.

5. **Test-Driven Development (TDD):**
   - Casos de uso do backend testados com xUnit e FluentAssertions.
   - Endpoints validados via testes de integração com `WebApplicationFactory`.
   - `AdminApiClient` e componentes Razor testados com bUnit e mocks de mensageria HTTP.

## Consequências
- A retaguarda ganha visão unificada do estoque e ciclo fabril de gravação.
- A esteira de impressão e gravação a laser pode ser acionada diretamente pelos gestores via interface web.
- Auditoria instantânea de tags NFC físicas contra registros no banco de dados.
- Total conformidade com o `AGENTS.md` e manutenibilidade garantida.
