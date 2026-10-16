# ADR 0002 - Padrão Result<T>

## Status
Aceito

## Contexto
As regras de arquitetura exigem que fluxo de domínio/aplicação não use exceções para regras esperadas.

## Decisão
Adotar um contrato canônico `Result`/`Result<T>` + `Error` no `SharedKernel` para representar sucesso/falha de forma explícita.

## Consequências
- Tratamento previsível de erros de negócio.
- Mapeamento consistente de falhas para HTTP na camada de API.
- Menor acoplamento com exceções para controle de fluxo.

## Implementação
- `Error`, `Result` e `Result<T>` implementados em `src/Shared/SharedKernel/Results`.
- Entidades de domínio (`Placa`, `Empresa`, `AcessoMetrica`) adotam `Result`/`Result<T>` para falhas esperadas de invariantes.
