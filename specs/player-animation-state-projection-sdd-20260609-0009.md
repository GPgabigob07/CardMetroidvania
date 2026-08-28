# Player Animation State Projection SDD - 20260609-0009

## Contexto

Esta especificacao define uma entrada unica para animacao do jogador sem fundir as state machines de locomocao e acao.

Fontes consultadas:

- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/player-movement-controller-sdd-20260604-2107.md`
- `specs/event-architecture-layout-20260526-0005.md`
- `specs/code-conventions-20260526-0014.md`
- implementacao atual em `Assets/Scrips/Architecture/Player`

O prototipo ja separa corretamente dois eixos de gameplay:

- locomocao descreve a relacao do corpo com movimento e ambiente;
- acao descreve ataques, dash, hurt, morte e outras execucoes temporarias.

Esses eixos nao devem ser transformados em uma unica state machine hierarquica. Uma acao pode acontecer em mais de uma locomocao, e cada sistema deve continuar dono das proprias regras.

Animacao, entretanto, precisa de uma decisao exata e de uma unica autoridade. A solucao desta versao e projetar o estado combinado de gameplay em um estado de apresentacao:

```text
Locomotion State ─┐
Action State ─────┼─> Snapshot Publisher
Action Phase ─────┤          |
Motion Facts ─────┘          v
                      PlayerAnimationSnapshot
                               |
                               v
                     PlayerAnimationMapper
                               |
                               v
                      PlayerAnimationCommand
                               |
                               v
                     PlayerAnimationDriver
                               |
                               v
                            Animator
```

## Objetivos

- oferecer uma unica entrada para escolher animacoes do jogador;
- preservar locomocao e acao como state machines independentes;
- evitar flags booleanas persistentes para cada possibilidade de animacao;
- evitar um enum com o produto cartesiano de todas as combinacoes;
- suportar estados estaveis e transicoes como `WalkBegin` e pouso;
- permitir regras de prioridade explicitas e testaveis;
- impedir que clip names, Animator hashes ou event channels vazem para gameplay;
- permitir adicionar novos eixos somente quando houver necessidade concreta.

## Nao objetivos

- substituir `PlayerLocomotionController` ou `PlayerActionRunner`;
- transformar animacao em autoridade sobre regras de locomocao;
- descrever nesta versao todas as animacoes futuras;
- criar automaticamente o Animator Controller;
- resolver eventos de timing internos de ataques, como hitbox e Card Time;
- gerar todas as combinacoes possiveis de locomocao e acao.

## Decisoes principais

1. Animacao e uma projecao derivada de gameplay, nao uma terceira fonte de verdade.
2. O snapshot guarda dimensoes ortogonais, nao nomes combinados como `GroundedAttack1`.
3. O publisher e o unico componente que constroi e publica snapshots.
4. O mapper e puro, ordenado e nao conhece `Animator`.
5. O driver e o unico componente que envia comandos ao `Animator`.
6. Mudancas sao publicadas como transicoes contendo snapshot anterior e atual.
7. Clips transitorios sao selecionados por arestas entre snapshots, nao por flags mantidas no `PlayerController`.
8. Regras de acao possuem prioridade sobre regras de locomocao, salvo excecao documentada.
9. Combinacoes sem regra usam fallback deterministico e geram diagnostico em desenvolvimento.

## Separacao de responsabilidades

### Gameplay state machines

Continuam responsaveis por regras concretas:

- `PlayerLocomotionController`: grounded, airborne, wall slide e locked;
- `PlayerActionRunner`: none, dash, ataques, hurt, dead e futuras actions;
- actions: fase atual, conclusao, cancelamento e overrides de locomocao;
- sensores e motor: contato com chao e velocidade efetiva.

Elas nao devem:

- referenciar clips;
- referenciar estados do Animator;
- publicar um canal especifico como `Player_WalkBegin_Event`;
- decidir precedencia visual entre locomocao e acao.

### Animation projection

Responsavel somente por traduzir fatos ja resolvidos em apresentacao:

- publisher coleta os fatos;
- snapshot os representa de forma imutavel;
- mapper escolhe um comando;
- driver executa o comando.

## PlayerAnimationSnapshot

`PlayerAnimationSnapshot` representa os fatos relevantes para animacao em um instante logico. Ele deve ser um tipo de valor imutavel e implementar igualdade por valor.

Primeiro contrato recomendado:

```csharp
public readonly struct PlayerAnimationSnapshot : IEquatable<PlayerAnimationSnapshot>
{
    public PlayerAnimationSnapshot(
        PlayerLocomotionState locomotion,
        PlayerActionState action,
        PlayerActionPhase actionPhase,
        PlayerHorizontalMotion horizontalMotion,
        PlayerVerticalMotion verticalMotion,
        PlayerCardTimeState cardTime,
        int facingDirection,
        float verticalSpeed)
    {
        Locomotion = locomotion;
        Action = action;
        ActionPhase = actionPhase;
        HorizontalMotion = horizontalMotion;
        VerticalMotion = verticalMotion;
        CardTime = cardTime;
        FacingDirection = facingDirection;
        VerticalSpeed = verticalSpeed;
    }

    public PlayerLocomotionState Locomotion { get; }
    public PlayerActionState Action { get; }
    public PlayerActionPhase ActionPhase { get; }
    public PlayerHorizontalMotion HorizontalMotion { get; }
    public PlayerVerticalMotion VerticalMotion { get; }
    public PlayerCardTimeState CardTime { get; }
    public int FacingDirection { get; }
    public float VerticalSpeed { get; }
}
```

Enums derivados iniciais:

```csharp
public enum PlayerHorizontalMotion
{
    Idle = 0,
    Moving = 10
}

