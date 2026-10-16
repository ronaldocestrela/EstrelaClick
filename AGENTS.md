# AGENTS.md — Regras de Desenvolvimento, Arquitetura e Padrões da Solução

> **Aviso para Agentes de IA e Desenvolvedores:** Este arquivo é o contrato mandatório de engenharia deste repositório. Nenhuma linha de código deve ser escrita sem estrita conformidade com as regras, padrões de design e convenções aqui estabelecidas.

---

## 1. Visão Geral da Solução e Tecnologias

A solução consolida toda a plataforma de geração, controle dinâmico de placas NFC/QR Code, gravação via hardware móvel e painéis administrativos em um **monólito modular** dentro de uma única Solution .NET (`.sln`).

### 1.1 Stack Tecnológica
* **Linguagem & Runtime:** C# 14 / .NET 10.
* **Backend:** ASP.NET Core Web API (Clean Architecture + Arquitetura Hexagonal / Ports & Adapters).
* **Persistência & Identidade:** Entity Framework Core 10 (SQL Server) + ASP.NET Core Identity Core com suporte a autenticação por Cookie/JWT.
* **Documentação de API:** OpenAPI nativo com **Scalar** (`Scalar.AspNetCore`).
* **Frontend Web:** Blazor Server-Side (Interatividade SSR/Server com SignalR).
  * Painel 1: **Backoffice / Gestão Interna** (Gerenciamento de lotes, clientes e inventário).
  * Painel 2: **Dashboard do Cliente / Lojista** (Configuração de links e analytics de acessos).
* **App Mobile:** .NET MAUI (Android/iOS) focado em operação de bancada (leitura, gravação NDEF e bloqueio de chips NTAG213/215).
* **Testes:** xUnit, FluentAssertions, Moq/NSubstitute, Respawn/Testcontainers (para testes de integração).

---

## 2. Regras Inegociáveis (Core Tenets)

1. **Test-Driven Development (TDD) Obrigatório:**
   * Nenhuma funcionalidade ou caso de uso é implementado antes do teste unitário correspondente falhar (*Red -> Green -> Refactor*).
   * Cobertura mínima de código exigida: **85%** no Core/Domain e Application.
2. **Documentação Viva e ADRs (Architectural Decision Records):**
   * Toda decisão arquitetural estrutural (ex.: estratégia de cache, ciclo de vida de tags NFC, autenticação entre apps) deve ser documentada em `/docs/adrs/` usando o formato Markdown ADR (`ADR-XXXX-titulo.md`).
   * A documentação técnica vive junto com o código e deve ser atualizada no mesmo PR/commit.
3. **Padrão Result\<T> (Zero Exceções de Domínio/Fluxo):**
   * O fluxo de controle nunca deve lançar exceções para regras de negócio esperadas (ex.: "Placa não encontrada", "Lote esgotado", "Slug inválido").
   * Todo método de serviço de aplicação e casos de uso deve retornar `Result<T>` ou `Result` (usando tipos funcionais com tratamento explícito de erro, código de erro e mensagens).
