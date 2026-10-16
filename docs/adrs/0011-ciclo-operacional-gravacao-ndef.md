# ADR 0011 - Ciclo Operacional de Gravação NDEF e Proteção por Senha no .NET MAUI

## Status
Aceito

## Contexto
O item 3.3 do roadmap técnico define o ciclo operacional automatizado de gravação NDEF para a bancada industrial com o aplicativo .NET MAUI:
1. Buscar e engatilhar a próxima placa livre na fila (`GET /api/placas/proxima-pendente`).
2. Montar o payload NDEF URI com prefixo `0x04` (`https://`).
3. Gravar na tag NFC (NTAG213 / NTAG215) por aproximação e indução magnética.
4. **Proteção de Regravação com Senha:** As tags devem ser protegidas contra sobrescrita não autorizada por terceiros, mas **podem ser regravadas** pela fábrica mediante uso de senha (autenticação PWD/PACK do NTAG213/215), em vez de bloqueio permanente e irreversível (`lockTag: false` com configuração de senha de escrita).
5. **Modo Contínuo como Padrão:** Para máxima cadência operacional em esteira/bancada fabril, o aplicativo deve operar por padrão no modo contínuo, iniciando a fila automaticamente e avançando sem interação em tela após cada bipe de sucesso.
6. **Feedback Sensorial Duplo:** Sinalização tátil (vibração curta no sucesso, dupla no erro) e sonora (bipe industrial de leitor de código de barras no sucesso, tom grave de alerta no erro) via `ISensoryFeedbackService`.
7. **Resiliência e Tratamento de Falhas:** Tratamento funcional com `Result<T>` sem exceções para casos como tag retirada rápido demais (`TagLost`), tag já gravada/bloqueada (`TagReadOnly`), ou timeout de rede na confirmação.

## Decisões

1. **Abstração Sensorial (`ISensoryFeedbackService`):**
   - Criação da interface `ISensoryFeedbackService` em `SharedKernel.Mobile` com `PlaySuccessFeedbackAsync()` e `PlayFailureFeedbackAsync()`.
   - Implementação Android nativa em `AndroidSensoryFeedbackService` utilizando `Vibrator` com `VibrationEffect` e `ToneGenerator` com `Stream.Music` (`Tone.PropBeep` para sucesso e `Tone.PropNack` para falha).
   - Implementação de fallback em `DummySensoryFeedbackService` para ambientes de desenvolvimento desktop e testes sem crash.

2. **Proteção por Senha em Tags NTAG213/215:**
   - As tags da linha NXP NTAG213/215 possuem registros de configuração de proteção por senha:
     - 4 bytes de senha (`PWD`, página `0xE5` no NTAG213 ou `0x85` no NTAG215).
     - 2 bytes de acknowledgment (`PACK`, página `0xE6` ou `0x86`).
     - Byte de controle de proteção de escrita (`PROT` em `AUTH0`), configurado para proteger apenas as operações de escrita (`PROT = 1b`), permitindo leitura NDEF pública irrestrita por smartphones de clientes finais.
   - O método `WriteUriRecordAsync` em `INfcService` recebe parâmetros de senha para aplicar a proteção de escrita durante o ciclo de gravação industrial.

3. **Orquestração Reativa no `BancadaViewModel`:**
   - Injeção de `INfcService`, `ISensoryFeedbackService`, `IFactoryApiClient` e `IOperatorSessionService`.
   - Inicialização do `ModoGravacaoContinua` como `true` por padrão.
   - Tratamento do evento `TagDetected` de forma desacoplada através do método `ProcessarTagAproximadaAsync(string chipUid, CancellationToken ct)`.
   - Suporte a guardas contra reentrância e tratamento resiliente de falhas de gravação sem desassociar a placa ativa até a confirmação exitosa.

4. **Automação Contínua da Esteira:**
   - Ao inicializar a bancada e após cada confirmação de sucesso na API (`POST /api/placas/confirmar-gravacao`), o `BancadaViewModel` em modo contínuo aciona imediatamente `CarregarProximaPlacaAsync()`, mantendo o operador focado unicamente no gesto físico de aproximação e retirada das placas na bancada.

## Consequências
- A cadência de produção atinge o tempo ótimo de ciclo (~200 a 400ms por gravação NDEF + confirmação assíncrona).
- Os clientes finais leem a URL da placa instantaneamente sem necessidade de senha ou chave especial.
- As placas ficam protegidas contra adulteração acidental ou intencional por aplicativos de terceiros (como NFC Tools), mas a fábrica pode regravá-las a qualquer momento fornecendo o PWD autorizado.
- A arquitetura preserva 100% de testabilidade unitária e isolamento de camadas exigido pelo `AGENTS.md`.
