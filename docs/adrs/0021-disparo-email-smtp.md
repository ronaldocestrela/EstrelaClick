# ADR 0021: Disparo de E-mails Transacionais via SMTP com MailKit e Padrão Result

## Status
Aceito

## Contexto
A plataforma EstrelaClick necessita de capacidade para disparo de e-mails transacionais (ex.: ativação de contas, redefinição de senhas, alertas operacionais e relatórios para lojistas e administradores).

Para atender a essas necessidades mantendo conformidade com as diretrizes de engenharia do repositório ([AGENTS.md](file:///c:/Users/ronal/source/repos/EstrelaClick/AGENTS.md)):
1. **Isolamento de Camadas (Arquitetura Hexagonal):** A camada `Application` não deve depender de bibliotecas externas de envio nem de implementações de rede. Deve definir apenas contratos (`IEmailSender`), DTOs (`EmailMessageDto`), opções estruturadas (`SmtpOptions`) e erros de domínio (`EmailErrors`).
2. **Eliminação de Exceções de Fluxo (Padrão Result):** O disparo de e-mail nunca deve propagar exceções não tratadas (ex.: timeout de conexão, falha de autenticação ou recusa de destinatário). Falhas devem ser capturadas e convertidas em instâncias de `Result.Failure(Error)`.
3. **Substituição do Obsoleto `System.Net.Mail`:** A documentação oficial do .NET desaconselha o uso de `System.Net.Mail.SmtpClient` em novas aplicações devido à falta de suporte a protocolos modernos, encerramento de conexões assíncronas e incompatibilidade com TLS moderno. A biblioteca open-source de padrão industrial recomendada pela Microsoft é o `MailKit` / `MimeKit`.
4. **Isolamento de Ambiente e Testes Locais:** Em desenvolvimento local, é crucial não disparar e-mails para servidores reais de produção. O ambiente deve permitir interceptação local usando contêineres SMTP (ex.: Mailpit).

## Decisão de Arquitetura

1. **Porta Outbound e DTOs no `Application`:**
   * Interface `IEmailSender` com método assíncrono `Task<Result> SendEmailAsync(EmailMessageDto message, CancellationToken cancellationToken = default)`.
   * Objeto `EmailMessageDto` com validação de destinatário, assunto e suporte a corpos HTML e texto simples.
   * `SmtpOptions` mapeado via Options Pattern do ASP.NET Core a partir da seção `Smtp` (`Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, `Smtp__EnableSsl`, `Smtp__FromEmail`, `Smtp__FromName`).

2. **Adaptador Secundário com MailKit em `Infrastructure`:**
   * Projeto `EstrelaClick.Backend.Infrastructure` referencia a biblioteca `MailKit`.
   * Implementação de `ISmtpClientWrapper` para encapsular a conexão, autenticação e envio do `MailKit.Net.Smtp.SmtpClient`, garantindo 100% de testabilidade unitária e isolamento de I/O em suites de teste TDD.
   * `SmtpEmailSender` implementa `IEmailSender`, tratando rigorosamente exceções (`AuthenticationException`, `SmtpCommandException`, `SocketException`, `TimeoutException`) e convertendo-as em `Result.Failure`.

3. **Ambiente de Desenvolvimento Local com Mailpit:**
   * Adição do serviço `mailpit` ao `docker-compose.yml`, provendo servidor SMTP na porta 1025 e interface gráfica web para inspeção na porta 8025 (`http://localhost:8025`).
   * Configuração padrão no `.env.example` apontando para o servidor local na porta 1025 sem autenticação obrigatória em modo de desenvolvimento.

## Consequências

### Positivas
* Código desacoplado e aderente à Clean Architecture / Ports & Adapters.
* Tratamento funcional de erros: nenhuma exceção de rede ou autenticação estoura para a camada de apresentação ou UseCases.
* Suporte a STARTTLS, SSL/TLS implícito e explícito através de MailKit moderno.
* Ambiente de desenvolvimento totalmente autônomo com Docker e Mailpit.
* Testabilidade unitária completa via mocks de `ISmtpClientWrapper` e validação prévia de invariantes da mensagem.

### Negativas / Mitigações
* Adição da dependência `MailKit` em `Infrastructure` (mitigada por ser o padrão de mercado recomendado pela Microsoft para .NET 10).
