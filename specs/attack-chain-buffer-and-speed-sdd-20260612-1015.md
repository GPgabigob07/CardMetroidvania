# Attack Chain Buffer and Speed SDD - 20260612-1015

## Contexto

Esta especificacao define uma janela de input mais permissiva para o combo basico de ataques e prepara o sistema para buffs e debuffs de velocidade de ataque.

O comportamento desejado e:

- `Reading/WindUp` sempre deve completar e nunca aceita buffer de chain;
- a primeira metade de `Execution` nao aceita buffer;
- da metade de `Execution` ate o fim de `Recovery`, um input de ataque pode ser bufferizado;
- um input bufferizado inicia o proximo ataque somente depois que `Recovery` terminar;
- cada novo ataque executa seu proprio `WindUp` completo;
- buffs e debuffs alteram o tempo real do ataque sem deslocar as janelas normalizadas.

Esta versao existe porque o runtime atual aceita o proximo ataque durante qualquer fase de `Attack1` ou `Attack2`. Isso obriga o jogador a pressionar repetidamente para garantir que algum input seja reconhecido e nao usa `PlayerActionFrame.AllowChain`, apesar de o Animator ja publicar esse dado.

## Historico e fontes

Arquivos usados como fonte:

- `gdd/gdd-canonico-20260526-2331.md`
- `specs/player-movement-controller-sdd-20260604-2107.md`
- `specs/player-animation-state-projection-sdd-20260609-0009.md`
- `Assets/Scrips/Architecture/Player/Runtime/AttackAction.cs`
- `Assets/Scrips/Architecture/Player/Runtime/PlayerController.cs`
- `Assets/Scrips/Architecture/Player/Runtime/PlayerActionAnimationStateBehaviour.cs`
- `Assets/Scrips/Architecture/Player/PlayerActionFrame.cs`
- `Assets/Scrips/Architecture/Player/Data/PlayerAttackDefinitionSO.cs`

## Decisoes canonicas

### Janela de chain

A janela deve usar progresso normalizado da animacao, nao segundos:

| Fase | Intervalo normalizado | Aceita buffer |
|---|---:|---|
| Reading/WindUp | `0.0 .. 1.0` | Nao |
| Execution | `0.0 .. < 0.5` | Nao |
| Execution | `0.5 .. 1.0` | Sim |
| Recovery | `0.0 .. 1.0` | Sim |

O threshold inicial de `Execution` deve ser authorable por ataque, com valor padrao `0.5`.

### Semantica do buffer

- existe somente um slot de buffer para o proximo ataque;
- o primeiro input valido preenche o slot;
- inputs adicionais enquanto o slot estiver preenchido sao ignorados;
- inputs anteriores a metade de `Execution` sao descartados, nao guardados;
- o buffer permanece valido ate o final de `Recovery`;
- o buffer e consumido somente quando a action atual termina;
- sem buffer, o combo termina normalmente;
- `Attack3` nao cria follow-up no combo basico atual;
- interrupcoes como `Hurt`, `Dead` ou cancelamentos explicitos limpam o buffer;
- pausar, desacelerar ou acelerar o ataque nao deve limpar o buffer.

Essa regra garante que uma unica pressao deliberada seja suficiente, sem transformar inputs muito antecipados em chains automaticos.

### Autoridade

O fluxo deve separar timing e decisao:

```text
Animator normalized phase progress
    -> PlayerActionAnimationStateBehaviour
    -> PlayerActionFrame
    -> AttackAction.CanBufferFollowUp
    -> combo runtime accepts or rejects input
    -> buffered follow-up state
    -> Recovery completes
    -> gameplay starts next AttackAction
```

O Animator autoriza a janela temporal, mas nao escolhe o proximo ataque e nao consome input.

Gameplay continua responsavel por:

- validar o input;
- escolher `Attack2` ou `Attack3`;
- armazenar e limpar o buffer;
- iniciar a proxima action;
- resolver interrupcoes e prioridades.

## Progresso normalizado

`PlayerActionFrame` deve expor:

```csharp
PlayerActionPhase Phase;
float NormalizedPhaseTime;
bool CanBufferFollowUp;
bool EndAction;
bool HasAnimatorAuthority;
```

`NormalizedPhaseTime` deve ser limitado a `0..1` para regras de gameplay, mesmo que `AnimatorStateInfo.normalizedTime` ultrapasse `1`.

`PlayerActionAnimationStateBehaviour` deve substituir o booleano estatico `allowChain` por configuracao de janela:

```text
supportsChainBuffer
chainBufferStartNormalized
chainBufferEndNormalized
```

Configuracao inicial:

- WindUp: desabilitado;
- Execution: `0.5 .. 1.0`;
- Recovery: `0.0 .. 1.0`.

O comportamento deve calcular `CanBufferFollowUp` a cada update do estado.

## Runtime do combo

### Responsabilidade

A regra de combo nao deve permanecer crescendo dentro de `PlayerController`.

Criar um runtime pequeno, por exemplo:

```text
PlayerAttackComboRuntime
```

Responsabilidades:

- receber input de ataque;
- consultar se a action atual aceita buffer;
- guardar no maximo um follow-up;
- usar `PlayerAttackSequence` para resolver o proximo estado;
- disponibilizar o follow-up quando a action atual terminar;
- limpar o buffer em interrupcoes.

`PlayerController` apenas encaminha input e inicia a action devolvida pelo runtime.

### Contrato da action

`AttackAction` deve expor a capacidade atual por contrato pequeno, sem casts concretos no controller:

```csharp
public interface IPlayerChainBufferSource
{
    /// <summary>
    /// Gets whether the current action timing allows one follow-up input to be buffered.
    /// </summary>
    bool CanBufferFollowUp { get; }
}
```

Quando o Animator possui autoridade, `AttackAction` usa `PlayerActionFrame.CanBufferFollowUp`.

No fallback por timer, a action calcula a mesma janela a partir de progresso normalizado:

```text
executionProgress = elapsedInExecution / effectiveExecutionDuration
```

Assim testes, cenas sem Animator e falhas de binding preservam a mesma regra.

## Velocidade de ataque

### Semantica

Velocidade de ataque e um multiplicador positivo:

- `1.0`: velocidade base;
- `1.25`: 25% mais rapido;
- `0.8`: 20% mais lento.

Duracoes efetivas:

```text
effectiveDuration = baseDuration / resolvedAttackSpeed
```

O mesmo valor deve controlar:

- playback do Animator de ataque;
- timers de fallback;
- quaisquer marcadores temporais derivados de segundos.

Janelas normalizadas nao sao multiplicadas. A metade de `Execution` continua sendo `0.5`.

### Resolucao de modificadores

Preparar um resolvedor independente de Animator e `AttackAction`:

```text
base speed
    + additive percentage modifiers
    -> multiplicative modifiers
    -> clamp authorable
    -> resolved attack speed
```

Formula conceitual:

```text
resolved = clamp(
    baseSpeed * (1 + sum(additivePercent)) * product(multipliers),
    minimumSpeed,
    maximumSpeed)
```

Exemplos:

- buff de `+20%`: additive percent `+0.20`;
- debuff de `-30%`: additive percent `-0.30`;
- efeito multiplicativo de lentidao: multiplier `0.75`;
- efeito multiplicativo de haste: multiplier `1.5`.

Valores zero ou negativos nunca podem chegar ao Animator ou a divisao de duracao.

Limites iniciais sugeridos, ainda authorable:

- minimo: `0.25`;
- maximo: `3.0`.

### Snapshot por action

A velocidade resolvida deve ser capturada quando cada `AttackAction` entra.

Consequencias:

- um ataque nao muda de velocidade no meio do clip por entrada ou expiracao de buff;
- o proximo golpe do combo captura os modificadores vigentes naquele momento;
- buffs e debuffs aplicados durante Attack1 podem afetar Attack2;
- comportamento, VFX, hitbox e locomocao permanecem sincronizados durante uma action.

Efeitos globais de tempo, pausa ou slow motion pertencem a outro sistema e podem continuar alterando o tempo imediatamente.

Uma futura mecanica que exija mudanca de attack speed no meio do golpe deve declarar essa capacidade explicitamente, em vez de mudar o comportamento padrao.

## Dados

Expandir `PlayerAttackDefinitionSO` ou introduzir uma definicao por golpe com:

```text
baseAttackSpeed
minimumAttackSpeed
maximumAttackSpeed
executionChainBufferStartNormalized = 0.5
executionChainBufferEndNormalized = 1.0
recoveryChainBufferStartNormalized = 0.0
recoveryChainBufferEndNormalized = 1.0
```

Quando Attack1, Attack2 e Attack3 ganharem frame data diferente, cada golpe deve possuir sua propria definicao. Nao espalhar thresholds por `StateMachineBehaviour`, controller e codigo.

O Animator pode serializar os mesmos thresholds para autoria visual inicialmente, mas a definicao de ataque deve ser a fonte canonica. Uma validacao de editor deve avisar quando configuracao do Animator divergir da definicao.