public enum PlayerVerticalMotion
{
    Stable = 0,
    Rising = 10,
    Falling = 20
}
```

### Regras do snapshot

- `Locomotion` vem de `PlayerLocomotionController.CurrentStateId`.
- `Action` vem de `PlayerActionRunner.CurrentState`.
- `ActionPhase` vem da action ativa por um contrato de leitura, quando ela possui fase; sem action, usa valor neutro definido pelo contrato.
- `HorizontalMotion` e derivado da velocidade final aplicada, com threshold configuravel.
- `VerticalMotion` e derivado da locomocao e velocidade final.
- `CardTime` vem do estado efetivo da action, nao diretamente do Animator.
- `FacingDirection` e normalizado para `-1` ou `1`.
- `VerticalSpeed` preserva informacao quantitativa necessaria para regras como hard landing.

Floats quantitativos nao devem participar diretamente da igualdade usada para detectar mudanca estrutural. A implementacao pode:

1. separar identidade estrutural de metricas; ou
2. implementar igualdade ignorando `VerticalSpeed`.

A opcao recomendada e tratar `VerticalSpeed` como metrica associada ao snapshot, mas ignorada na igualdade estrutural. Isso evita publicar um novo estado a cada passo de fisica durante queda.

Mesmo quando a identidade estrutural for igual, o publisher deve substituir `Current` pelo snapshot mais recente. Ele apenas suprime o evento `Changed`. Assim, a proxima transicao recebe metricas atualizadas, como a velocidade imediatamente anterior ao pouso.

### Estado detalhado de actions

`IPlayerAction` expoe hoje apenas `State`. O snapshot nao deve fazer casts para classes concretas como `AttackAction`.

Adicionar um contrato pequeno quando fase e Card Time entrarem na integracao:

```csharp
public interface IPlayerActionAnimationSource
{
    /// <summary>
    /// Gets the current action phase exposed to presentation.
    /// </summary>
    PlayerActionPhase AnimationPhase { get; }

