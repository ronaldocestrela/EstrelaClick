# 🌟 EstrelaClick — Plataforma Integrada de Placas Inteligentes (NFC & QR Code)

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# 14](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20%2F%20Hexagonal-blue)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
[![TDD](https://img.shields.io/badge/Tests-TDD%20(233%2B%20Passing)-brightgreen)](file:///tests/UnitTests)
[![Blazor Server](https://img.shields.io/badge/Frontend-Blazor%20Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![.NET MAUI](https://img.shields.io/badge/Mobile-.NET%20MAUI%20(NFC)-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/maui)
[![Documentation](https://img.shields.io/badge/Docs-Scalar%20OpenAPI-orange)](http://localhost:5000/scalar/v1)

A **EstrelaClick** é uma plataforma integrada de ponta a ponta voltada à fabricação industrial, gravação seriada em bancada via hardware móvel, redirecionamento dinâmico de ultra baixa latência e gestão completa de métricas para placas físicas inteligentes equipadas com tecnologia **NFC** (NTAG213/NTAG215) e **QR Code**.

O ecossistema permite que comércios físicos (restaurantes, clínicas, lojas e prestadores de serviço) alavanquem avaliações no **Google Meu Negócio**, capturem feedbacks via filtro de satisfação ou direcionem clientes para WhatsApp e cardápios digitais, **sem que o suporte físico precise ser regravado ou recolhido**.

---

## 📑 Sumário

1. [Visão Arquitetural](#1-visão-arquitetural)
2. [Stack Tecnológica](#2-stack-tecnológica)
3. [Estrutura da Solution](#3-estrutura-da-solution)
4. [Pilares de Engenharia e Padrões Mandatórios](#4-pilares-de-engenharia-e-padrões-mandatórios)
5. [Ciclo Operacional: Da Fábrica ao Balcão do Lojista](#5-ciclo-operacional-da-fábrica-ao-balcão-do-lojista)
6. [Registro de Decisões Arquiteturais (ADRs)](#6-registro-de-decisões-arquiteturais-adrs)
7. [Status do Roadmap](#7-status-do-roadmap)
8. [Primeiros Passos e Execução Local](#8-primeiros-passos-e-execução-local)
9. [Qualidade e Suíte de Testes](#9-qualidade-e-suíte-de-testes)

---

## 1. Visão Arquitetural

A engenharia do ecossistema adota a estratégia de **monólito modular** em uma única Solution .NET (`EstrelaClick.slnx`). O backend segue rigorosamente a **Clean Architecture** e a **Arquitetura Hexagonal (Ports & Adapters)**, com fronteiras protegidas por testes de arquitetura automatizados.

```
                               ┌─────────────────────────────────────────┐
                               │           Dispositivos Finais           │
                               │  (Aproximação NFC / Leitura de QR Code) │
                               └────────────────────┬────────────────────┘
                                                    │
                                                    ▼ HTTP (302) < 25ms
┌──────────────────────────┐           ┌────────────────────────────────┐
│   Factory Mobile App     │           │       Backend Web API          │
│ (.NET MAUI - Android/iOS)├──────────►│        (.NET 10 / C# 14)       │
│  Gravação NDEF de Chips  │  REST API │ - Redirecionador Ultra Rápido  │
│  Operação de Bancada     │           │ - HybridCache (L1/L2)          │
└──────────────────────────┘           │ - Telemetria via Channel<T>    │
                                       │ - Documentação Scalar OpenAPI  │
┌──────────────────────────┐           └───────┬───────────────┬────────┘
│    Admin Dashboard       │                   │               │
│  (Blazor Server-Side)    ├───────────────────┘               │
│ Gestão de Fábrica/Lotes  │                                   ▼
│ Exportação SVG/PNG Lote  │                       ┌────────────────────┐
└──────────────────────────┘                       │     SQL Server     │
                                                   │ EF Core 10 + Migr. │
┌──────────────────────────┐                       └────────────────────┘
│    Client Dashboard      │                                   ▲
│  (Blazor Server-Side)    ├───────────────────────────────────┘
│ Onboarding / Métricas    │
│ Link Dinâmico / Reviews  │
└──────────────────────────┘
```

---

## 2. Stack Tecnológica

| Camada | Tecnologia | Finalidade |
| :--- | :--- | :--- |
| **Linguagem & Runtime** | C# 14 / .NET 10 SDK | Recursos modernos de linguagem, type safety e performance |
| **Backend API** | ASP.NET Core Minimal APIs | Endpoints concisos, alta taxa de requisições por segundo |
| **Documentação API** | Scalar (`Scalar.AspNetCore`) | Interface moderna e interativa de API sobre OpenAPI nativo |
| **Persistência** | EF Core 10 + SQL Server | Mapeamentos explícitos via Fluent API e Migrations seguras |
| **Performance & Cache** | `Microsoft.Extensions.Caching.Hybrid` | Cache de dois níveis com resolução de buscas de slug em $O(1)$ |
| **Processamento Assíncrono**| `System.Threading.Channels` | Buffer de telemetria sem bloqueio de I/O na rota crítica |
| **Geração de QR Code** | `QRCoder` | Saída vetorial SVG para corte/gravação a laser e PNG a 300 DPI |
| **Painéis Web** | Blazor Server-Side (.NET 10) | Interatividade SSR rápida via SignalR com Dashboards segregados |
| **Design System** | `Shared.UI` + `tokens.css` | Design Tokens CSS centralizados com suporte completo a Dark Mode |
| **Operação de Bancada** | .NET MAUI (Android / iOS) | Integração com `NfcAdapter`, gravação contínua NDEF e feedback tátil/sonoro |
| **Qualidade & Testes** | xUnit, FluentAssertions, Moq, NetArchTest | TDD estrito com validação de regras invariantes e fronteiras |

---

## 3. Estrutura da Solution

A organização física das pastas reflete o isolamento estrito de responsabilidades:

```text
EstrelaClick/
├── .editorconfig                       # Convenções de formatação e análise estática C#
├── AGENTS.md                           # Contrato mandatório de engenharia e regras de desenvolvimento
├── inidial_roadmap.md                  # Roadmap e roteiro de homologação da plataforma
├── EstrelaClick.slnx                   # Arquivo de solução XML unificado (.NET 10)
│
├── docs/                               # Documentação viva de engenharia
│   ├── adrs/                           # 14 Architectural Decision Records (ADRs)
│   └── architecture/                   # Diagramas e fluxos de gravação NFC
│
├── src/
│   ├── Backend/                        # Camadas de Backend (Clean Architecture / Hexagonal)
│   │   ├── Domain/                     # Entidades puras (Placa, Empresa, AcessoMetrica), Enums, Invariantes
│   │   ├── Application/                # Casos de uso (UseCases), Portas Inbound/Outbound, DTOs
│   │   ├── Infrastructure/             # Adapters: EF Core 10, SQL Server, Identity, Cache, QRCoder
│   │   └── Api/                        # Minimal APIs, Scalar OpenAPI (/scalar/v1), Redirecionador /r/{slug}
│   │
│   ├── Frontend/                       # Camadas de Apresentação Web Segregadas
│   │   ├── AdminDashboard/             # Blazor Server: Gestão de fábrica, emissão de lotes e clientes
│   │   ├── ClientDashboard/            # Blazor Server: Onboarding do lojista, link dinâmico e analytics
│   │   └── Shared.UI/                  # Componentes reutilizáveis (Razor) e Design Tokens (CSS)
│   │
│   ├── Mobile/                         # Ferramenta Operacional Industrial
│   │   └── FactoryMobileApp/           # .NET MAUI: Gravação contínua NDEF de tags NTAG213/215
│   │
│   └── Shared/
│       └── SharedKernel/               # Tipos utilitários compartilhados: Result<T>, Error, Enums globais
│
└── tests/
    ├── UnitTests/                      # Testes unitários (Domain, Application e UseCases) — 219+ testes
    ├── IntegrationTests/               # Testes de integração de infraestrutura e banco
    └── ArchitectureTests/              # Validação de regras de dependência (NetArchTest)
```

---

## 4. Pilares de Engenharia e Padrões Mandatórios

Conforme detalhado no [AGENTS.md](file:///AGENTS.md), o projeto é regido por diretrizes de código inegociáveis:

1. **Test-Driven Development (TDD) Obrigatório:**
   * Nenhuma funcionalidade é implementada antes da escrita de testes de unidade e regras de fronteira (*Red -> Green -> Refactor*).
   * Cobertura mínima de 85% no Domain e Application.
2. **Padrão Funcional `Result<T>` (Zero Exceções de Domínio):**
   * Nenhum fluxo esperado de negócio lança exceções. Métodos retornam instâncias imutáveis de `Result<T>` ou `Result` com códigos de erro canônicos (`Error`).
3. **Fronteiras da Clean Architecture Rigorosamente Testadas:**
   * O projeto `Domain` possui **zero dependências** externas ou referências a pacotes de banco.
   * `ArchitectureTests` garantem via compilação contínua que o `Domain` e `Application` não referenciam `Infrastructure` ou `Api`.
4. **Redirecionamento de Ultra Baixa Latência ($< 25\text{ ms}$):**
   * O endpoint `GET /r/{slug}` atende aproximações de smartphones em tempo real via cache de alto desempenho, enfileirando registros de telemetria em um canal assíncrono não-bloqueante (`Channel<T>`).
5. **Segregação Total dos Portais Web:**
   * `AdminDashboard` (operação de fábrica) e `ClientDashboard` (lojista) são aplicações Blazor Server independentes, impedindo vazamento de contextos de segurança e regras administrativas.
6. **Design Tokens e Identidade Visual Centralizada:**
   * Cores, espaçamentos, tipografia e raios de borda são consumidos via variáveis CSS em `tokens.css` na biblioteca compartilhada `Shared.UI`. É estritamente proibido o uso de valores arbitrários *hardcoded*.

---

## 5. Ciclo Operacional: Da Fábrica ao Balcão do Lojista

```
 [1. Gerar Lote]  ──►  [2. Gravar NTAG]  ──►  [3. Onboarding]  ──►  [4. Operação]
  AdminDashboard       FactoryMobileApp       ClientDashboard        Cliente Final
  SVG/PNG 300DPI        NDEF URI + UID          Link Dinâmico         HTTP 302 < 25ms
```

### 1. Geração de Lotes e Fabricação Física
* O operador acessa o `AdminDashboard` e cria um novo lote de placas.
* A API gera os registros no banco com status `Criada` e `slugs` aleatórios criptograficamente seguros.
* O sistema disponibiliza download compactado (.zip) com **QR Code em SVG** (para gravação a laser) e **PNG 300 DPI** (para gráfica e impressão direta).

### 2. Gravação em Linha de Produção (.NET MAUI)
* O operador de bancada autentica-se no `FactoryMobileApp` em dispositivo Android equipado com leitor NFC.
* O aplicativo solicita automaticamente a próxima placa pendente (`GET /api/placas/proxima-pendente`).
* Ao aproximar o adesivo NFC (NTAG213 ou NTAG215), o app grava a mensagem NDEF do tipo URI (`https://estrela.click/r/{slug}`) utilizando `Foreground Dispatch`.
* O app lê o UID físico de fábrica do chip, dispara bipe sonoro e vibração tátil, confirma a gravação na API (`POST /api/placas/confirmar-gravacao`) e passa para o status `Gravada`.

### 3. Onboarding Instantâneo pelo Lojista
* O lojista recebe sua placa física e aproxima seu smartphone ou escaneia o QR Code.
* Como a placa está com status `Gravada` (ainda sem vínculo de empresa), o redirecionador a encaminha imediatamente para o portal de ativação do `ClientDashboard`.
* O lojista cria sua conta ou efetua login, vinculando a placa ao seu estabelecimento e cadastrando o link de destino desejado (Google Meu Negócio, WhatsApp, cardápio digital, etc.).
* O status da placa avança para `Vinculada`.

### 4. Operação Contínua e Métricas em Tempo Real
* Clientes aproximam o smartphone da placa no balcão da loja.
* A rota `/r/{slug}` recupera o destino a partir do cache L1/L2 e despacha o redirecionamento HTTP 302 instantâneo.
* O evento de acesso é enfileirado na memória via `Channel<T>` e gravado em lote por um `BackgroundService` no banco de dados.
* O lojista acompanha toques diários, semanais, horários de pico e distribuição de dispositivos em seu dashboard.

---

## 6. Registro de Decisões Arquiteturais (ADRs)

Todas as decisões arquiteturais fundamentais da plataforma estão formalizadas em documentos Markdown versionados no diretório [docs/adrs/](file:///docs/adrs):

| ADR | Título | Status |
| :---: | :--- | :---: |
| [0001](file:///docs/adrs/0001-estrutura-solucao.md) | Estrutura da Solução em Monólito Modular | Aprovado |
| [0002](file:///docs/adrs/0002-padrao-result-monad.md) | Adoção do Padrão Result/Result\<T> e Struct Error | Aprovado |
| [0003](file:///docs/adrs/0003-modelagem-entidades-invariantes.md) | Modelagem de Entidades Ricas e Invariantes de Negócio | Aprovado |
| [0004](file:///docs/adrs/0004-persistencia-efcore-identity-migrations.md) | Persistência com EF Core 10, Mapeamentos e Migrations | Aprovado |
| [0005](file:///docs/adrs/0005-migracao-postgresql-para-sqlserver.md) | Migração de Persistência PostgreSQL para SQL Server | Aprovado |
| [0006](file:///docs/adrs/0006-motor-redirecionamento-cache-metricas.md) | Motor de Redirecionamento de Baixa Latência e Telemetria Assíncrona | Aprovado |
| [0007](file:///docs/adrs/0007-motor-geracao-qrcode-exportacao-lotes.md) | Motor de Geração de QR Code (PNG 300 DPI e SVG Laser) | Aprovado |
| [0008](file:///docs/adrs/0008-api-lotes-controle-fila-mobile.md) | API de Lotes e Controle de Fila para App Mobile | Aprovado |
| [0009](file:///docs/adrs/0009-abstracao-nfc-foreground-dispatch-maui.md) | Abstração de Hardware NFC e Foreground Dispatch no .NET MAUI | Aprovado |
| [0010](file:///docs/adrs/0010-interface-operador-bancada-maui.md) | Interface Operacional de Bancada e Gravação Contínua | Aprovado |
| [0011](file:///docs/adrs/0011-ciclo-operacional-gravacao-ndef.md) | Ciclo Operacional de Gravação NDEF e Validação de Chips | Aprovado |
| [0012](file:///docs/adrs/0012-biblioteca-compartilhada-design-tokens-ui.md) | Biblioteca Compartilhada de Componentes UI e Design Tokens | Aprovado |
| [0013](file:///docs/adrs/0013-dashboard-gestao-interna-admin.md) | Arquitetura e Telas do Dashboard Administrativo Interno | Aprovado |
| [0014](file:///docs/adrs/0014-dashboard-cliente-lojista-onboarding-analytics.md) | Dashboard do Cliente Lojista: Onboarding, Edição e Analytics | Aprovado |
| [0015](file:///docs/adrs/0015-execucao-migrations-arranque-api.md) | Execução Automática de Migrações do EF Core no Arranque da API | Aprovado |
| [0016](file:///docs/adrs/0016-carregamento-variaveis-ambiente-dotenv.md) | Carregamento de Variáveis de Ambiente via Arquivo .env | Aprovado |
| [0017](file:///docs/adrs/0017-docker-compose-servicos-desenvolvimento.md) | Docker Compose para Serviços de Apoio em Desenvolvimento Local | Aprovado |

---

## 7. Status do Roadmap

Acompanhamento detalhado segundo o [inidial_roadmap.md](file:///inidial_roadmap.md):

* **Fase 1: Arquitetura, Core Domain e Persistência**
  * [x] Solução .NET 10, Clean Architecture e Hexagonal configurados.
  * [x] Contratos canônicos `Result<T>` e entidades ricas (`Placa`, `Empresa`, `AcessoMetrica`).
  * [x] Mapeamentos EF Core 10 e índices de busca em $O(1)$.
  * [x] Suíte de testes unitários e de arquitetura aprovada.
* **Fase 2: Backend e Redirecionador de Alta Performance**
  * [x] Redirecionador `GET /r/{slug}` com `HybridCache` e telemetria via `Channel<T>`.
  * [x] Motor de geração de QR Code em SVG e PNG 300 DPI com exportação em lote (.zip).
  * [x] API de lotes e controle de fila industrial com autenticação e documentação Scalar OpenAPI.
* **Fase 3: Aplicativo de Bancada Mobile (.NET MAUI)**
  * [x] Serviços Android nativos de NFC com `Foreground Dispatch`.
  * [x] Ciclo contínuo de gravação NDEF, captura de UID de chip e feedback sonoro/háptico.
  * [x] Tratamento de tags protegidas contra gravação e retry resiliente.
* **Fase 4: Painéis Web Segregados (Blazor Server)**
  * [x] Biblioteca `Shared.UI` com Design Tokens em `tokens.css` e componentes desacoplados.
  * [x] `AdminDashboard`: Gestão de fábrica, lotes e exportação gráfica.
  * [x] `ClientDashboard`: Onboarding sem atrito, alteração do link dinâmico e gráficos de métricas.
* **Fase 5: Hardware, Homologação e Entrada em Produção**
  * [ ] Homologação de amostras físicas de chips NTAG213/215 em acrílico, madeira e superfícies anti-metal.
  * [ ] Pipeline de CI/CD e empacotamento em contêineres Docker Linux multi-stage.
  * [ ] Provisionamento de banco de dados gerenciado SQL Server com alta disponibilidade e SSL/TLS obrigatório.

---

## 8. Primeiros Passos e Execução Local

### Pré-requisitos
* [.NET 10 SDK](https://dotnet.microsoft.com/) (v10.0.400+)
* [Docker Desktop](https://www.docker.com/) ou Docker Engine com Docker Compose
* Carga de trabalho .NET MAUI (caso deseje compilar o aplicativo móvel):
  ```powershell
  dotnet workload install maui
  ```

### Clonar e Compilar
```powershell
git clone https://github.com/ronaldocestrela/EstrelaClick.git
cd EstrelaClick
dotnet restore
dotnet build
```

### Configurar Variáveis de Ambiente (.env)
Copie o arquivo modelo `.env.example` para `.env` na raiz do repositório:
```powershell
# Windows (PowerShell)
Copy-Item .env.example .env

# Linux / macOS (Bash)
cp .env.example .env
```
O arquivo `.env` já contém valores padrão adequados para desenvolvimento local. As aplicações (Backend API, Admin Dashboard e Client Dashboard) carregam este arquivo automaticamente no arranque.

### Iniciar Serviços de Infraestrutura (Docker Compose)
Inicie o SQL Server 2022 e o Redis 7 em segundo plano com um único comando:
```powershell
docker compose up -d
```
Para verificar a saúde dos serviços em execução:
```powershell
docker compose ps
```

### Configurar Banco de Dados e Migrations
1. Por padrão, a API executa migrações pendentes automaticamente ao iniciar (`Database__AutoMigrate=true`).
2. Alternativamente, para aplicar migrações via CLI:
   ```powershell
   dotnet ef database update --project src/Backend/Infrastructure --startup-project src/Backend/Api
   ```

### Executar a API e Documentação Scalar
```powershell
dotnet run --project src/Backend/Api
```
Acesse a documentação interativa das rotas OpenAPI no navegador:
```
http://localhost:5000/scalar/v1
```

### Executar os Dashboards Web
* **Admin Dashboard:**
  ```powershell
  dotnet run --project src/Frontend/AdminDashboard
  ```
* **Client Dashboard:**
  ```powershell
  dotnet run --project src/Frontend/ClientDashboard
  ```

---

## 9. Qualidade e Suíte de Testes

A integridade do domínio e os acoplamentos arquiteturais são validados continuamente via xUnit e NetArchTest:

```powershell
# Executar toda a suíte de testes unitários e de arquitetura
dotnet test --filter Category!=Integration
```

Os testes de arquitetura garantem que:
* `Domain` não possui nenhuma referência a frameworks externos, persistência ou EF Core.
* `Application` depende unicamente do `Domain` e do `SharedKernel`.
* Regras de transição de status das placas (`Criada` -> `Gravada` -> `Vinculada`) são estritamente cumpridas.

---

*Desenvolvido com foco em alta performance, robustez e simplicidade operacional.*