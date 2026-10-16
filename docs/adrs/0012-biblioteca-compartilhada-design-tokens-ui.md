# ADR 0012 - Biblioteca Compartilhada de UI (Shared.UI) e Design Tokens com Dark Mode

## Status
Aceito

## Contexto
O item 4.1 do roadmap técnico estabelece a criação da biblioteca compartilhada de componentes visuais (`Shared.UI`) e a padronização estética da plataforma EstrelaClick:
1. **Design Tokens Mandatórios:** O `AGENTS.md` proíbe o uso de cores fixas, espaçamentos ou bordas arbitrárias nas camadas de apresentação. Todas as propriedades visuais devem ser mediadas por tokens CSS estruturados.
2. **Suporte a Dark Mode:** Necessidade de alternância contínua entre temas claro e escuro, respeitando preferências do sistema operacional (`prefers-color-scheme`) e permitindo alternância forçada via atributo (`[data-theme="dark"]`).
3. **Componentização e Reuso (DRY):** Dashboards administrativos internos (`AdminDashboard`) e do lojista (`ClientDashboard`) devem reutilizar componentes comuns como badges de status de placas, tabelas paginadas com busca, modais de confirmação, cartões de métrica e inputs sem duplicação de lógica ou estilos.
4. **Testabilidade (TDD):** Adoção de `bUnit` para assegurar que cada componente Razor seja testado unitariamente para renderização, classes condicionais e disparos de eventos antes de sua integração nos painéis.

## Decisões

1. **Catálogo Canônico de Design Tokens (`tokens.css`):**
   - Centralizado em `src/Frontend/Shared.UI/wwwroot/css/tokens.css`.
   - Organizado em camadas semânticas:
     - Cores base e de marca (`--color-primary`, `--color-accent`, `--color-surface`, `--color-background`, `--color-border`).
     - Estados operacionais (`--color-success`, `--color-warning`, `--color-danger`, `--color-info` e suas respectivas variantes de fundo e texto).
     - Escala tipográfica (`--font-family-base`, `--font-size-xs` a `--font-size-2xl`, `--font-weight-regular/medium/bold`).
     - Escala de espaçamento (`--space-2xs` [4px] a `--space-xl` [32px]).
     - Raios de borda (`--radius-sm`, `--radius-md`, `--radius-lg`, `--radius-full`) e elevações/sombras (`--shadow-sm`, `--shadow-md`, `--shadow-lg`, `--shadow-card`).
   - O Dark Mode é ativado através da sobrescrita transparente das variáveis semânticas de superfície, fundo, texto e bordas sob `@media (prefers-color-scheme: dark)` e seletor `[data-theme="dark"]`.

2. **Indicador Visual de Status (`PlacaStatusBadge.razor`):**
   - Componente acoplado ao enum canônico `StatusPlaca` (`SharedKernel`).
   - Apresenta ícones SVG inline e paleta de cores correspondente:
     - `Criada`: visual neutro/cinza slate;
     - `EmGravacao`: visual azul índigo com indicador animado de pulso;
     - `Gravada`: visual esmeralda/verde de prontidão;
     - `Vinculada`: visual azul royal de placa ativa em cliente.

3. **Diálogos e Modais de Confirmação (`ConfirmationModal.razor`):**
   - Gerenciamento reativo de estado aberto/fechado com `IsOpen`, título, mensagem e suporte a `ChildContent`.
   - Emissão assíncrona de `OnConfirm` e `OnCancel` (`EventCallback`).
   - Tratamento de acessibilidade (`role="dialog"`, `aria-modal="true"`), overlay com efeito de backdrop blur e bloqueio de interação de fundo.

4. **Tabela de Dados Paginada (`DataTable<TItem>.razor` e `PaginationControl.razor`):**
   - Tabela genérica fortemente tipada com slots flexíveis (`HeaderTemplate`, `RowTemplate`, `EmptyContent`).
   - Barra de pesquisa integrada com debounce, acionando o evento `OnSearch`.
   - Estado nativo de carregamento (`IsLoading`) com indicador animado (`LoadingSpinner.razor`).
   - Controle de paginação (`PaginationControl.razor`) com cálculo de páginas, itens por página e disparador `OnPageChanged`.

5. **Cartões de KPI e Métricas (`MetricCard.razor`):**
   - Exibição de estatísticas chave (ex.: "Placas Ativas", "Total Gravado", "Taxa de Cliques") com título, valor principal, badge de variação e slot para ícone visual.

6. **Estratégia de Testes Automatizados com bUnit:**
   - Criação da suíte `tests/UnitTests/UI/Components/` cobrindo o ciclo de vida completo, parâmetros e eventos de cada componente.

## Consequências
- Os painéis Blazor mantêm consistência visual corporativa absoluta e aderência irrestrita ao `AGENTS.md`.
- Troca de tema (Light/Dark) instantânea sem refatoração de código C# ou regras de negócio.
- Eliminação completa de estilos inline e classes arbitrárias nos projetos de front-end.
