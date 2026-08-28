# Card Metroidvania

Prototipo Unity de um metroidvania 2D com foco em combate corpo a corpo, movimento baseado em momentum, combate aereo e um sistema de cartas usado durante combos.

O projeto ainda esta em fase inicial de documentacao, memoria de design e prototipo tecnico. Este README existe para orientar revisores sobre quais documentos devem ser lidos primeiro e como interpretar a estrutura versionada.

## Leitura Recomendada

Para entender o estado atual do projeto, leia nesta ordem:

1. O `gdd-canonico` mais recente em `gdd/`
2. A especificacao de arquitetura de prototipo mais recente em `specs/`
3. A especificacao de convencoes de codigo mais recente em `specs/`
4. Specs especificas em `specs/`, conforme o sistema em revisao
5. Documentos historicos em `.docs/`, apenas se precisar rastrear origem ou contexto academico

## Documento Principal Atual

O documento correto para entender as decisoes atuais de design e sempre o `gdd-canonico` mais recente dentro de `gdd/`.

Os arquivos canonicos seguem o padrao:

- `gdd/gdd-canonico-YYYYMMDD-HHMM.md`

Escolha o arquivo com o maior timestamp. Esse documento deve conter as decisoes atuais de design, incluindo:

- fantasia sci-fi/medieval;
- protagonista perdido no tempo ou em uma dimensao conectada a outro tempo;
- prioridade da habilidade do jogador sobre a habilidade do personagem;
- cartas como amplificacao de combos;
- combate inspirado em Hollow Knight e Nine Sols;
- movimento com momentum, mas feeling firme;
- pipeline visual com rig/posing e cleanup manual em pixel art.

Versoes anteriores em `gdd/` devem ser tratadas como memoria do projeto, nao como fonte principal.

## Cronograma Atual Da Build

O plano de producao vigente para a build de 24/11/2026, incluindo o estado
avaliado do repositorio, checklists e marcos semanais, esta em
[`gdd/cronograma-build-novembro-20260828-1401.md`](gdd/cronograma-build-novembro-20260828-1401.md).

## Estrutura De Memoria

### `.docs/`

Contem documentos originais, extraidos ou historicos.

Use esta pasta como fonte de contexto, nao como referencia canonica imediata. Ela preserva ideias, analises e materiais academicos que podem explicar de onde certas decisoes vieram.

### `gdd/`

Contem documentos de design e memoria de design versionados.

Arquivos com `gdd-canonico-YYYYMMDD-HHMM.md` representam versoes canonicas em momentos especificos. Para revisar o design atual, escolha o `gdd-canonico` com timestamp mais recente.

Arquivos com `review` ou nomes semelhantes registram analises, riscos, problemas ou reorganizacoes sugeridas.

### `specs/`

Contem especificacoes tecnicas e planejamento de sistemas.

Use esta pasta quando a revisao envolver arquitetura, codigo Unity, convencoes, testes ou implementacao de subsistemas.

As specs tambem seguem versionamento por timestamp. Quando houver mais de uma versao da mesma familia de documento, use a mais recente.

Familias atuais:

- `prototype-architecture-sdd-*`: arquitetura geral do prototipo.
- `damage-system-sdd-*`: sistema de dano.
- `event-architecture-layout-*`: arquitetura de eventos.
- `testing-conventions-*`: convencoes de testes.
- `unity-script-asset-file-layout-*`: layout esperado para scripts/assets Unity.
- `code-conventions-*`: convencoes de codigo.

## Como Escolher O Arquivo Certo

Use a regra abaixo:

| Necessidade | Ler primeiro |
| --- | --- |
| Entender o jogo atual | `gdd/`, arquivo `gdd-canonico-*` mais recente |
| Entender decisoes antigas ou origem das ideias | `.docs/` e arquivos de review em `gdd/` |
| Revisar arquitetura Unity | `specs/`, familia `prototype-architecture-sdd-*` mais recente |
| Revisar codigo C# | `specs/`, familia `code-conventions-*` mais recente |
| Revisar sistema de dano | `specs/`, familia `damage-system-sdd-*` mais recente |
| Revisar eventos | `specs/`, familia `event-architecture-layout-*` mais recente |
| Revisar testes | `specs/`, familia `testing-conventions-*` mais recente |

## Regra De Versionamento

Mudancas semanticas em documentos de design ou especificacao devem gerar um novo arquivo timestampado em vez de sobrescrever a versao anterior.

Formato recomendado:

- `gdd/<documento>-YYYYMMDD-HHMM.md`
- `specs/<documento>-YYYYMMDD-HHMM.md`

Correcoes pequenas de typo podem ser feitas no proprio arquivo quando nao alterarem o significado.

## Estado Do Prototipo

O repositorio ja possui uma primeira base tecnica em Unity, incluindo estrutura de arquitetura, eventos, dano, dados e testes edit mode.

O objetivo imediato do prototipo e validar se movimento, ataque basico e cartas em combo conseguem demonstrar que a habilidade do jogador e mais importante que a habilidade do personagem.
