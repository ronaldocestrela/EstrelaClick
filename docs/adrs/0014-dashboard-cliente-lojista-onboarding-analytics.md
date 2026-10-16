# ADR 0014 - Dashboard do Cliente / Lojista (ClientDashboard), Onboarding e Analytics

## Status
Aceito

## Contexto
O item 4.3 do roadmap técnico demanda a implementação da plataforma do cliente/lojista (`ClientDashboard`), que atende aos compradores finais das placas físicas EstrelaClick (lojistas, comércios, restaurantes, clínicas e prestadores de serviços). Os principais requisitos são:
1. **Fluxo de Onboarding / Primeiro Acesso:** Ao tocar pela primeira vez na placa gravada da fábrica (`StatusPlaca.Gravada`) ou escanear seu QR Code, o redirecionador não deve emitir erro 422, e sim encaminhar o lojista para o fluxo de ativação instantânea (`/ativar/{slug}`);
2. **Painel de Configurações de Links:** Permitir a troca dinâmica e imediata da URL de destino da placa com invalidação atômica de cache;
3. **Painel de Analytics:** Apresentar toques agregados por período, horários de pico (00h-23h) e distribuição por sistema operacional/dispositivo;
4. **Módulo de Filtro de Satisfação:** Mecanismo opcional para reter avaliações desfavoráveis (1 a 3 estrelas) em ouvidoria interna e encaminhar 4 a 5 estrelas ao Google Meu Negócio;
5. **Segregação de Dashboards & Limites de Camadas:** Isolamento estrito entre `AdminDashboard` e `ClientDashboard` conforme [AGENTS.md](file:///c:/Users/ronal/source/repos/EstrelaClick/AGENTS.md), operando via Web API com cliente tipado e padrão `Result<T>`.

## Decisões

1. **Redirecionamento Inteligente para Onboarding (`/r/{slug}`):**
   - Quando o status da placa for `StatusPlaca.Gravada` (placa gravada pela fábrica mas sem vínculo de cliente), o endpoint de redirecionamento responde com HTTP 302 para `{ClientDashboardUrl}/ativar/{slug}`.
   - Isso elimina qualquer atrito de configuração física, permitindo que o lojista tire a placa da caixa, encoste seu telefone e conclua o cadastro em menos de 1 minuto.

2. **Ativação e Associação Flexível:**
   - O caso de uso `AtivarPlacaOnboardingUseCase` oferece suporte tanto a novos cadastros (criando usuário Identity e entidade `Empresa`) quanto a clientes já cadastrados (adicionando novas placas ao inventário da mesma empresa).
   - A placa é transicionada para `StatusPlaca.Vinculada`, e o cache é imediatamente populado com a nova URL de destino.

3. **Edição Dinâmica e Invalidação Imediata de Cache:**
   - A entidade `Placa` recebe método de negócio `AtualizarUrlDestino(string novaUrl)` com validação estrita de URI HTTP/HTTPS.
   - O caso de uso `AtualizarUrlPlacaUseCase` persiste a alteração e atualiza o `IPlacaRedirecionamentoCache` para que o próximo toque físico reflita a nova URL em menos de 25 ms.

4. **Motor de Analytics Desacoplado e Agregado:**
   - O caso de uso `ObterAnalyticsClienteUseCase` calcula métricas a partir da tabela `AcessoMetrica`:
     - Série temporal de acessos por dia no período (7, 30 ou 90 dias);
     - Horários de pico (00:00 às 23:00);
     - Distribuição estimada de plataformas (Android, iOS, Desktop/Outros) a partir do cabeçalho `User-Agent`.
   - As consultas são otimizadas para leitura rápida sem onerar o banco de transações.

5. **Módulo Opcional de Filtro de Satisfação:**
   - Configurável por placa via método de negócio `ConfigurarFiltroSatisfacao(bool ativo, string? urlSacInterno)`.
   - Se ativo, o redirecionamento público encaminha para a tela de avaliação prévia (`/feedback/{slug}`). Clientes com nota 4 ou 5 são redirecionados à URL pública (Google Avaliações), enquanto notas 1 a 3 abrem ouvidoria/SAC direto no WhatsApp do lojista.

6. **Comunicação Web API Tipada (`ClientApiClient`) e Design Tokens:**
   - O projeto `ClientDashboard` consome os endpoints `/api/client/...` por meio de `IClientApiClient` / `ClientApiClient`.
   - A interface utiliza os componentes e tokens de `Shared.UI` (`tokens.css`), garantindo suporte nativo a Dark Mode e identidade visual corporativa unificada.

## Consequências
- Experiência de primeiro uso (unboxing) sem fricção para o cliente final.
- Independência completa de deploy entre o portal administrativo e o painel do cliente.
- Preservação estrita das regras inegociáveis de TDD, padrão Result, arquitetura limpa e ausência de dependências diretas de banco no Blazor.