    /// <summary>
    /// Gets the current Card Time presentation state.
    /// </summary>
    PlayerCardTimeState AnimationCardTime { get; }
}
```

`PlayerActionRunner` pode expor esses valores normalizados a partir da action atual. Isso preserva o publisher contra conhecimento de tipos concretos.

Enquanto `PlayerActionFrame` ainda receber autoridade do Animator, o snapshot deve ler o estado efetivo normalizado pelo runner/action. A direcao alvo, contudo, e que o snapshot reflita gameplay e que callbacks de animacao fornecam apenas marcadores de timing, evitando um ciclo de autoridade.

### Crescimento do snapshot

Adicionar uma dimensao apenas quando ela alterar concretamente a escolha de animacao.

Possiveis dimensoes futuras:

- wall contact;
- weapon stance;
- equipped form;
- status visual dominante;
- traversal mode;
- aiming direction.

Nao adicionar antecipadamente flags como `IsJumping`, `IsWalking` e `IsAttacking` quando esses fatos ja podem ser derivados dos campos existentes.

## PlayerAnimationTransition

O mapper precisa conhecer arestas, nao somente o estado atual:

```csharp
public readonly struct PlayerAnimationTransition
{
    public PlayerAnimationTransition(
        PlayerAnimationSnapshot previous,
        PlayerAnimationSnapshot current,
        bool hasPrevious)
    {
        Previous = previous;
        Current = current;
        HasPrevious = hasPrevious;
    }

    public PlayerAnimationSnapshot Previous { get; }
    public PlayerAnimationSnapshot Current { get; }
    public bool HasPrevious { get; }
}
```

Exemplos:

- `Grounded + Idle -> Grounded + Moving` seleciona `WalkBegin`;
- `Grounded -> Airborne + Rising` seleciona `JumpUp`;
- `Airborne + Falling -> Grounded` seleciona pouso;
- `Action None -> Attack1` seleciona startup de ataque;
- `Attack1 -> None` devolve a decisao para locomocao.

`HasPrevious` permite resolver o primeiro snapshot sem inventar um estado anterior.

## PlayerAnimationSnapshotPublisher

O publisher e o adaptador entre runtime de gameplay e a projecao de animacao.

Contrato recomendado:

```csharp
public interface IPlayerAnimationSnapshotSource
{
    /// <summary>
    /// Builds the current animation snapshot from resolved gameplay state.
    /// </summary>
    PlayerAnimationSnapshot Capture();
}

public sealed class PlayerAnimationSnapshotPublisher
{
    public event Action<PlayerAnimationTransition> Changed;

    public PlayerAnimationSnapshot Current { get; private set; }
    public bool HasCurrent { get; private set; }

    public void Publish(PlayerAnimationSnapshot snapshot);
}
```

### Responsabilidades

- receber ou capturar um snapshot depois que gameplay estiver resolvido;
- comparar identidade estrutural com o snapshot atual;
- publicar uma transicao somente quando a identidade mudar;
- atualizar metricas do snapshot atual mesmo quando nao publicar uma transicao;
- manter `Current` para consumidores tardios e debug;
- nao escolher animacao;
- nao conhecer clip names ou `Animator`.

### Momento de captura

O snapshot deve usar a decisao final do passo, incluindo overrides da action.

Ordem recomendada em `FixedUpdate`:

```text
1. Atualizar sensores.
2. Resolver transicoes de locomocao.
3. Atualizar action.
4. Construir LocomotionFrame.
5. Aplicar override da action.
6. Capturar PlayerAnimationSnapshot usando estado e frame finais.
7. Publicar a transicao estrutural, se houver.
8. Aplicar o frame no motor.
```

Para actions iniciadas ou encerradas em `Update`, o publisher pode receber uma captura adicional no fim de `Update`. O metodo `Publish` elimina duplicatas estruturais.

Uma implementacao inicial mais simples pode publicar somente em `FixedUpdate`, aceitando latencia visual de ate um passo de fisica. Se essa latencia for perceptivel, adicionar a captura de `Update` sem mudar o contrato.

### Origem dos dados

O source pode ler:

- `PlayerContext`;
- `PlayerLocomotionController`;
- `PlayerActionRunner`;
- o `LocomotionFrame` final;
- sensores somente quando o fato ainda nao estiver representado nos estados resolvidos.

O publisher nao deve ser armazenado dentro de `PlayerContext`. Ele e um adaptador de apresentacao e deve depender do contexto, nao o contrario.

### Eventos do publisher

Usar inicialmente um evento C# tipado:

```csharp
event Action<PlayerAnimationTransition> Changed;
```

Nao criar um `VoidEventChannelSO` por animacao. Caso consumidores externos precisem observar transicoes no futuro, criar um canal tipado com payload `PlayerAnimationTransition` ou `PlayerAnimationCommand`.

## PlayerAnimationMapper

O mapper transforma uma transicao em um unico comando de animacao.

Contrato recomendado:

```csharp
public interface IPlayerAnimationMapper
{
    /// <summary>
    /// Resolves one presentation command from a gameplay snapshot transition.
    /// </summary>
    PlayerAnimationCommand Map(in PlayerAnimationTransition transition);
}
```

O mapper deve ser:

- puro;
- deterministico;
- independente de `MonoBehaviour`;
- independente do `Animator`;
- testavel em EditMode;
- explicito sobre prioridade.

### Regras esparsas, nao produto cartesiano

O snapshot possui varias dimensoes, mas o mapper descreve apenas combinacoes relevantes.

Exemplo de precedencia:

```text
1. Dead
2. Hurt
3. Finisher / CardChain
4. Attack1 / Attack2 / Attack3 por fase
5. Dash
6. WallSlide
7. Airborne rising
8. Airborne falling
9. Grounded movement transition
10. Grounded moving
11. Grounded idle
12. Fallback
```

Assim, `Grounded + Attack1` e `Airborne + Attack1` podem compartilhar regra enquanto a arte for igual. Uma regra mais especifica e adicionada somente se o ataque aereo precisar de clip diferente.

### Regras de transicao iniciais

| Condicao | Comando |
|---|---|
| primeiro snapshot grounded e idle | `Idle` |
| grounded idle para grounded moving | `WalkBegin` |
| grounded moving permanece moving | nenhum novo comando |
| qualquer locomocao para airborne rising sem action dominante | `JumpUp` |
| airborne rising para airborne falling sem action dominante | `Fall` |
| airborne falling para grounded, impacto abaixo do threshold | `GroundedFall` |
| airborne falling para grounded, impacto acima do threshold | `HardLanding` |
| action muda para `Attack1` | estado de ataque correspondente a fase |
| fase de ataque muda | estado correspondente a nova fase, se houver clip separado |
| action termina | resolver imediatamente a locomocao atual |
| action muda para `Hurt` | `Hurt` |
| action muda para `Dead` | `Dead` |

`WalkBegin` e `GroundedFall` sao entradas transitorias. O termino desses clips deve encaminhar para o estado estavel apropriado, conforme definido pelo comando ou pelo driver.

### Hard landing

Para detectar impacto, o snapshot anterior deve preservar a ultima velocidade vertical airborne antes do contato.

Regra conceitual:

```csharp
var landed = previous.Locomotion == PlayerLocomotionState.Airborne
    && current.Locomotion == PlayerLocomotionState.Grounded;

