# ADR 0006 - Motor de Redirecionamento Ultrarrápido, HybridCache e Barramento Assíncrono de Métricas

## Status
Aceito

## Contexto
O endpoint de redirecionamento `GET /r/{slug}` é o ponto mais crítico de desempenho da plataforma EstrelaClick, pois é acionado a cada leitura física de tags NFC ou escaneamento de QR Code. A regra inegociável do sistema exige tempo de resposta inferior a 25 ms, com telemetria e métricas desacopladas para não onerar o tempo de resposta percebido pelo usuário final.

## Decisão
1. **Cache em Dois Níveis com HybridCache (.NET 10):**
   - Utilizar a nova biblioteca `Microsoft.Extensions.Caching.Hybrid` para gerenciar o cache de resolução de slugs em L1 (memória local ultra-rápida) com suporte a L2 distribuído e proteção nativa contra *cache stampede*.
   - Chave de cache padronizada: `slug:{slug}`.
2. **Barramento Assíncrono Desacoplado via System.Threading.Channels:**
   - Adotar um `BoundedChannel<MetricaAcessoEvent>` em memória com capacidade limitada (50.000 itens) e política `BoundedChannelFullMode.DropWrite`.
   - Isso garante que, mesmo sob pico extremo ou indisponibilidade temporária do banco de dados, o endpoint de redirecionamento permaneça não-bloqueante e com tempo de resposta $< 25\text{ ms}$.
3. **Persistência em Lote (Batch Insert) via BackgroundService:**
   - Um worker em segundo plano (`MetricasBatchProcessorWorker`) drena o canal em lotes (até 100 itens ou a cada 2 segundos) e persiste via EF Core no SQL Server.
4. **Tratamento de Ciclo de Vida da Placa:**
   - Placa `Vinculada`: HTTP 302 (Found) redirecionando para `UrlDestino` e enfileirando métrica.
   - Placa `Gravada` (sem vínculo com cliente): HTTP 422 (Unprocessable Entity) com `ProblemDetails` funcional indicando que a placa ainda não foi ativada.
   - Placa `Criada` ou inexistente: HTTP 404 (Not Found).
5. **Privacidade e LGPD:**
   - O endereço IP não é gravado em texto puro; é calculado um hash criptográfico (SHA-256 com salt) gerando `IpHash` para respeitar a privacidade e a LGPD.

## Consequências
- Resposta sub-25ms garantida pela leitura imediata de cache e enfileiramento $O(1)$ sem I/O de banco na requisição HTTP.
- Zero bloqueio na requisição do usuário em caso de lentidão de I/O de banco.
- Despejo eficiente em lote reduz drasticamente o número de transações e conexões no SQL Server.
