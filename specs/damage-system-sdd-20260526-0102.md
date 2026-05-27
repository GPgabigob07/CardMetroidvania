# Damage System SDD - 20260526-0102

## Contexto

Esta especificacao define uma proposta data-driven para dano, amplificacao de dano e notificacoes de dano causado/recebido. Ela complementa:

- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/code-conventions-20260526-0014.md`
- `gdd/gdd-review-20260525-2143.md`
- `.docs/GDD-TIC.md`

O GDD possui uma formula de dano com foco em amplificacao ofensiva:

```text
Dano = ((Golpe% + BonusG%) * (Ataque * (1 + buffs%)) + dano flat) * (1 + DANO%) * CritValue
```

O prototipo nao precisa implementar todos os eixos de amplificacao imediatamente, mas a arquitetura deve permitir que eles existam sem reescrever o sistema.

## Objetivo

Criar um sistema onde:

- dano seja resolvido por um orquestrador puro, sem depender diretamente do ciclo de vida Unity;
- dados base venham de ScriptableObjects;
- modificadores temporarios e buffs possam afetar uma ou varias instancias de dano;
- atores sejam notificados quando causam dano efetivo;
- alvos sejam notificados quando recebem dano;
- eventos globais sejam publicados para HUD, audio, camera e debug;
- a ordem de resolucao para multiplos alvos seja explicita e testavel.

## Conceitos

### DamageProfileSO

Asset de definicao base. Representa o "tipo/forma" do dano:

- id;
- nome;
- descricao;
- dano base opcional;
- hit stop;
- knockback;
- tags de dano;
- multiplicador de golpe base;
- flags de comportamento, se necessario.

Exemplos:

- `Damage_Player_LightAttack_01`
- `Damage_Player_AirFinisher`
- `Damage_Card_Storm`
- `Damage_Enemy_Spike`

### DamageInstance

Representa uma instancia ofensiva reutilizavel. E a "oferta de dano" antes de escolher alvos especificos.

Um `DamageInstance` pode gerar uma ou mais `DamageRequest`.

Exemplo: um golpe em arco atinge tres inimigos. O ataque cria uma unica instancia de dano, e essa instancia e resolvida contra tres alvos em uma request multi-target.

Campos sugeridos:

```text
DamageInstance
- InstanceId
- SourceObject
- DamageProfileSO
- BaseAttackValue
- StrikeMultiplierPercent
- StrikeBonusPercent
- AttackBuffPercent
- FlatDamage
- FinalDamagePercent
- CritValue
- Tags
- MaxTargets
- TargetPriorityMode
- SourceSnapshot
```

Notas:

- `SourceSnapshot` congela valores relevantes no momento da criacao da instancia.
- Isso evita que buffs mudem no meio de uma resolucao multi-hit de forma acidental.
- Modificadores podem declarar se afetam a instancia inteira, cada alvo ou apenas dano efetivo.

### DamageRequest

Representa uma execucao concreta de uma instancia contra um ou mais alvos.

Campos sugeridos:

```text
DamageRequest
- DamageInstance
- CandidateTargets
- HitPoint
- Direction
- RequestTags
- TargetLimit
- TargetPriorityMode
- AllowPartialResolution
```

Uma request pode:

- resolver uma instancia contra um alvo;
- resolver uma instancia contra varios alvos;
- ordenar alvos por prioridade;
- parar apos atingir limite;
- continuar mesmo se um alvo recusar dano.

### DamageContext

Payload entregue ao `IDamageable`. Deve conter o resultado ofensivo ja calculado para aquele alvo.

Campos sugeridos:

```text
DamageContext
- SourceObject
- TargetObject
- DamageProfileSO
- RawAmount
- FinalAmount
- HitPoint
- Direction
- Tags
- InstanceId
- TargetIndex
- WasCritical
```

### DamageResult

Resultado retornado pelo alvo:

```text
DamageResult
- Accepted
- Killed
- AppliedAmount
- RemainingHealth
- WasBlocked
- WasParried
- TargetObject
```

### DamageResolutionReport

Resultado completo da request:

```text
DamageResolutionReport
- DamageInstance
- Request
- PerTargetResults
- TotalAppliedAmount
- EffectiveHitCount
- KilledTargets
```

Esse relatorio e importante para atualizar o ator do dano e alimentar regras como:

- buff por hits efetivos;
- carga gerada por dano causado;
- cartas que terminam apos causar dano;
- efeitos que disparam ao matar alvo;
- chain/rebound damage.

## Contratos

### IDamageProvider

Fonte ofensiva consultavel.

Responsabilidades:

- fornecer ataque base;
- fornecer tags ofensivas;
- fornecer modificadores ativos;
- receber notificacao de resultado ofensivo.

Metodos/propriedades sugeridos:

```csharp
float AttackValue { get; }
GameplayTagSet OffensiveTags { get; }
IEnumerable<IDamageModifier> GetDamageModifiers();
void OnDamageResolved(in DamageResolutionReport report);
```

### IDamageable

Alvo que aceita ou recusa dano.

Responsabilidades:

- aplicar dano final;
- retornar resultado;
- decidir se bloqueou/parryou/ignorou por estado interno.

Metodo:

```csharp
DamageResult ApplyDamage(in DamageContext context);
```

### IDamageModifier

Modificador de dano data-driven ou runtime.

Responsabilidades:

- alterar instancia;
- alterar contexto por alvo;
- reagir ao resultado.

Fases possiveis:

```text
InstanceBuild
PreTargetResolve
PostTargetResolve
PostRequestResolve
```

Exemplos:

- carta que aumenta `FinalDamagePercent`;
- buff que adiciona `FlatDamage`;
- carta que consome 1 stack ao causar dano efetivo;
- efeito que aumenta dano no terceiro hit;
- efeito que reduz dano apos atingir mais de 3 alvos.

### IDamageListener

Listener local opcional para reacoes do proprio objeto.

Uso recomendado:

- animacao de hit;
- flash visual;
- audio local;
- reacao de escudo;
- atualizar contador local de carga/hit.

Uso nao recomendado:

- HUD global;
- camera shake global;
- analytics;
- musica;
- log central.

Para estes, usar event channels.

## DamageResolver

`DamageResolver` deve ser C# puro e preferencialmente estatico no inicio.

Responsabilidade:

- validar request;
- coletar `IDamageProvider`, `IDamageable`, `IDamageModifier` e `IDamageListener`;
- ordenar alvos;
- calcular dano final por alvo;
- aplicar dano;
- montar report;
- notificar source/target/listeners;
- devolver report para chamador;
- opcionalmente publicar eventos via dispatcher/callback passado na request.

Nao deve:

- conhecer player controller;
- conhecer inimigo especifico;
- tocar HUD diretamente;
- instanciar VFX;
- carregar assets;
- depender de `Update`, `Awake` ou cena.

Assinatura inicial sugerida:

```csharp
public static DamageResolutionReport Resolve(in DamageRequest request, IDamageEventSink eventSink = null)
```

`IDamageEventSink` e opcional para evitar acoplar o resolver puro a ScriptableObjects/event channels.

## Pipeline de resolucao

### 1. Criacao da instancia

O ataque, carta ou hazard cria uma `DamageInstance`.

Dados entram de:

- `DamageProfileSO`;
- `IDamageProvider` do source;
- buffs ativos;
- cartas ativas;
- contexto do golpe.

### 2. Criacao da request

O hit detector cria `DamageRequest` com:

- instancia;
- alvos candidatos;
- ponto de contato;
- direcao;
- prioridade;
- limite de alvos.

### 3. Snapshot ofensivo

O resolver congela os valores ofensivos que devem ser compartilhados por toda a request:

- ataque base;
- buffs globais da instancia;
- tags ofensivas;
- critico, caso seja decidido por instancia.

### 4. Ordenacao de alvos

Modos possiveis:

- ClosestToHitPoint;
- ClosestToSource;
- LowestHealth;
- HighestHealth;
- ExplicitOrder;
- RandomStable;
- None.

Para prototipo, usar:

- `ExplicitOrder`;
- `ClosestToHitPoint`;
- `None`.

### 5. Resolucao por alvo

Para cada alvo selecionado:

1. aplicar modificadores `PreTargetResolve`;
2. calcular dano bruto;
3. calcular dano final;
4. criar `DamageContext`;
5. chamar `IDamageable.ApplyDamage`;
6. aplicar modificadores `PostTargetResolve`;
7. registrar resultado.

### 6. Notificacao do source

Ao fim da request, notificar o source:

```text
IDamageProvider.OnDamageResolved(report)
IDamageListener.OnDamageDealt(report)
```

Isso permite:

- buffs baseados em hits efetivos;
- cartas que acumulam carga;
- contadores de combo;
- efeitos "ao matar";
- efeitos "ao causar dano em N alvos".

### 7. Notificacao dos targets

Cada target pode receber:

```text
IDamageListener.OnDamageReceived(context, result)
```

### 8. Eventos globais

Event channels recomendados:

- DamageRequested;
- DamageApplied;
- DamageRejected;
- DamageResolved;
- TargetKilled.

## Formula data-driven

Base alinhada ao GDD:

```text
RawDamage = ((StrikePercent + StrikeBonusPercent) * (Attack * (1 + AttackBuffPercent)) + FlatDamage)
FinalDamage = RawDamage * (1 + FinalDamagePercent) * CritValue
```

Campos:

- `StrikePercent`: quanto o golpe representa do ataque base.
- `StrikeBonusPercent`: bonus direto sobre o golpe.
- `Attack`: atributo ofensivo do source.
- `AttackBuffPercent`: buffs sobre ataque.
- `FlatDamage`: dano somado apos escala de ataque.
- `FinalDamagePercent`: amplificacao final.
- `CritValue`: multiplicador critico.

Para prototipo, implementar apenas:

```text
FinalDamage = (Attack * StrikePercent + FlatDamage) * (1 + FinalDamagePercent)
```

Mas manter a estrutura com campos para:

- `StrikeBonusPercent`;
- `AttackBuffPercent`;
- `CritValue`.

Assim o sistema comporta a formula completa depois sem migracao conceitual.

## Modificadores e prioridade

`IDamageModifier` deve possuir prioridade.

Ordem sugerida:

1. modificadores da instancia;
2. modificadores do source;
3. modificadores do alvo;
4. modificadores globais da sala/dificuldade, se existirem.

Cada modificador define:

```text
- Priority
- Phase
- AppliesTo(tags/context)
- Modify(...)
- OnResult(...)
```

Exemplo:

```text
Glass Cannon
- Phase: InstanceBuild
- Adds FinalDamagePercent +100%
- Adds tag "incoming-damage-risk" ao source por duracao externa