var impactSpeed = Mathf.Abs(previous.VerticalSpeed);
```

O threshold pertence a configuracao do mapper ou a uma definicao de animacao, nao ao estado de locomocao.

## PlayerAnimationCommand

O output do mapper descreve intencao de apresentacao, sem executar Unity APIs.

Contrato inicial:

```csharp
public readonly struct PlayerAnimationCommand
{
    public PlayerAnimationCommand(
        PlayerAnimationState state,
        PlayerAnimationState fallbackState,
        bool hasFallback,
        float crossFadeDuration,
        bool restart)
    {
        State = state;
        FallbackState = fallbackState;
        HasFallback = hasFallback;
        CrossFadeDuration = crossFadeDuration;
        Restart = restart;
    }

    public PlayerAnimationState State { get; }
    public PlayerAnimationState FallbackState { get; }
    public bool HasFallback { get; }
    public float CrossFadeDuration { get; }
    public bool Restart { get; }
}
```

`PlayerAnimationState` e um enum de estados visuais reais, nao de combinacoes de gameplay:

```csharp
public enum PlayerAnimationState
{
    Idle = 0,
    WalkBegin = 10,
    WalkLoop = 20,
    JumpUp = 30,
    Fall = 40,
    GroundedFall = 50,
    HardLanding = 60,
    Dash = 70,
    Attack1Reading = 100,
    Attack1Execution = 110,
    Attack1Recovery = 120,
    Hurt = 200,
    Dead = 210
}
```

Estados devem ser adicionados conforme clips e regras reais surgirem. Nao preencher antecipadamente todas as combinacoes.

### Transitorios e fallback

Exemplos:

- `WalkBegin` possui fallback `WalkLoop`;
- `GroundedFall` possui fallback `Idle` ou `WalkLoop`, resolvido a partir do snapshot atual;
- `HardLanding` possui fallback grounded apropriado;
- estados estaveis usam `HasFallback = false`.

O mapper escolhe o fallback no momento do comando. O driver nao reinterpreta gameplay para decidir o proximo estado.

## PlayerAnimationDriver

Embora nao seja um dos tres elementos centrais, o driver completa o limite arquitetural.

Responsabilidades:

- assinar `PlayerAnimationSnapshotPublisher.Changed`;
- chamar o mapper;
- ignorar comando equivalente quando `Restart` for falso;
- converter `PlayerAnimationState` em hash/nome do Animator;
- executar `Play` ou `CrossFade`;
- completar transitorios usando o fallback presente no comando;
- ser o unico ponto de escrita no Animator do jogador.

O driver nao deve:

- ler input;
- consultar locomocao ou action runner;
- aplicar regras de prioridade;
- criar snapshots;
- publicar eventos de gameplay.

O mapeamento de `PlayerAnimationState` para Animator state pode ser:

1. codigo com hashes estaticos; ou
2. `ScriptableObject` de bindings authorable.

Para o prototipo, hashes centralizados sao suficientes. Migrar para asset somente quando autoria sem codigo trouxer valor real.

## One-way data flow

Fluxo obrigatorio:

```text
Gameplay truth
    -> final movement/action state
    -> animation snapshot
    -> transition
    -> mapped command
    -> Animator