4. **Padrão DRY (Don't Repeat Yourself) & Componetização:**
   * Lógicas repetidas devem ser abstraídas em bibliotecas compartilhadas locais dentro da solution.
   * Elementos de UI repetidos no Blazor Server devem ser desacoplados em componentes isolados no projeto `Shared.UI`.
5. **Tokenização da Identidade Visual (Design Tokens):**
   * Cores, tipografia, espaçamentos, raios de borda e sombras não podem ser colocados como valores arbitrários (*hardcoded*).
   * Devem ser mapeados estritamente através de variáveis CSS / Tokens estruturados compartilhados entre os Dashboards e estilização do MAUI.
6. **Segregação de Dashboards:**
   * O Dashboard de Gestão (Operação Interna) e o Dashboard do Cliente (Lojista) devem ser aplicações/projetos Blazor completamente separados para garantir isolamento de contexto, segurança e ciclo de vida de deploy.

---

## 3. Estrutura da Solução e Organização de Pastas

```text
/
├── .editorconfig
├── AGENTS.md
├── EstrelaClick.slnx
│
├── docs/
│   ├── adrs/                      # Architectural Decision Records
│   │   ├── 0001-estrutura-solucao.md
│   │   └── 0002-padrao-result-monad.md
│   └── architecture/              # Diagramas e fluxos de gravação NFC
│
├── src/
│   ├── Backend/                   # Clean Architecture / Hexagonal
│   │   ├── Domain/                # Entidades puras, Enums, Interfaces de Domínio
│   │   │   ├── Entities/
│   │   │   ├── ValueObjects/
│   │   │   ├── Exceptions/        # Apenas exceções catastróficas de infraestrutura
│   │   │   └── Common/            # Classes base (Entity, AggregateRoot)
│   │   │
│   │   ├── Application/           # Casos de uso (UseCases / Handlers)
│   │   │   ├── Ports/
│   │   │   │   ├── Inbound/       # Contratos de casos de uso
│   │   │   │   └── Outbound/      # Interfaces de repositórios, serviços externos, NFC
│   │   │   ├── UseCases/
│   │   │   ├── DTOs/
│   │   │   └── Common/            # Result<T>, Error, PagedResult
│   │   │
│   │   ├── Infrastructure/        # Adapters Secundários (Driven)
│   │   │   ├── Persistence/       # EF Core 10, DbContext, Migrations, Mappings
│   │   │   ├── Identity/          # ASP.NET Core Identity Core Configs
│   │   │   ├── QrCode/            # Geração com QRCoder (PNG, SVG)
│   │   │   └── Cache/             # Redis / In-Memory Cache para o Redirecionador
│   │   │
│   │   └── Api/                   # Adapters Primários (Driving)
│   │       ├── Controllers/       # Ou Minimal APIs
│   │       ├── Endpoints/         # Rotas Scalar OpenAPI, Redirecionador /r/{slug}
│   │       ├── Middleware/        # Global Exception Handler, Headers de Telemetria
│   │       └── Program.cs
│   │
│   ├── Frontend/                  # Camadas de Apresentação Web
│   │   ├── AdminDashboard/        # Blazor Server: Painel Interno / Fábrica / Lotes
│   │   │   ├── Pages/
│   │   │   ├── Layout/
│   │   │   └── Program.cs
│   │   │
│   │   ├── ClientDashboard/       # Blazor Server: Lojista (Métricas e Edição de Link)
│   │   │   ├── Pages/
│   │   │   ├── Layout/
│   │   │   └── Program.cs
│   │   │
│   │   └── Shared.UI/             # Componentes Blazor reutilizáveis e Design Tokens
│   │       ├── Components/        # Modais, Gráficos, Tabelas, Inputs customizados
│   │       ├── Tokens/            # Tokens de estilo (Cores, Tipografia, Spacing)
│   │       └── wwwroot/css/       # Variáveis CSS raíz (tokens compilados)
│   │
│   ├── Mobile/                    # Ferramenta Operacional de Bancada
│   │   └── FactoryMobileApp/      # .NET MAUI (Android/iOS)
│   │       ├── Platforms/         # Integrações nativas (NfcAdapter, NDEF Writer)
│   │       ├── Views/             # Interface de gravação contínua
│   │       ├── ViewModels/        # MVVM
│   │       └── Services/          # Cliente HTTP da API para consumir fila de slugs
│   │
│   └── Shared/
│       └── SharedKernel/          # Tipos utilitários compartilhados entre Back, Front e App
│           ├── Results/           # Definição canônica de Result<T> e Error
│           └── Enums/             # Enums compartilhados (ex.: StatusPlaca, TipoLink)
│
└── tests/
    ├── UnitTests/                 # Testes unitários focados em TDD (Domain + Application)
    ├── IntegrationTests/          # Testes de EF Core, Endpoints e Fluxos de Banco
    └── ArchitectureTests/         # Testes de validação das fronteiras arquiteturais (NetArchTest)
```

---

## 4. Diretrizes de Código e Padrões Obrigatórios

### 4.1 O Padrão `Result<T>`
Nunca lance exceções para fluxo de validação ou regras de negócio esperadas.

```csharp
// Exemplo canônico de contrato
public readonly record struct Error(string Code, string Description);

public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error Error { get; }

    protected Result(T value) { IsSuccess = true; Value = value; Error = Error.None; }
    protected Result(Error error) { IsSuccess = false; Value = default; Error = error; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
}
```

### 4.2 Arquitetura Hexagonal & Clean Architecture
* **Domain:** Zero dependências de outros projetos, frameworks ou EF Core. Contém entidades ricas com invariantes protegidas por métodos de negócio.
* **Application:** Contém as Portas de Entrada (`IUseCase`) e Portas de Saída (`IPlacaRepository`, `INfcService`, `IQrCodeGenerator`). Não acessa diretamente implementações de banco ou bibliotecas de terceiros.
* **Infrastructure:** Implementa as portas de saída. Apenas este projeto referencia `Microsoft.EntityFrameworkCore` e pacotes externos concretos.
* **Api / Redirecionador:** O endpoint de redirecionamento (`GET /r/{slug}`) deve responder em tempo inferior a 25 ms, com leitura em cache prioritária.
* **Scalar OpenAPI:** Configurado no `Program.cs` para servir documentação interativa em `/scalar/v1`.

### 4.3 Design Tokens & Componentização (Blazor)
* **Tokens:** Definidos como variáveis no arquivo `tokens.css` dentro de `Shared.UI`. Exemplos:
  ```css
  :root {
      --color-primary: #0F172A;
      --color-accent: #3B82F6;
      --color-surface: #FFFFFF;
      --spacing-base: 8px;
      --radius-default: 6px;
  }
  ```
* É estritamente proibido o uso de classes CSS com valores arbitrários ou cores fixadas no código HTML. Toda personalização deve referenciar os tokens.
* **Componentes:** Todo formulário, modal, indicador de status e cartão de métrica deve ser componente Razor em `Shared.UI`.

### 4.4 Aplicativo Mobile (.NET MAUI)
* O MAUI destina-se ao fluxo industrial/bancada: **Login -> Carregar Fila -> Gravar NDEF por aproximação -> Confirmar UID -> Bipe/Haptic Feedback**.
* Nenhuma lógica de negócio de domínio reside no app. Ele apenas consome a API através de contratos definidos e realiza chamadas às APIs de hardware de NFC.

---

## 5. Instruções de Trabalho para o Agente de IA

Ao criar ou editar arquivos neste repositório:
1. **Verifique se há teste:** Comece escrevendo o teste unitário no projeto de testes correspondente.
2. **Respeite os limites das camadas:** Não referencie EF Core na Application ou Domain. Não acerte o banco a partir do Blazor sem passar pelos UseCases ou cliente de API autenticado.
3. **Mantenha os Dashboards Isolados:** Não crie dependências de código entre o `AdminDashboard` e o `ClientDashboard`. Use o `Shared.UI` para reaproveitamento visual.
4. **Atualize as ADRs:** Se introduzir uma dependência externa relevante ou mudar um padrão de comunicação, crie o respectivo registro em `docs/adrs/`.
5. **Cumpra o Padrão Result:** Retorne sempre `Result<T>` nos métodos de negócio e mapeie adequadamente os códigos HTTP na camada de API.