## Integracao com animacao

`PlayerAttackAnimationDriver` deve receber a velocidade capturada no snapshot/comando e:

- aplicar o multiplicador somente durante estados de ataque;
- restaurar playback `1.0` em `Idling`;
- nao calcular buffs/debuffs;
- nao decidir janelas de chain.

O snapshot de animacao pode crescer com:

```text
AttackPlaybackSpeed
```

Esse float e metrica de apresentacao. Ele nao deve provocar eventos estruturais a cada frame. Como o valor fica congelado durante a action, a transicao de entrada do ataque e suficiente para aplica-lo.

## Interrupcoes e casos extremos

### Hurt e Dead

- limpam buffer imediatamente;
- substituem a action conforme prioridade existente;
- nao iniciam follow-up depois da interrupcao.

### Dash e outras actions

- nao preenchem o buffer de ataque;
- politica de cancelamento deve ser separada da politica de chain;
- permitir dash cancel no futuro nao deve reutilizar `CanBufferFollowUp`.

### Hit stop

- nao altera progresso normalizado;
- nao consome buffer;
- congela Animator e gameplay de maneira coordenada;
- nao deve ser implementado como attack speed zero.

### FPS baixo

A janela deve usar comparacao `>= 0.5`, nao igualdade.

Se um frame saltar de `0.49` para `0.57`, o input recebido nesse frame deve ser aceito porque o estado atual ja esta dentro da janela.

### Debuff extremo

Debuffs abaixo do minimo sao limitados pelo clamp. A janela fica mais longa em segundos, mas preserva a mesma proporcao da animacao.

### Buff extremo

Buffs acima do maximo sao limitados para preservar legibilidade, input sampling e VFX. O maximo final deve ser validado em playtest.

## Testes

### EditMode

- WindUp nunca aceita buffer;
- Execution em `0.49` rejeita;
- Execution em `0.50` aceita;
- Execution em `1.0` aceita;
- Recovery em `0.0` aceita;
- Recovery em `1.0` aceita antes de completar;
- input anterior a janela nao fica armazenado;
- um input valido ocupa o unico slot;
- inputs adicionais nao pulam diretamente para Attack3;
- buffer e consumido somente apos Recovery;
- Hurt/Dead limpam o buffer;
- Attack3 nao produz follow-up basico;
- speed `2.0` reduz duracoes fallback pela metade;
- speed `0.5` dobra duracoes fallback;
- modificadores additive e multiplicative respeitam a ordem;
- speed e limitado por minimo e maximo;
- speed capturado nao muda no meio da action;
- proxima action captura modificadores atualizados.

### PlayMode

- uma unica pressao entre 50% e 100% de Execution executa o proximo golpe;
- uma unica pressao durante Recovery executa o proximo golpe;
- pressionar antes de 50% nao cria chain;
- o proximo WindUp sempre toca por completo;
- Attack1, Attack2 e Attack3 funcionam em speed `0.5`, `1.0`, `1.5` e `2.0`;
- VFX, hitbox e movimento permanecem sincronizados;
- combo nao sobrevive a Hurt ou Dead;
- nao ha necessidade de spam para manter o combo.

## Etapas de implementacao

1. Adicionar progresso normalizado e `CanBufferFollowUp` a `PlayerActionFrame`.
2. Tornar `PlayerActionAnimationStateBehaviour` capaz de calcular janelas normalizadas.
3. Configurar Execution e Recovery dos tres ataques com as janelas canonicas.
4. Adicionar `IPlayerChainBufferSource` a `AttackAction`.
5. Extrair o buffer e progressao de combo de `PlayerController` para `PlayerAttackComboRuntime`.
6. Fazer o runtime aceitar input apenas quando `CanBufferFollowUp` for verdadeiro.
7. Limpar buffer em interrupcoes e fim invalido de combo.
8. Introduzir resolvedor de attack speed com modifiers e clamp.
9. Capturar resolved speed na entrada de cada `AttackAction`.
10. Aplicar speed ao Animator e aos timers fallback.
11. Adicionar testes EditMode.
12. Adicionar testes PlayMode e validar os clamps por playtest.

## Fora de escopo desta versao

- cancelamento de ataque para dash, parry ou carta;
- hit confirm como requisito para chain;
- diferentes sequencias por arma;
- branching combo;
- consumo de stamina;
- hit stop e global time scale;
- alteracao live de attack speed no meio de uma action;
- frame data final dos tres ataques.