```

Feedback permitido no sentido inverso:

```text
Animator timing event
    -> PlayerAnimationEventRelay
    -> active gameplay action
```

Esse feedback deve conter somente marcadores de timing previamente autorizados, como:

- `EnableHitbox`;
- `DisableHitbox`;
- `OpenComboWindow`;
- `EndAction`.

O Animator nao pode alterar locomocao ou escolher a action ativa por conta propria.

## Localizacao proposta

```text
Assets/Scrips/Architecture/Animation/
  PlayerAnimationState.cs
  PlayerAnimationSnapshot.cs
  PlayerAnimationTransition.cs
  PlayerAnimationCommand.cs
  IPlayerAnimationSnapshotSource.cs
  IPlayerAnimationMapper.cs
  PlayerAnimationMapper.cs
  PlayerAnimationSnapshotPublisher.cs
  PlayerAnimationDriver.cs
```

Se o mapper usar configuracao authorable:

```text
Assets/Scrips/Architecture/Animation/Data/
  PlayerAnimationMapSO.cs
```

Cada `MonoBehaviour` ou `ScriptableObject` concreto deve permanecer em arquivo homonimo.

## Integracao com PlayerController

`PlayerController` continua orquestrando gameplay, mas nao interpreta estados visuais.

Integracao alvo:

```csharp
var frame = Locomotion.BuildFrame(context: Context, fixedDeltaTime: Time.fixedDeltaTime);
ActionRunner.CurrentLocomotionOverride?.ModifyLocomotionFrame(
    frame: ref frame,
    context: Context,
    fixedDeltaTime: Time.fixedDeltaTime);

animationSnapshotPublisher.Publish(
    snapshot: animationSnapshotSource.Capture(
        context: Context,
        finalFrame: frame));

