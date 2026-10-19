# 📘 Manual de Instruções de Uso — Plataforma EstrelaClick

Guia prático e operacional com instruções passo a passo para configuração de ambiente, execução dos módulos, fluxo de fabricação e operação das placas inteligentes NFC e QR Code da plataforma **EstrelaClick**.

---

## 📑 Sumário

1. [Visão Geral dos Componentes](#1-visão-geral-dos-componentes)
2. [Pré-requisitos do Sistema](#2-pré-requisitos-do-sistema)
3. [Configuração Inicial do Ambiente](#3-configuração-inicial-do-ambiente)
4. [Execução dos Módulos da Solução](#4-execução-dos-módulos-da-solução)
   - [4.1 Backend Web API](#41-backend-web-api)
   - [4.2 Admin Dashboard (Backoffice)](#42-admin-dashboard-backoffice)
   - [4.3 Client Dashboard (Painel do Lojista)](#43-client-dashboard-painel-do-lojista)
   - [4.4 Factory Mobile App (.NET MAUI)](#44-factory-mobile-app-net-maui)
5. [Fluxo Operacional Passo a Passo (Ciclo de Vida da Placa)](#5-fluxo-operacional-passo-a-passo-ciclo-de-vida-da-placa)
   - [Passo 1: Geração de Lotes no Admin Dashboard](#passo-1-geração-de-lotes-no-admin-dashboard)
   - [Passo 2: Gravação NDEF na Bancada Móvel](#passo-2-gravação-ndef-na-bancada-móvel)
   - [Passo 3: Ativação e Onboarding pelo Lojista](#passo-3-ativação-e-onboarding-pelo-lojista)
   - [Passo 4: Operação do Cliente Final e Redirecionamento Dinâmico](#passo-4-operação-do-cliente-final-e-redirecionamento-dinâmico)
   - [Passo 5: Filtro de Avaliações (Feedback Gate)](#passo-5-filtro-de-avaliações-feedback-gate)
6. [Tabela de Referência Rápida (Portas, URLs e Credenciais)](#6-tabela-de-referência-rápida-portas-urls-e-credenciais)
7. [Execução de Testes Automatizados](#7-execução-de-testes-automatizados)
8. [Resolução de Dúvidas e Problemas Comuns (Troubleshooting)](#8-resolução-de-dúvidas-e-problemas-comuns-troubleshooting)

---

## 1. Visão Geral dos Componentes

O ecossistema EstrelaClick é estruturado em um monólito modular composto por quatro aplicações principais:

* **Backend Web API (`src/Backend/Api`):**
  - Motor de redirecionamento HTTP 302 em ultra baixa latência (< 25 ms) na rota `/r/{slug}`.
  - Documentação OpenAPI interativa servida via **Scalar** em `/scalar/v1`.
  - Processamento de métricas assíncrono não-bloqueante via `Channel<T>`.
  - Cache híbrido L1 (em memória) e L2 (**Redis**).
* **Admin Dashboard (`src/Frontend/AdminDashboard`):**
  - Aplicação Blazor Server voltada à operação de fábrica e gestão de pedidos.
  - Criação de lotes com slugs criptográficos aleatórios.
  - Exportação compactada (.zip) com QR Codes vetoriais em **SVG** (para corte/gravação a laser) e **PNG 300 DPI** (para gráfica).
  - Gestão de clientes, placas associadas e trilha de auditoria.
* **Client Dashboard (`src/Frontend/ClientDashboard`):**
  - Aplicação Blazor Server para os clientes/lojistas.
  - Onboarding instantâneo sem atrito via tela de ativação (`/ativar/{slug}`).
  - Configuração do link dinâmico de destino (Google Meu Negócio, WhatsApp, Link Direto ou Filtro Feedback Gate).
  - Gráficos e indicadores de acessos, dispositivos e horários de pico.
* **Factory Mobile App (`src/Mobile/FactoryMobileApp`):**
  - Ferramenta móvel .NET MAUI para Android/iOS dedicada à bancada industrial.
  - Fila de placas pendentes com gravação contínua NDEF em chips NTAG213/NTAG215.
  - Captura do UID de fábrica do chip e emissão de feedback sonoro e háptico (vibração).

---

## 2. Pré-requisitos do Sistema

Antes de iniciar a execução local, verifique a instalação das ferramentas:

1. **.NET 10 SDK (versão 10.0+)**:
   ```powershell
   dotnet --version
   ```
2. **Docker Desktop** (ou Docker Engine no Linux) com suporte a **Docker Compose**:
   ```powershell
   docker --version
   docker compose version
   ```
3. **Carga de Trabalho .NET MAUI** *(opcional, necessária apenas para compilar o app móvel de bancada)*:
   ```powershell
   dotnet workload install maui
   ```

---

## 3. Configuração Inicial do Ambiente

### 3.1 Clonar o Repositório e Restaurar Pacotes
No terminal (PowerShell ou Bash):
```powershell
git clone https://github.com/ronaldocestrela/EstrelaClick.git
cd EstrelaClick
dotnet restore
dotnet build
```

### 3.2 Configurar o Arquivo `.env`
Copie o modelo de ambiente `.env.example` para `.env` na raiz do projeto:
```powershell
# No Windows PowerShell:
Copy-Item .env.example .env

# No Linux ou macOS:
cp .env.example .env
```
O arquivo `.env` já vem pré-configurado com valores padrão para desenvolvimento local:
* **Banco de dados:** Conexão local com o SQL Server na porta `1433`.
* **Redis:** Conexão com o cache distribuído na porta `6379`.
* **Segurança:** Chaves e senhas padrão de administradores e operadores.

### 3.3 Inicializar Serviços de Apoio (Docker Compose)
Inicie os contêineres do **SQL Server 2022** e do **Redis 7**:
```powershell
docker compose up -d
```
Verifique se os contêineres estão saudáveis:
```powershell
docker compose ps
```

### 3.4 Banco de Dados e Migrações
A API está configurada para aplicar migrações pendentes automaticamente ao inicializar (`Database__AutoMigrate=true`).
Caso prefira aplicar manualmente via CLI do Entity Framework Core:
```powershell
dotnet ef database update --project src/Backend/Infrastructure --startup-project src/Backend/Api
```

---

## 4. Execução dos Módulos da Solução

Para executar o ecossistema completo em desenvolvimento, abra terminais separados para cada serviço ou utilize o perfil de inicialização múltipla do Visual Studio / JetBrains Rider.

### 4.1 Backend Web API
Inicie a API responsável pelos endpoints e pelo redirecionador:
```powershell
dotnet run --project src/Backend/Api
```
* **URL HTTP Principal:** `http://localhost:5180`
* **URL HTTPS:** `https://localhost:7157`
* **Documentação Interativa (Scalar OpenAPI):** `http://localhost:5180/scalar/v1`
* **Endpoint de Redirecionamento:** `http://localhost:5180/r/{slug}`

> [!TIP]
> Acesse a documentação Scalar no navegador para explorar, testar rotas e conferir os schemas JSON de entrada e saída.

---

### 4.2 Admin Dashboard (Backoffice)
Inicie o painel administrativo de gestão de fábrica e lotes:
```powershell
dotnet run --project src/Frontend/AdminDashboard
```
* **URL Principal:** `http://localhost:5077` (ou `https://localhost:7203`)
* **Página de Login:** `http://localhost:5077/login`
* **Credenciais Padrão do Administrador Master:**
  - **Usuário:** `admin@estrelaclick.com.br`
  - **Senha:** `AdminMaster2026!`

---

### 4.3 Client Dashboard (Painel do Lojista)
Inicie o painel de autoatendimento para lojistas:
```powershell
dotnet run --project src/Frontend/ClientDashboard
```
* **URL Principal:** `http://localhost:5251` (ou `https://localhost:7143`)
* **Tela de Ativação:** `http://localhost:5251/ativar` ou `http://localhost:5251/ativar/{slug}`
* **Página de Avaliação (Feedback Gate):** `http://localhost:5251/feedback/{slug}`

---

### 4.4 Factory Mobile App (.NET MAUI)
O aplicativo móvel opera na bancada de fabricação para gravar os chips NFC.

#### Compilação e Execução para Android:
```powershell
dotnet build src/Mobile/FactoryMobileApp -f net10.0-android
```
Ou abra a solution no Visual Studio, defina `FactoryMobileApp` como projeto de inicialização, selecione o emulador ou aparelho físico Android conectado via USB (depuração USB ativada) e pressione `F5`.

#### Credenciais Padrão do Operador de Bancada:
* **Usuário:** `operador`
* **Senha:** `operador123`
* **URL da API Configurada:** `http://10.0.2.2:5180` (no emulador Android) ou `http://<IP_DO_SEU_PC>:5180` (em dispositivo físico na mesma rede Wi-Fi).

---

## 5. Fluxo Operacional Passo a Passo (Ciclo de Vida da Placa)

```
┌──────────────────┐      ┌──────────────────┐      ┌──────────────────┐      ┌──────────────────┐
│  1. Gerar Lote   │ ──►  │ 2. Gravar NFC    │ ──►  │ 3. Ativar Placa  │ ──►  │ 4. Uso do Cliente│
│ (AdminDashboard) │      │ (FactoryMobile)  │      │ (ClientDashboard)│      │   (/r/{slug})    │
│  Lote + SVG/PNG  │      │  NDEF + UID Chip │      │  Link de Destino │      │  302 Instantâneo │
└──────────────────┘      └──────────────────┘      └──────────────────┘      └──────────────────┘
```

### Passo 1: Geração de Lotes no Admin Dashboard
1. Acesse `http://localhost:5077/login` com as credenciais master (`admin@estrelaclick.com.br` / `AdminMaster2026!`).
2. No menu lateral, acesse **Lotes de Produção** (`/lotes`).
3. Clique em **Novo Lote** e defina:
   - **Nome/Identificador do Lote:** Ex.: `LOTE-2026-001`.
   - **Quantidade de Placas:** Ex.: `50`.
   - **Material/Tipo:** Acrílico, Madeira ou Metal.
4. O sistema gera automaticamente os slugs criptográficos seguros para cada placa com status inicial **`Criada`**.
5. Clique no botão de download para baixar o arquivo `.zip`:
   - Os arquivos **SVG** são usados diretamente nas máquinas de corte e gravação a laser.
   - Os arquivos **PNG a 300 DPI** servem para impressão gráfica de alta definição do QR Code.

---

### Passo 2: Gravação NDEF na Bancada Móvel
1. O operador de bancada abre o aplicativo **Factory Mobile App** no smartphone de produção.
2. Faz login com o usuário `operador` e senha `operador123`.
3. Ao entrar na tela de **Bancada de Gravação**:
   - O aplicativo consome automaticamente a fila de placas pendentes (`GET /api/placas/proxima-pendente`).
   - A tela exibe o slug atual aguardando aproximação.
4. O operador aproxima a placa física (adesivo com chip **NTAG213** ou **NTAG215**) da traseira do smartphone:
   - O app grava a URL NDEF: `http://localhost:5180/r/{slug}`.
   - O leitor captura o UID físico de fábrica do chip (ex.: `04:A2:8B:1A:3C:90:80`).
   - O aparelho emite um **bipe sonoro e vibração tátil** confirmando o sucesso.
   - A gravação é confirmada na API (`POST /api/placas/confirmar-gravacao`).
5. O status da placa na base de dados é promovido para **`Gravada`**.
6. Se o modo contínuo estiver ativado, o aplicativo carrega imediatamente a próxima placa da fila sem necessidade de toque na tela.

---

### Passo 3: Ativação e Onboarding pelo Lojista
1. A placa física chega ao estabelecimento comercial do lojista.
2. O lojista aproxima seu smartphone da placa ou escaneia o QR Code.
3. Como a placa está com status **`Gravada`** (ainda não vinculada a nenhum comércio), o redirecionador `/r/{slug}` o envia automaticamente para o portal de ativação:
   ```
   http://localhost:5251/ativar/{slug}
   ```
4. Na tela de ativação, o lojista:
   - Informa os dados da sua empresa (Nome Fantasia, WhatsApp, E-mail, Cidade/UF).
   - Define o **Tipo de Destino** da placa:
     * **Google Meu Negócio:** Link direto para a tela de avaliação no Google Maps/Search.
     * **WhatsApp:** Link direto para conversa (ex.: `https://wa.me/55...`).
     * **Link Direto:** Cardápio digital, Instagram ou website institucional.
     * **Filtro de Satisfação (Feedback Gate):** Direciona para a tela inteligente de 1 a 5 estrelas.
   - Clica em **Ativar Minha Placa**.
5. O status da placa muda para **`Vinculada`**. A partir desse instante, a placa está ativa para o público!

---

### Passo 4: Operação do Cliente Final e Redirecionamento Dinâmico
1. O cliente do estabelecimento aproxima o smartphone da placa no balcão da loja.
2. O navegador dispara a requisição para `http://localhost:5180/r/{slug}`.
3. O motor de redirecionamento:
   - Resolve o slug instantaneamente a partir do cache L1/L2 (**HybridCache**) em **tempo inferior a 25 ms**.
   - Responde com um `HTTP 302 Found` apontando para o link cadastrado pelo lojista.
   - Despacha o registro de telemetria (data, hora, User-Agent, tipo de dispositivo, cidade aproximada e IP com hash anonimizado conforme LGPD) para uma fila assíncrona em memória (`Channel<T>`).
4. Um serviço em segundo plano grava as métricas no banco de dados em lote, sem retardar a experiência do cliente final.
5. O lojista acompanha os acessos em tempo real na aba **Métricas & Analytics** do seu painel.

---

### Passo 5: Filtro de Avaliações (Feedback Gate)
Quando o lojista escolhe o modo **Feedback Gate**:
1. O cliente final é direcionado para a tela de classificação `http://localhost:5251/feedback/{slug}`.
2. O cliente vê uma tela moderna perguntando *"Como foi sua experiência conosco?"* e 5 estrelas:
   - **Se o cliente avaliar com 4 ou 5 estrelas:** O sistema parabeniza e redireciona imediatamente para a página pública de avaliações do Google Meu Negócio, alavancando a pontuação pública da loja.
   - **Se o cliente avaliar com 1, 2 ou 3 estrelas:** O sistema exibe um formulário de feedback interno para que o cliente descreva o ocorrido ou envie uma mensagem direta ao WhatsApp do gerente. Assim, o estabelecimento resolve a insatisfação internamente sem manchar sua reputação online.

---

## 6. Tabela de Referência Rápida (Portas, URLs e Credenciais)

| Módulo / Serviço | Porta HTTP / Externa | URL de Acesso / Endpoint | Credenciais Padrão |
| :--- | :--- | :--- | :--- |
| **Backend API** | `5180` (HTTP) / `7157` (HTTPS) | `http://localhost:5180` | Autenticação via JWT / Header `X-Api-Key` |
| **Documentação Scalar** | `5180` | `http://localhost:5180/scalar/v1` | Acesso público em ambiente de Dev |
| **Redirecionador** | `5180` | `http://localhost:5180/r/{slug}` | Acesso público |
| **Admin Dashboard** | `5077` (HTTP) / `7203` (HTTPS) | `http://localhost:5077` | `admin@estrelaclick.com.br` / `AdminMaster2026!` |
| **Client Dashboard** | `5251` (HTTP) / `7143` (HTTPS) | `http://localhost:5251` | Acesso público / Onboarding |
| **App Mobile Bancada** | Emulador / Celular | Conecta em `http://<IP>:5180` | `operador` / `operador123` |
| **SQL Server 2022** | `1433` | `localhost,1433` | Usuário: `sa` / Senha: `Your_strong_password123` |
| **Redis 7** | `6379` | `localhost:6379` | Sem senha em desenvolvimento |
| **Chave de API Fábrica** | — | Header `X-Api-Key` | `dev-factory-secret-key-2026` |

---

## 7. Execução de Testes Automatizados

A plataforma possui uma suíte rigorosa com mais de 270 testes automatizados que cobrem invariantes de domínio, casos de uso e fronteiras arquiteturais:

### Executar Testes Unitários e de Arquitetura:
```powershell
dotnet test --filter Category!=Integration
```

### Executar Testes de Arquitetura (NetArchTest):
Garante que as regras de Clean Architecture não foram violadas (por exemplo, `Domain` não referencia frameworks externos):
```powershell
dotnet test tests/ArchitectureTests
```

### Executar Testes de Integração:
*(Requer Docker com SQL Server ativo)*
```powershell
dotnet test tests/IntegrationTests
```

---

## 8. Resolução de Dúvidas e Problemas Comuns (Troubleshooting)

### 1. "Não foi possível conectar ao banco de dados SQL Server"
* Certifique-se de que o contêiner do Docker está em execução:
  ```powershell
  docker compose ps
  ```
* Caso esteja parado, reinicie com `docker compose up -d`.
* Verifique se a porta `1433` não está sendo usada por outra instância local do SQL Server Express.

### 2. "Erro de certificado SSL/HTTPS ao rodar localmente"
Execute o comando para confiar no certificado de desenvolvimento do .NET:
```powershell
dotnet dev-certs https --trust
```

### 3. "O Factory Mobile App no Android não alcança a API (porta 5180)"
* **No Emulador Android Oficial:** O aplicativo utiliza por padrão `http://10.0.2.2:5180` (alias interno do emulador para o host do computador). O tráfego HTTP é permitido pelo `android:usesCleartextTraffic="true"` no `AndroidManifest.xml`.
* **No Dispositivo Físico via Cabo USB (Recomendado):**
  Com a Depuração USB ativada no celular, execute no PowerShell:
  ```powershell
  adb reverse tcp:5180 tcp:5180
  ```
  Isso redireciona o tráfego da porta `5180` do smartphone diretamente para o seu computador via cabo.
* **No Dispositivo Físico via Wi-Fi:** O smartphone e o computador devem estar na **mesma rede Wi-Fi**. Utilize o **IP local da sua máquina** na rede (ex.: `http://192.168.1.50:5180`) e verifique se o Firewall do Windows permite conexões de entrada na porta `5180`.
* **Atenção:** Se o backend já estava em execução, encerre-o e reinicie com `dotnet run --project src/Backend/Api` para carregar as configurações de bind em `0.0.0.0:5180` e a desativação do redirect HTTPS forçado em desenvolvimento.

### 4. "A tag NFC foi aproximada mas acusou erro de gravação"
* Verifique se o chip é do modelo **NTAG213** ou **NTAG215** (padrão NDEF Forum Type 2).
* Verifique se a tag não possui bloqueio permanente de fábrica contra escrita (Lock Bytes acionados).
* Se a placa possuir base metálica, certifique-se de que o adesivo NFC utilizado possui camada anti-metal (**ferrite shield**), caso contrário a superfície metálica anulará o campo magnético da antena.

### 5. "Como alterar o destino de uma placa que já foi vinculada?"
* O lojista pode acessar seu painel em `http://localhost:5251`, selecionar a placa em sua lista e atualizar o endereço de destino (URL).
* A alteração entra em vigor **instantaneamente** para todos os novos toques, sem necessidade de recolher a placa física do balcão.

---

*EstrelaClick — Tecnologia integrada para potencializar a presença física e digital do comércio.*
