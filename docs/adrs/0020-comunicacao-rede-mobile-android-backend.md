# ADR 0020: Comunicação de Rede entre Factory Mobile App (Android) e Backend Local

## Status
Aceito

## Contexto
O aplicativo móvel operacional de fábrica (`FactoryMobileApp`) desenvolvido em .NET MAUI conecta-se ao backend da solução (`Backend.Api`) em ambiente de desenvolvimento local (porta 5180) para autenticar operadores, sincronizar a fila de placas pendentes e confirmar gravações NDEF em chips NTAG213/215.

Durante os testes no Android, foram identificados impedimentos técnicos que impediam a comunicação entre o aplicativo e a API:
1. **Bloqueio de Tráfego Cleartext (HTTP puro):** Desde o Android 9 (API 28), o sistema operacional bloqueia por padrão todo o tráfego HTTP sem criptografia (`Cleartext HTTP traffic not permitted`). Como o desenvolvimento local na porta 5180 roda em HTTP, as chamadas eram rejeitadas no nível de socket do Android.
2. **Redirecionamento HTTPS Indiscriminado:** A API chamava `app.UseHttpsRedirection()` sem restrição de ambiente, disparando redirecionamentos HTTP 307/308 para URLs HTTPS cujo certificado de desenvolvimento não é confiável pelo Android, além de redirecionar para `localhost` (que no Android referencia o próprio dispositivo).
3. **Escuta de Interface de Rede Kestrel (`localhost` vs `0.0.0.0`):** No `launchSettings.json`, o Kestrel escutava exclusivamente em `localhost` (127.0.0.1), rejeitando chamadas vindas de pontes de rede do emulador ou de dispositivos físicos na LAN Wi-Fi.
4. **Resolução de Host em Dispositivos Físicos vs Emuladores:** O emulador oficial do Android utiliza o gateway `10.0.2.2` para acessar o host do computador. Dispositivos físicos conectados via USB, por outro lado, requerem o encaminhamento de porta `adb reverse` ou o IP da máquina na rede local.

## Decisão de Arquitetura

1. **Habilitação de Tráfego Cleartext no Android:**
   * Configuração de `android:usesCleartextTraffic="true"` no `AndroidManifest.xml` do projeto `FactoryMobileApp`, permitindo que o app se comunique com o endpoint HTTP local da esteira fabril.

2. **Condicionamento de `UseHttpsRedirection` a Ambientes de Não-Desenvolvimento:**
   * No `Program.cs` da `Backend.Api`, `app.UseHttpsRedirection()` passa a ser executado apenas fora do ambiente `Development` (`if (!app.Environment.IsDevelopment())`). Em desenvolvimento, as chamadas HTTP à porta 5180 são atendidas diretamente sem redirects forçados.

3. **Binding Kestrel em Todas as Interfaces Locais:**
   * No `launchSettings.json` da `Backend.Api`, a URL do Kestrel no perfil de desenvolvimento passa a ser configurada com `http://0.0.0.0:5180`, aceitando requisições do emulador Android (`10.0.2.2`), de `localhost` e de IPs locais da rede (ex.: `192.168.x.x`).

4. **Resolução Dinâmica e Configurável no MAUI (`MauiProgram.cs`):**
   * Configuração padrão no Android para `http://10.0.2.2:5180` (emulador) e fallback para `Preferences.Default.Get("ApiBaseUrl", ...)` caso o desenvolvedor opte por utilizar o IP local da LAN ou aparelho físico conectado via cabo USB com `adb reverse tcp:5180 tcp:5180`.

## Consequências

### Positivas
* O aplicativo .NET MAUI no Android comunica-se perfeitamente com a API local em `http://10.0.2.2:5180`.
* Suporte nativo tanto para emuladores quanto para aparelhos físicos via USB (`adb reverse`) ou via rede Wi-Fi local.
* Preservação da segurança em produção, onde `UseHttpsRedirection` permanece ativo.

### Negativas / Mitigações
* Para depuração com aparelho físico via USB, o desenvolvedor deve executar `adb reverse tcp:5180 tcp:5180` no terminal ou configurar o IP de sua máquina na rede local.