motor.ApplyFrame(frame: frame);
```

O exemplo e conceitual. O source pode ser incorporado ao publisher se isso mantiver a implementacao inicial menor, desde que captura e publicacao permaneçam separadas do mapper.

## Migracao da implementacao atual

Ao implementar esta especificacao:

1. remover `jumpUpEvent`, `walkBeginEvent` e `wasWalking` de `PlayerController`;
2. remover o evento especifico `JumpStarted` se ele existir somente para animacao;
3. manter eventos de gameplay somente quando houver consumidores de gameplay reais;
4. substituir `AnimationClipDispatcher` pelo publisher + mapper + driver;
5. remover canais void por clip da cena quando nao tiverem outro consumidor;
6. preservar os assets historicos ate confirmar que nao possuem referencias;
7. ligar o Animator exclusivamente ao `PlayerAnimationDriver`.

Nao remover ou sobrescrever assets durante a primeira etapa da migracao. Primeiro provar o novo fluxo em cena; depois fazer limpeza explicita.

## Estrategia de testes

### Snapshot publisher

- primeiro snapshot publica transicao com `HasPrevious = false`;
- snapshot estrutural igual nao publica novamente;
- snapshot estrutural igual atualiza metricas em `Current`;
- mudanca de locomocao publica uma vez;
- mudanca de action publica uma vez;
- alteracao somente de velocidade quantitativa nao publica continuamente;
- current snapshot permanece disponivel depois da publicacao.

### Mapper

- grounded idle resolve `Idle`;
- idle para moving resolve `WalkBegin` com fallback `WalkLoop`;
- moving para moving nao reinicia `WalkBegin`;
- grounded para airborne rising resolve `JumpUp`;
- rising para falling resolve `Fall`;
- ataque domina walk;
- ataque domina jump quando a regra do ataque for compartilhada;
- fim do ataque resolve locomocao atual;
- hurt domina ataque e locomocao;
- dead domina todos os estados;
- landing usa velocidade anterior para escolher normal ou hard landing;
- landing recebe a metrica mais recente mesmo sem eventos durante a queda;
- combinacao sem regra retorna fallback deterministico.

### Driver

- comando repetido nao reinicia estado estavel;
- `Restart = true` reinicia quando solicitado;
- transitorio conclui no fallback fornecido;
- driver nao consulta `PlayerContext`;
- binding ausente gera diagnostico claro sem quebrar gameplay.

### Validacao manual

- iniciar e parar caminhada repetidamente;
- inverter direcao sem reiniciar `WalkBegin`, salvo decisao visual futura;
- pular parado e correndo;
- usar coyote jump;
- cair sem pular;
- pousar em velocidades baixa e alta;
- atacar parado, andando, subindo e caindo;
- terminar ataque no ar e no chao;
- receber hurt durante locomocao e durante ataque;
- verificar que somente um componente escreve no Animator.

## Fases de implementacao

### Fase 1 - locomocao baseline

- snapshot com locomocao, movimento horizontal, movimento vertical e velocidade vertical;
- publisher com transicao anterior/atual;
- mapper para idle, walk begin, walk loop, jump, fall e landing;
- driver unico para Animator;
- testes puros do publisher e mapper.

### Fase 2 - actions

- incluir action e phase no snapshot;
- regras de dash, attack, hurt e dead;
- precedencia e retorno para locomocao;
- testes de combinacoes relevantes.

### Fase 3 - autoria e diagnostico

- avaliar bindings em `ScriptableObject`;
- inspector/debug overlay do snapshot atual e comando resolvido;
- validacao de estados sem binding;
- canal tipado somente se consumidores externos precisarem.

## Riscos e limites

### Snapshot grande demais

Risco: transformar o snapshot em uma copia de todo `PlayerContext`.

Mitigacao: cada campo deve justificar uma escolha visual concreta.

### Mapper procedural extenso

Risco: uma cadeia grande de `if` ficar dificil de manter.

Mitigacao: manter regras ordenadas por dominio e extrair pequenos resolvers, por exemplo `ResolveAction`, `ResolveAirborne` e `ResolveGrounded`. Migrar para regras data-driven somente quando a quantidade real justificar.

### Animator como segunda state machine concorrente

Risco: transicoes internas do Animator contradizerem o comando externo.

Mitigacao: Animator simples, dirigido pelo driver, com transicoes internas apenas para completar clips transitorios quando necessario. A selecao semantica permanece no mapper.

### Ciclo action-animation

Risco: action escolhe animacao, animacao define fase da action e a fase escolhe outra animacao, criando ciclo instavel.

Mitigacao: separar selecao visual de marcadores de timing. Gameplay escolhe a familia visual; animation events somente informam marcos autorizados da execucao.

## Criterios de aceite

- existe exatamente um publisher de snapshot para o jogador;
- existe exatamente um driver escrevendo no Animator;
- `PlayerController` nao possui flags por animacao;
- locomotion states e actions nao referenciam clips ou canais por clip;
- snapshots iguais nao reiniciam animacoes;
- transicoes usam previous/current para escolher clips de entrada;
- mapper cobre locomocao baseline e possui fallback;
- regras de prioridade entre action e locomocao estao testadas;
- adicionar uma nova action nao exige criar valores para todas as locomocoes;
- o fluxo permanece unidirecional de gameplay para apresentacao.

## Decisoes em aberto

- se `ActionPhase` neutra deve ganhar valor `None` em vez de reutilizar `Reading`;
- se ataques terao clips separados por fase no primeiro slice;
- se o fallback de transitorios sera controlado pelo driver ou por callback de fim de clip;
- se bindings de estado ficarao em codigo ou `ScriptableObject`;
- threshold exato de `HorizontalMotion.Moving`;
- threshold exato de hard landing;
- se mudanca de facing durante movimento reinicia alguma animacao;
- quando um ataque aereo deve divergir visualmente do mesmo ataque no chao.
