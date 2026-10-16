# ADR 0003 - Modelagem inicial de entidades e invariantes de domínio

## Status
Aceito

## Contexto
A fase 1.2 do roadmap exige a modelagem das entidades centrais (`Empresa`, `Placa`, `AcessoMetrica`) com invariantes explícitas e transições de estado seguras, sem uso de exceções para fluxo de negócio.

## Decisão
Adotar entidades ricas no `Domain` com criação/mutação orientadas por `Result`/`Result<T>`, estabelecendo as seguintes regras iniciais:
- `Placa.Slug` deve ser GUID no formato `N` (sem hífens).
- Fluxo de status da placa estritamente sequencial: `Criada -> Gravada -> Vinculada`.
- `RegistrarGravacao(chipUid)` exige `chipUid` válido e status `Criada`.
- `VincularEmpresa(empresaId, urlDestino)` exige status `Gravada`, `empresaId` válido e URL absoluta HTTP/HTTPS.
- `Empresa` valida `RazaoSocial`, `Email` e `Telefone`; vínculo de identidade é contrato de domínio (`IdentityUserId`) sem acoplamento técnico nesta fase.
- `AcessoMetrica` exige `PlacaId` válido e `IpHash` obrigatório.

## Consequências
- Invariantes passam a ser protegidas no núcleo de domínio.
- Regras de transição ficam centralizadas e testáveis por TDD.
- A integração técnica com ASP.NET Core Identity e EF Core permanece para as fases de Infrastructure/Persistência.
