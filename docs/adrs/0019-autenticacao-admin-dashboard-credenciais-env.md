# ADR 0019: Autenticação do Admin Dashboard com Credenciais Master via .env

## Status
Aceito

## Contexto
O painel administrativo (`AdminDashboard`) do monólito modular EstrelaClick concentra operações de alta criticidade da fábrica: geração de lotes de placas NFC/QR Code, auditoria de UID/slug e cadastro de empresas clientes.
Anteriormente, o `AdminDashboard` operava em modo de prototipagem aberta sem camada de autenticação, o que impedia o controle de acesso e auditoria das ações de fábrica.

Para o provisionamento e operação inicial do sistema pela equipe de fábrica/engenharia, é necessário um mecanismo de autenticação para um usuário master administrador cujas credenciais sejam injetadas via variáveis de ambiente (`.env`), sem necessidade de fluxo complexo de cadastro prévio no banco de dados para esse perfil de operador master.

## Decisão de Arquitetura

1. **Identificador Unificado com E-mail Corporativo e Credenciais Injetadas via `.env`:**
   * O identificador de acesso (`Username`) do administrador master é obrigatoriamente idêntico ao seu endereço de e-mail corporativo (ex.: `admin@estrelaclick.com.br`).
   * A chave de configuração `AdminSecurity:MasterUser` no arquivo `.env` recebe o e-mail corporativo do administrador master.
   * Na validação de credenciais, o e-mail/username é normalizado (*trim* e conversão para minúsculas invariantes / *lowercase*) para garantir que a identificação seja insensível a maiúsculas/minúsculas no formato padrão de e-mails, mantendo a senha estritamente sensível a maiúsculas e minúsculas (*case-sensitive*).
   * Caso as credenciais não estejam configuradas ou estejam em branco no `.env`, o serviço de autenticação recusa autenticações retornando erro explícito (`AdminAuth.ConfiguracaoInvalida`) via padrão `Result<T>`.

2. **Segurança Criptográfica e Comparação em Tempo Constante:**
   * A comparação das credenciais enviadas com os valores configurados utiliza hash SHA256 com `CryptographicOperations.FixedTimeEquals` para mitigar ataques de temporização (*timing attacks*).
   * Senhas e segredos nunca são registrados em logs nem trafegados em URLs.

3. **Mecanismo de Autenticação em Blazor Server:**
   * Utilização do esquema padrão `CookieAuthenticationDefaults.AuthenticationScheme` do ASP.NET Core integrado ao Blazor Server via `AddAuthentication`, `AddAuthorization` e `AddCascadingAuthenticationState`.
   * Criação de endpoints locais Minimal API (`/auth/admin-login` e `/auth/admin-logout`) para emissão e revogação de cookies HTTP seguros (`HttpOnly`, `SameSite=Lax`, expiração configurável).
   * As páginas administrativas (`/`, `/lotes`, `/clientes`, `/auditoria`) recebem a anotação `@attribute [Authorize]` e o roteador `Routes.razor` emprega `AuthorizeRouteView` com redirecionamento de usuários não autenticados para `/login`.

4. **Design Tokens e Segregação de Layout:**
   * A página de autenticação (`Login.razor`) utiliza um layout dedicado (`AuthLayout.razor`), isolando a área pública da navegação administrativa (`NavMenu.razor`).
   * Toda a estilização obedece estritamente às variáveis do `tokens.css` de `Shared.UI`.
   * O layout administrativo exibe o nome do usuário autenticado e botão de encerramento de sessão (*Logout*).

## Consequências

### Positivas
* O painel administrativo fica protegido contra acesso não autorizado.
* Simplicidade de configuração no ambiente de desenvolvimento e produção via `.env`.
* Zero exceções para fluxo de autenticação, aderindo integralmente ao padrão `Result<T>` do `AGENTS.md`.
* Suporte nativo aos temas claro/escuro através dos tokens da solução.

### Negativas / Mitigações
* Como as credenciais residem no `.env`, a alteração da senha do administrador master exige reinício do processo ou container. Em versões futuras, poderá ser adicionada autenticação multifator ou integração com tabela de operadores.
