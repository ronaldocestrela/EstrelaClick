# ADR 0008 - API de Lotes e Controle de Fila para App Mobile

## Status
Aceito

## Contexto
O item 2.3 do roadmap técnico exige a implementação dos serviços de retaguarda para o ciclo fabril de gravação de placas NFC e QR Code:
1. Geração em massa de placas com slugs aleatórios seguros (`POST /api/placas/gerar-lote`).
2. Fila sequencial (FIFO) com reserva temporária para operadores de bancada ou esteiras automatizadas (`GET /api/placas/proxima-pendente`).
3. Confirmação e registro físico da gravação com UID do chip lido (`POST /api/placas/confirmar-gravacao`).
4. Autenticação e autorização híbrida para o ambiente fabril (`X-Api-Key` para máquinas/esteiras ou `Bearer JWT` para operadores autenticados no app mobile).
5. Exposição e documentação via OpenAPI nativo com interface interativa Scalar em `/scalar/v1`.

## Decisões

1. **Ciclo de Vida da Placa e Status `EmGravacao`:**
   - Adicionada a transição de estado no enum `StatusPlaca`: `Criada (1)` -> `EmGravacao (4)` -> `Gravada (2)` -> `Vinculada (3)`.
   - Na busca da próxima placa pendente, a placa é colocada em reserva com um TTL (Time-To-Live) configurável (padrão de 90 segundos) e associada ao operador solicitante (`OperadorReservaId`).
   - Se uma placa reservada não for confirmada dentro da janela de TTL, ela se torna automaticamente elegível para re-reserva, impedindo que tags fiquem travadas em caso de queda de conexão ou falha física do dispositivo.

2. **Concorrência Segura na Fila de Gravação:**
   - A obtenção da próxima placa é feita via método transacional no repositório (`IPlacaRepository`), garantindo que apenas um operador por vez aloque a mesma placa (evitando duplicações em bancadas concorrentes).
   - A ordenação da fila é estritamente FIFO (`DataCriacaoUtc ASC`), com suporte a filtro opcional por `NumeroLote`.

3. **Geração Segura de Slugs:**
   - A geração em lote cria slugs criptograficamente seguros em formato GUID "N" (32 caracteres hexadecimais minúsculos sem hífens), em estrita conformidade com as invariantes da entidade `Placa` e compatível com o redirecionador ultrarrápido `/r/{slug}`.

4. **Autenticação e Autorização Híbrida Fabril:**
   - Implementado um esquema customizado de autenticação `ApiKeyAuthenticationScheme` com handler `ApiKeyAuthenticationHandler`, verificando o cabeçalho `X-Api-Key` contra chave interna de fábrica.
   - Suporte combinado a tokens `Bearer JWT` para o operador de bancada autenticado.
   - Criada a política de autorização `FactoryAuthPolicy` que autoriza chamadas autenticadas via `ApiKey` OU `JwtBearer`.

5. **Aderência aos Padrões do Repositório (AGENTS.md):**
   - Retorno estrito de `Result<T>` em todos os casos de uso e métodos de domínio, com zero exceções lançadas para fluxos normais de negócio.
   - Mapeamento adequado de erros de domínio/aplicação para ProblemDetails RFC 7807 contendo `errorCode`.
   - Clean Architecture e Arquitetura Hexagonal com isolamento total de dependências em `Domain` e `Application`.
   - Test-Driven Development (TDD) com testes unitários cobrindo o domínio, casos de uso e integração dos endpoints.

## Consequências
- A esteira de gravação mobile ganha resiliência contra concorrência e falhas de comunicação.
- Ambientes automatizados (robôs de gravação) e manuais (operadores com MAUI) podem consumir a mesma API com autenticação adequada.
- A documentação interativa em `/scalar/v1` detalha os esquemas de autenticação e contratos dos endpoints de fábrica.