Hit Charge
- Phase: PostRequestResolve
- Para cada dano aceito, adiciona carga ao source

Multi Target Falloff
- Phase: PreTargetResolve
- Alvos apos o terceiro recebem -30% FinalDamagePercent
```

## Rebounds e danos encadeados

Rebote nao deve entrar no prototipo inicial, mas o modelo deve permitir:

- `DamageResolutionReport` gerar nova `DamageRequest`;
- nova request referenciar `ParentInstanceId`;
- limitar profundidade de cadeia;
- impedir loop infinito por target ja atingido.

Regra futura:

```text
MaxChainDepth
VisitedTargets
ParentInstanceId
```

## Recomendacao de implementacao incremental

### Fase 1

- `IDamageProvider`
- `IDamageModifier`
- `IDamageListener`
- `DamageInstance`
- `DamageRequest`
- `DamageResolutionReport`
- `DamageResolver`
- `IDamageEventSink`

### Fase 2

- `DamageFormula`
- `DamageModifierSO` abstrato
- modificadores runtime por buffs/cartas
- event channels concretos de dano

### Fase 3

- multi-target priority modes
- target filters
- rebound/chain damage
- testes EditMode completos

## Decisao atual

Usar `DamageResolver` puro como orquestrador de transacoes de dano.

Evitar `DamageManager` como MonoBehaviour global nesta etapa.

Evitar Factory/Abstract Factory por enquanto. A criacao de instancias deve ser feita por construtores/builders simples e dados em ScriptableObjects. Factory so passa a fazer sentido se a criacao de dano variar muito por origem, carta ou arma e comecar a duplicar codigo.

