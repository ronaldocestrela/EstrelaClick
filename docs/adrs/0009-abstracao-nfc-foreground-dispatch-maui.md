# ADR 0009 - Abstração de Hardware NFC e Foreground Dispatch no .NET MAUI

## Status
Aceito

## Contexto
O item 3.1 do roadmap técnico estabelece as bases de hardware móvel no aplicativo .NET MAUI (`FactoryMobileApp`) para gravação contínua em bancada fabril de tags NFC (NTAG213 e NTAG215):
1. Configuração de permissões e capacidades de hardware no manifesto Android (`AndroidManifest.xml`).
2. Implementação de serviços nativos de leitura e gravação NDEF (`NfcAdapter`, `Ndef`, `NdefRecord`).
3. Configuração de intents em primeiro plano (*Foreground Dispatch*) para captura contínua e sem interrupção pela UI do sistema operacional.
4. Total aderência às regras do `AGENTS.md`: aplicação do padrão `Result<T>` sem exceções lançadas para falhas de aproximação ou gravação, e desacoplamento do hardware para permitir testes unitários (TDD).

## Decisões

1. **Abstração Multiplataforma (`INfcService`):**
   - Criação da interface `INfcService` desacoplada das APIs nativas do Android/Java.
   - Todo fluxo de ativação, leitura, detecção de tag e escrita retorna `Result<T>` utilizando os tipos canônicos de `SharedKernel`.
   - Códigos de erro bem definidos através de `NfcErrors` (`Nfc.NotSupported`, `Nfc.Disabled`, `Nfc.TagLost`, `Nfc.TagReadOnly`, `Nfc.CapacityExceeded`, `Nfc.InvalidUri`, etc.).

2. **Captura Contínua via Foreground Dispatch:**
   - A `MainActivity` intercepta o ciclo de vida do Android:
     - `OnResume`: ativa `NfcAdapter.EnableForegroundDispatch` com um `PendingIntent` e filtros para `ActionNdefDiscovered`, `ActionTechDiscovered` e `ActionTagDiscovered`.
     - `OnPause`: chama `NfcAdapter.DisableForegroundDispatch` liberando o sensor enquanto o app estiver em segundo plano.
     - `OnNewIntent`: extrai o objeto nativo `Android.Nfc.Tag` e dispara o evento no serviço via barramento interno seguro.
   - Isso evita que o Android abra diálogos padrão ("Abrir com...") ou feche a tela da aplicação durante a passagem sucessiva de placas de bancada.

3. **Formatação de Registros NDEF:**
   - Utilização de `NdefRecord` do tipo Well-Known URI (`RTD_URI`) padronizado com prefixo `https://` (`0x04`) para economia de bytes nas memórias limitadas dos chips NTAG213 (144 bytes) e NTAG215 (504 bytes).
   - Extração do UID do chip em formato hexadecimal limpo padronizado em letras maiúsculas com separador `:` (ex.: `04:A3:8C:F2:1E:5B:80`).

4. **Tratamento de Exceções de Hardware com `Result<T>`:**
   - Exceções nativas (`TagLostException`, `FormatException`, `IOException`) são interceptadas dentro do adaptador nativo e convertidas de forma determinística para `Result<T>.Failure(NfcErrors.*)`.

## Consequências
- A camada de apresentação (`ViewModels` e `Views`) interage exclusivamente com a abstração `INfcService`, garantindo facilidade de teste unitário via Mocks/NSubstitute.
- A bancada fabril opera em modo contínuo de alta cadência, processando aproximações de tags sem fricção de navegação no Android.
