# ROADMAP — Plataforma de Placas Inteligentes (NFC & QR Code)

Este documento estabelece o roteiro completo de desenvolvimento, validação de hardware, arquitetura de software e homologação para entrada em produção da plataforma.

---

## Sumário das Fases
- [Fase 1: Arquitetura, Core Domain e Persistência](#fase-1-arquitetura-core-domain-e-persistência)
- [Fase 1.4: Migração de PostgreSQL para SQL Server](#fase-14-migração-de-postgresql-para-sql-server)
- [Fase 2: Backend e Redirecionador de Alta Performance](#fase-2-backend-e-redirecionador-de-alta-performance)
- [Fase 3: Aplicativo de Bancada Mobile (.NET MAUI)](#fase-3-aplicativo-de-bancada-mobile-net-maui)
- [Fase 4: Painéis Web Segregados (Blazor Server)](#fase-4-painéis-web-segregados-blazor-server)
- [Fase 5: Hardware, Homologação e Entrada em Produção](#fase-5-hardware-homologação-e-entrada-em-produção)

---

## Fase 1: Arquitetura, Core Domain e Persistência
**Objetivo:** Estruturar a solução modular, definir os contratos de domínio, implementar o padrão funcional `Result<T>` e configurar o banco relacional com Entity Framework Core 10.

### 1.1 Configuração da Solução .NET 10
- [x] Criar a Solution `EstrelaClick.slnx` com estrutura em monólito modular.
- [x] Configurar os projetos da camada Backend seguindo Clean Architecture / Hexagonal:
  - `Domain` (Entidades puras, Value Objects, contratos de domínio).
  - `Application` (Casos de uso, portas Inbound e Outbound).
	- `Infrastructure` (EF Core 10, SQL Server, Identity Core, QRCoder, Telemetria).
  - `Api` (Minimal APIs, Scalar OpenAPI, rotas de redirecionamento).
- [x] Configurar o projeto `SharedKernel` compartilhado entre Backend, Frontends e App Mobile.
- [x] Criar projetos de teste unitário (`xUnit`, `FluentAssertions`, `NetArchTest`).

### 1.2 Modelagem de Entidades e Validações Invariantes
- [x] Implementar classe canônica `Result`/`Result<T>` e struct `Error` no `SharedKernel`.
- [x] Criar entidade `Empresa`:
  - Propriedades: `Id`, `RazaoSocial`, `Email`, `Telefone`, `StatusAtivo`, `DataCadastroUtc`.
	- Integração com ASP.NET Core Identity Core para credenciais de acesso (**contrato de domínio com `IdentityUserId`; integração técnica ficará na fase de Infrastructure/Identity**).
- [x] Criar entidade `Placa`:
  - Propriedades: `Id`, `Slug`, `UrlDestino`, `StatusPlaca` (`Criada`, `Gravada`, `Vinculada`), `ChipUid`, `DataCriacaoUtc`, `DataGravacaoUtc`, `DataAtivacaoUtc`, `EmpresaId`.
  - Métodos de negócio: `Criar()`, `RegistrarGravacao(chipUid)`, `VincularEmpresa(empresaId, urlDestino)`.
- [x] Criar entidade `AcessoMetrica`:
  - Propriedades: `Id`, `PlacaId`, `DataHoraUtc`, `UserAgent`, `IpHash`, `OrigemReferer`.

### 1.3 Mapeamento EF Core 10 e Migrations
- [x] Configurar mapeamentos explícitos via Fluent API (`IEntityTypeConfiguration<T>`).
- [x] Criar índice único obrigatório em `Placa.Slug` (`B-Tree`) para buscas em tempo constante $O(1)$.
- [x] Criar índice em `AcessoMetrica.PlacaId` e `AcessoMetrica.DataHoraUtc` para relatórios agregados.
- [x] Gerar e validar a migration inicial (`InitialCreate`).

### Fase 1.4: Migração de PostgreSQL para SQL Server
**Objetivo:** Migrar a persistência para SQL Server e eliminar dependências PostgreSQL do código, testes e documentação.

- [ ] Substituir provider `Npgsql.EntityFrameworkCore.PostgreSQL` por `Microsoft.EntityFrameworkCore.SqlServer` na camada `Infrastructure`.
- [ ] Trocar `UseNpgsql(...)` por `UseSqlServer(...)` em `DependencyInjection` e `DbContextFactory`.
- [ ] Atualizar `ConnectionStrings:DefaultConnection` para padrão SQL Server.
- [ ] Regenerar migrations/snapshot sem referências `Npgsql` em `src/Backend/Infrastructure/Persistence/Migrations/`.
- [ ] Migrar testes de integração de `Testcontainers.PostgreSql` para contêiner SQL Server.
- [ ] Reescrever SQL de validação de schema/índices atualmente acoplado a `information_schema` e `pg_indexes`.
- [ ] Atualizar pipelines e ambientes para provisionamento SQL Server gerenciado.
- [ ] Validar remoção completa das referências `Npgsql`, `UseNpgsql` e `Testcontainers.PostgreSql`.

---

## Fase 2: Backend e Redirecionador de Alta Performance
**Objetivo:** Implementar o endpoint de ultra baixa latência ($< 25\text{ ms}$), telemetria desacoplada, motor de geração de QR Codes e gestão da fila de gravação.

### 2.1 Motor de Redirecionamento Ultrarrápido (`GET /r/{slug}`)
- [x] Implementar o endpoint de Minimal API `GET /r/{slug}`.
- [x] Configurar cache em memória de dois níveis (`HybridCache` .NET 10 prioritário / chave `slug:{slug}`).
- [x] Implementar barramento interno não-bloqueante usando `System.Threading.Channels.Channel<MetricaAcessoEvent>`.
- [x] Criar worker em segundo plano (`BackgroundService`) para despejo em lote (*batch insert*) de métricas no banco de dados.
- [x] Retornar `Results.Redirect(urlDestino, permanent: false)` (HTTP 302).

### 2.2 Motor de Geração de QR Code
- [x] Integrar biblioteca `QRCoder`.
- [x] Criar serviço com saída em PNG de alta definição (300 DPI) para envio a gráficas.
- [x] Criar serviço com saída vetorial em SVG para corte e gravação a laser.
- [x] Adicionar suporte para exportação em lote compactada (.zip) por número de lote.

### 2.3 API de Lotes e Controle de Fila para App Mobile
- [x] Endpoint `POST /api/placas/gerar-lote`: geração em massa de placas com slugs aleatórios seguros.
- [x] Endpoint `GET /api/placas/proxima-pendente`: busca a próxima placa com status `Criada` para o operador de bancada.
- [x] Endpoint `POST /api/placas/confirmar-gravacao`: recebe slug e UID do chip lido, efetuando a transição para `Gravada`.
- [x] Proteger endpoints da fábrica com chave de API interna e JWT.
- [x] Configurar e documentar rotas via OpenAPI nativo com interface interativa **Scalar** em `/scalar/v1`.

---

## Fase 3: Aplicativo de Bancada Mobile (.NET MAUI)
**Objetivo:** Construir o aplicativo móvel de uso interno para gravação seriada de tags NFC (NTAG213 e NTAG215) em ritmo de produção industrial.

### 3.1 Configurações Nativas e Permissões
- [x] Configurar `AndroidManifest.xml` com `<uses-permission android:name="android.permission.NFC" />` e recurso `<uses-feature android:name="android.hardware.nfc" android:required="true" />`.
- [x] Implementar serviços nativos de gravação no Android (`NfcAdapter`, `Ndef`, `NdefRecord`).
- [x] Configurar intents em primeiro plano (*Foreground Dispatch*) para captura contínua sem fechar o app.

### 3.2 Interface do Operador de Bancada
- [x] Tela de autenticação do operador integrada à API.
- [x] Painel com métricas de produção da bancada:
  - Total do lote.
  - Quantidade gravada na sessão.
  - Identificador e URL da placa ativa.
- [x] Botão de alternância para modo "Gravação Contínua".

### 3.3 Ciclo Operacional de Gravação NDEF
- [x] Buscar automaticamente a próxima placa livre na fila (`GET /api/placas/proxima-pendente`).
- [x] Montar registro NDEF do tipo URI com prefixo `https://` (`0x04`).
- [x] Escrever mensagem NDEF no chip por indução magnética.
- [x] Capturar e validar o UID físico do chip NFC.
- [x] Disparar feedback tátil (vibração) e sonoro (bipe de sucesso).
- [x] Confirmar gravação na API (`POST /api/placas/confirmar-gravacao`) e avançar automaticamente para a próxima tag.
- [x] Tratamento de falhas: detecção de tag protegida contra gravação ou perda prematura de contato.

---

## Fase 4: Painéis Web Segregados (Blazor Server)
**Objetivo:** Criar os portais administrativos internos e do cliente lojista, isolados em projetos distintos e estilizados com Design Tokens centralizados.

### 4.1 Biblioteca Compartilhada de UI (`Shared.UI`)
- [x] Criar catálogo de tokens de design em `tokens.css`:
  - Cores (primária, suporte, estados de sucesso/erro/alerta) com suporte nativo a Dark Mode.
  - Escalas de espaçamento (`--space-2xs` a `--space-2xl`).
  - Raios de borda e sombras padronizadas.
- [x] Construir componentes reutilizáveis:
  - `PlacaStatusBadge.razor` (indicador de status da placa).
  - Modais de confirmação (`ConfirmationModal.razor`) e formulários desacoplados (`FormInputText.razor`).
  - Componentes de tabela com paginação e busca rápida (`DataTable<TItem>.razor`, `PaginationControl.razor`, `EmptyState.razor`, `LoadingSpinner.razor`).
  - Cartão de KPIs e Métricas (`MetricCard.razor`).

### 4.2 Dashboard de Gestão Interna (`AdminDashboard`)
- [x] Painel gerencial da fábrica: total de placas criadas, gravadas em estoque e ativadas em clientes.
- [x] Módulo de geração de lotes em massa (especificação de quantidade e prefixo).
- [x] Módulo de exportação de lotes para impressão (download unificado de imagens em alta definição e arquivos vetoriais para corte a laser).
- [x] Gerenciamento de clientes e consulta de auditoria por chip UID ou slug.

### 4.3 Dashboard do Cliente / Lojista (`ClientDashboard`)
- [x] **Fluxo de Onboarding / Primeiro Acesso:**
  - O lojista aproxima o celular da placa virgem ou lê o QR Code.
  - A plataforma detecta status `Gravada` (sem vínculo) e redireciona para a tela de registro/associação.
  - Vinculação instantânea da placa à conta criada.
- [x] **Painel de Configurações:**
  - Edição dinâmica da URL de destino (link do Google Meu Negócio, WhatsApp, cardápio digital ou catálogo).
- [x] **Painel de Analytics:**
  - Gráficos de toques por período (visão diária, semanal e mensal).
  - Distribuição estimada por tipo de dispositivo e horários de pico.
- [x] *(Opcional)* Módulo de filtro de satisfação (redirecionamento de notas 4-5 para o Google e 1-3 para SAC interno).

---

## Fase 5: Hardware, Homologação e Entrada em Produção
**Objetivo:** Validar os insumos físicos, conduzir testes ponta a ponta e implantar a infraestrutura de produção com alta disponibilidade.

### 5.1 Homologação de Hardware e Suprimentos
- [ ] Homologar adesivos **NTAG213** (144 bytes, ideal para URLs curtas) e **NTAG215** (504 bytes).
- [ ] Testar limites de proximidade e leitura com diferentes modelos de smartphones (Android e iOS).
- [ ] Validar comportamento físico em suportes de **acrílico**, **madeira** e superfícies metálicas (aplicação de tags com camada protetora de ferrite/anti-metal).
- [ ] Testar resistência de impressão UV contra desbotamento e atrito de bancada.

### 5.2 Infraestrutura e Deploy Contínuo
- [ ] Empacotar os serviços em contêineres Docker multi-stage otimizados para .NET 10 Linux.
- [ ] Configurar banco de dados gerenciado SQL Server com alta disponibilidade e rotina de backup contínuo.
- [ ] Registrar domínio institucional curto para redirecionamentos (ex.: `marca.link`).
- [ ] Configurar terminação TLS/SSL obrigatória com certificados Let's Encrypt / Cloudflare (tags NFC exigem HTTPS estrito para leitura automática sem telas de advertência).
- [ ] Configurar observabilidade e telemetria de erros (OpenTelemetry / Serilog / Prometheus).

### 5.3 Validação Ponta a Ponta Pré-Lançamento
- [ ] Gerar lote de teste com 10 placas no `AdminDashboard`.
- [ ] Executar gravação das 10 tags físicas via app .NET MAUI em bancada.
- [ ] Imprimir QR Codes em escala real e montar protótipos de acrílico.
- [ ] Executar o fluxo completo de onboarding pelo celular de um lojista fictício.
- [ ] Efetuar toques sucessivos por múltiplos aparelhos e validar a precisão dos registros na fila assíncrona de métricas.