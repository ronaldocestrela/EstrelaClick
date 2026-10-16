# ADR 0010 - Interface do Operador de Bancada no .NET MAUI

## Status
Aceito

## Contexto
O item 3.2 do roadmap técnico estabelece a interface móvel (.NET MAUI) da bancada fabril para gravação de placas NFC/QR Code:
1. Autenticação integrada à API para identificação segura do operador de bancada.
2. Painel em tempo real exibindo métricas operacionais essenciais: Total do Lote, Quantidade Gravada na Sessão e Identificador (Slug) & URL da placa ativa.
3. Controle de modo operacional: alternância entre Gravação Manual e Gravação Contínua com avanço automático na fila.
4. Total aderência ao `AGENTS.md`: arquitetura MVVM desacoplada, padrão `Result<T>` sem exceções de fluxo, e estilização estritamente orientada a Design Tokens (`tokens.css`, `Colors.xaml`).

## Decisões

1. **Autenticação e Sessão do Operador:**
   - Criação do endpoint `POST /api/auth/operador-login` que valida as credenciais do operador e emite token JWT assinado compatível com a política `FactoryAuthPolicy`.
   - O aplicativo mantém a sessão ativa via `IOperatorSessionService` (`OperatorSessionService`), armazenando o token JWT e controlando o tempo de vida da sessão e a contagem de tags gravadas na sessão.

2. **Arquitetura MVVM e Desacoplamento:**
   - Os ViewModels (`LoginViewModel`, `BancadaViewModel`) são implementados como modelos observáveis sem dependência do runtime do MAUI, permitindo cobertura de 100% via testes unitários automatizados com xUnit.
   - A comunicação com a API é centralizada em `IFactoryApiClient` (`FactoryApiClient`), mapeando códigos HTTP diretamente para `Result<T>` com tipos canônicos de `SharedKernel`.

3. **Métricas de Lote em Tempo Real:**
   - Exposição do endpoint `GET /api/placas/lotes/{numeroLote}/resumo` na API de fábrica, permitindo que a bancada calcule e exiba instantaneamente o total de placas do lote, quantidade gravada, pendente e vinculada.
   - O painel exibe cartões dedicados para **Total Lote**, **Na Sessão** e **Pendentes**.

4. **Modo Gravação Contínua:**
   - A interface dispõe de um seletor visual (*Switch*) para alternar entre o modo manual e o modo contínuo.
   - Quando ativado, o aplicativo fica preparado para avançar automaticamente na fila de gravação após o término com sucesso de cada ciclo de gravação NDEF (item 3.3).

5. **Tokenização Visual e UI Premium:**
   - As cores, tipografia, espaçamentos e raios de borda das telas `LoginPage` e `BancadaPage` utilizam tokens centralizados (`EstrelaPrimary`, `EstrelaAccent`, `EstrelaSurface`, `EstrelaBackground`, `EstrelaSuccess`, etc.), evitando valores arbitrários no XAML.

## Consequências
- A bancada de produção ganha uma interface reativa, limpa e responsiva para o operador industrial.
- A segurança de ponta a ponta é garantida através do JWT injetado nas requisições da fábrica.
- Toda a lógica de negócios da interface é testada unitariamente via TDD antes da execução no hardware.
