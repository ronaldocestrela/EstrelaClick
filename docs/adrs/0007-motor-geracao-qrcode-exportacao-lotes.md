# ADR 0007 - Motor de Geração de QR Code e Exportação de Lotes

## Status
Aceito

## Contexto
O item 2.2 do roadmap técnico exige a geração de QR Codes em alta definição (PNG a 300 DPI) para envio a gráficas, geração vetorial em SVG para corte e gravação a laser, e suporte à exportação em lote compactada (.zip). A solução precisa ser executável em ambientes Linux/Docker sem dependências do subsistema gráfico legado do Windows (como `System.Drawing.Common`), respeitar a Clean Architecture / Hexagonal, o padrão `Result<T>` e fornecer rastreabilidade direta por número de lote.

## Decisão
1. **Isolamento de Bibliotecas de Terceiros e Arquitetura Hexagonal:**
   - A biblioteca de geração de QR Code (`QRCoder`) é estritamente referenciada apenas na camada `Infrastructure`.
   - A camada `Application` define a porta de saída (`IQrCodeGenerator`), expondo métodos que operam com primitivas seguras (`Result<byte[]>`).
2. **Compatibilidade Cross-Platform (Linux / Docker / .NET 10):**
   - Utilizar exclusivamente geradores sem dependência de GDI+ / `System.Drawing`:
     - `PngByteQRCode` para renderização matricial direta de bytes PNG.
     - `SvgQRCode` para renderização vetorial XML limpa.
3. **Resolução de Impressão Gráfica (300 DPI):**
   - O formato PNG por padrão não armazena DPI explicitamente a menos que o chunk físico `pHYs` esteja presente.
   - O adaptador de infraestrutura injeta o chunk `pHYs` padrão RFC/ISO (11.811 pixels por metro, correspondente a 300 DPI) no fluxo binário do PNG, garantindo que birôs gráficos e softwares de diagramação leiam o tamanho físico correto sem distorções de escala.
4. **Modelagem de Lote no Domínio:**
   - Adicionar a propriedade `NumeroLote` à entidade `Placa`, garantindo integridade e indexação no banco de dados (`IX_Placas_NumeroLote`).
   - A geração e exportação de QR Codes por lote são orquestradas pelo caso de uso `ExportarLoteQrCodeZipUseCase`, que busca as placas pelo número do lote e compõe o arquivo comprimido.
5. **Estrutura do Pacote Compactado (.zip):**
   - Para atender aos requisitos de gráfica e laser de forma segregada, o arquivo `.zip` gerado contém:
     - Pasta `png/`: imagens `{slug}.png` em 300 DPI.
     - Pasta `svg/`: arquivos vetoriais `{slug}.svg`.
     - Arquivo `manifest.csv`: manifesto contendo cabeçalho `Slug;UrlRedirecionamento;NumeroLote` para conferência do birô de produção.
6. **Padronização Result<T>:**
   - Qualquer falha na validação de dados de lote ou geração de arquivos é propagada via `Result<T>` com erros funcionais (`QrCodeErrors`), sem lançamento de exceções de fluxo.

## Consequências
- A solução roda de forma totalmente agnóstica de SO em contêineres Linux/.NET 10.
- As gráficas e oficinas de corte a laser recebem os insumos padronizados em escala correta e sem necessidade de conversão manual.
- Alto desacoplamento e conformidade com testes de arquitetura e cobertura TDD superior a 85%.
