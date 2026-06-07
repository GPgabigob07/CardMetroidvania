# Player Movement Controller SDD - 20260604-2107

## Contexto

Esta especificacao registra a direcao inicial para o controller do jogador antes da implementacao. Ela complementa:

- `gdd/gdd-canonico-20260526-2331.md`
- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/code-conventions-20260526-0014.md`
- `specs/unity-script-asset-file-layout-20260526-0000.md`

O objetivo e iniciar o movimento do personagem sem superengenharia, mas reconhecendo desde o inicio que o jogo tera sobreposicao frequente entre locomocao e acoes: ataque no chao, ataque aereo, dash, hitstun, Card Time e futuros cancelamentos.

Esta versao consolida a decisao de usar controle direto de velocidade em `Rigidbody2D`, com um pipeline de comandos de movimento que permite acoes temporarias modificarem ou sobrescreverem a locomocao base.

## Objetivo

Criar uma base de controller 2D que seja:

- precisa e previsivel para um metroidvania de combate corpo a corpo;
- facil de tunar por `ScriptableObject`;
- capaz de suportar ataque aereo e Card Time sem reescrever a locomocao;
- simples o bastante para implementar o primeiro prototipo;
- testavel onde nao depender diretamente de fisica/cena Unity.

## Decisoes de alto nivel

1. Usar controle direto de velocidade, nao `AddForce`, para o movimento principal.
2. Tratar `Rigidbody2D` como corpo de colisao e transporte fisico, nao como simulacao livre de personagem.
3. Separar o estado do personagem em dois eixos principais:
   - locomocao: onde e como o corpo esta se movendo;
   - acao: o que o personagem esta executando naquele momento.
4. Representar estados iniciais como enums, evitando uma hierarquia pesada de classes no primeiro slice.
5. Usar acoes temporarias como objetos runtime autocontidos para dash, ataques, hurt e futuros efeitos de carta.
6. Permitir que a acao atual exponha um override de locomocao opcional.
7. Evitar behavior tree para o jogador nesta fase. Behavior tree pode ser considerada para inimigos futuramente.

## Modelo de estados

### Locomocao

Estado macro do corpo. Deve responder a sensores, colisao, input de movimento e regras de gravidade.

```csharp
public enum PlayerLocomotionState
{
    Grounded,
    Airborne,
    WallSlide,
    Locked
}
```

Escopo inicial recomendado:

- `Grounded`: movimento horizontal, atrito, salto, transicao para queda.
- `Airborne`: controle aereo, gravidade, queda, pouso.
- `WallSlide`: reservado para implementacao futura.
- `Locked`: reservado para casos em que a locomocao base deve parar de tomar decisoes proprias.

`Idle` nao precisa ser estado separado no primeiro momento. Ele pode ser derivado de `Grounded` com input horizontal zero.

### Acao

Estado especifico da execucao atual. Pode modificar locomocao, abrir janelas, tocar animacao, habilitar hitbox ou bloquear inputs.

```csharp
public enum PlayerActionState
{
    None,
    Dash,
    Attack1,
    Attack2,
    Attack3,
    CardChain,
    Finisher,
    Hurt,
    Dead
}
```

`Jump` nao entra como acao longa inicialmente. O salto deve ser uma intencao consumida pela locomocao/motor, exceto se futuramente houver jump startup, jump squat, cancelamento especial ou janela de animacao que justifique uma action propria.

## Pipeline de movimento

O movimento deve ser resolvido em uma estrutura intermediaria, antes de aplicar velocidade no `Rigidbody2D`.

Fluxo alvo por `FixedUpdate`:

```text
1. Atualizar sensores de chao/parede.
2. Resolver estado de locomocao.
3. Locomocao monta um LocomotionFrame base.
4. Acao atual modifica ou sobrescreve o LocomotionFrame, se implementar override.
5. Motor aplica o LocomotionFrame final no Rigidbody2D.
```

Regra central:

```text
Locomocao calcula o comportamento padrao do corpo.
Acao temporaria pode modificar, limitar ou sobrescrever esse comportamento.
Motor aplica o resultado final.
```

## LocomotionFrame

`LocomotionFrame` representa a decisao final de movimento para um passo fisico.

Campos sugeridos:

```csharp
public struct LocomotionFrame
{
    public Vector2 Velocity;
    public float GravityScale;
    public bool AllowHorizontalInput;
    public bool AllowGravity;
    public bool LockFacing;
}
```

Possiveis campos futuros, caso a implementacao precise:

- `bool PreserveVerticalVelocity`
- `bool IgnoreGroundSnap`
- `float HorizontalControlMultiplier`
- `float MaxFallSpeedOverride`
- `Vector2 ExternalImpulse`

Nao adicionar esses campos antes de necessidade concreta.

## Contratos

### IPlayerAction

Acao temporaria executada pelo `PlayerActionRunner`.

```csharp
public interface IPlayerAction
{
    /// <summary>
    /// Gets the action state represented by this runtime action.
    /// </summary>
    PlayerActionState State { get; }

    /// <summary>
    /// Gets whether the action finished and should be cleared by the runner.
    /// </summary>
    bool IsComplete { get; }

    /// <summary>
    /// Called once when the action becomes active.
    /// </summary>
    void Enter(PlayerContext context);

    /// <summary>
    /// Updates input, timers and non-physics action rules.
    /// </summary>
    void Tick(PlayerContext context, float deltaTime);

    /// <summary>
    /// Updates physics-step action rules.
    /// </summary>
    void FixedTick(PlayerContext context, float fixedDeltaTime);

    /// <summary>
    /// Called once before the action is removed or replaced.
    /// </summary>
    void Exit(PlayerContext context);
}
```

### ILocomotionOverride

Opcional. Implementado apenas por acoes que alteram movimento.

```csharp
public interface ILocomotionOverride
{
    /// <summary>
    /// Modifies or replaces the locomotion frame before it is applied to the body.
    /// </summary>
    void ModifyLocomotionFrame(
        ref LocomotionFrame frame,
        PlayerContext context,
        float fixedDeltaTime);
}
```

Uma acao pode implementar apenas `IPlayerAction`, ou `IPlayerAction` + `ILocomotionOverride`.

## Componentes propostos

### PlayerController

Componente orquestrador do jogador.

Responsabilidades:

- ler ou receber snapshot de input;
- atualizar `PlayerContext`;
- chamar locomocao e action runner na ordem correta;
- manter fluxo `Update`/`FixedUpdate` claro.

Nao deve concentrar a matematica de movimento, combo, dano ou Card Time.

### PlayerContext

Objeto compartilhado com referencias controladas para sistemas do personagem.

Responsabilidades:

- expor input atual;
- expor motor, sensores, locomocao e action runner;
- expor direcao de facing;
- expor configs e dados necessarios para acoes.

`PlayerContext` deve evitar virar service locator generico. Ele deve conter somente dependencias diretamente usadas pelo controller do jogador.

### PlayerMotor2D

Componente responsavel por aplicar movimento no corpo.

Responsabilidades:

- manter referencia ao `Rigidbody2D`;
- aplicar `LocomotionFrame`;
- executar operacoes atomicas como `SetVelocity`, `SetFacing`, `ExecuteJump`;
- centralizar clamp de velocidade e gravidade quando fizer sentido.

### PlayerLocomotionController

Resolve locomocao base.

Responsabilidades:

- manter `PlayerLocomotionState`;
- consultar sensores;
- gerar `LocomotionFrame` base;
- consumir salto bufferizado quando permitido;
- aplicar coyote time, jump buffer, controle horizontal, gravidade e max fall speed.

### PlayerActionRunner

Gerencia a acao temporaria atual.

Responsabilidades:

- manter `IPlayerAction CurrentAction`;
- expor `ILocomotionOverride CurrentLocomotionOverride`;
- iniciar, substituir e encerrar acoes;
- encaminhar eventos de animacao para a acao atual;
- aplicar regras simples de prioridade/cancelamento no primeiro momento.

No primeiro slice, usar apenas uma action ativa por vez.

## Ordem de Update

### Update

```text
1. Capturar input em PlayerInputSnapshot.
2. PlayerActionRunner.Tick.
3. Processar pedidos nao fisicos: iniciar ataque, dash, etc.
4. Atualizar animacao/parametros visuais simples.
```

### FixedUpdate

```text
1. PlayerSensors2D.UpdateSensors.
2. PlayerLocomotionController.ResolveState.
3. PlayerActionRunner.FixedTick.
4. PlayerLocomotionController.BuildFrame.
5. Se houver CurrentLocomotionOverride, chamar ModifyLocomotionFrame.
6. PlayerMotor2D.ApplyFrame.
```

A ordem exata pode ser ajustada durante implementacao, mas a decisao importante e que a velocidade final seja aplicada uma vez por passo fisico, apos locomocao e acao terem contribuido.

## Acoes iniciais

### AttackAction

Ataques devem ser acoes temporarias com fases internas:

```csharp
public enum PlayerActionPhase
{
    Reading,
    Execution,
    Recovery
}
```

Responsabilidades:

- controlar timer ou reagir a eventos de animacao;
- habilitar/desabilitar hitbox;
- abrir janelas de combo;
- abrir janelas de Card Time;
- aplicar modificadores de locomocao por fase;
- escolher comportamento diferente para chao/ar somente quando necessario.

Comportamento de movimento esperado:

- no chao: reduzir controle horizontal e aplicar pequeno deslocamento para frente ou para tras conforme fase;
- no ar: reduzir gravidade severamente durante execucao, opcionalmente garantir pequeno lift vertical e aplicar nudge horizontal;
- durante recovery: devolver controle gradualmente ou encerrar a action.

### DashAction

Dash deve ser uma action com override forte.

Comportamento esperado:

- zerar velocidade vertical;
- ignorar gravidade durante duracao ativa;
- sobrescrever velocidade horizontal por `dashSpeed * facingDirection`;
- bloquear input horizontal enquanto ativo;
- encerrar por tempo, colisao ou regra futura de cancelamento.

Dash fica em `PlayerActionState.Dash`, nao em `PlayerLocomotionState.Dash`, para preservar contexto de chao/ar.

### HurtAction

Hurt deve ser uma action com override forte ou moderado, dependendo do dano recebido.

Comportamento esperado:

- receber knockback a partir do dano ou perfil de ataque;
- bloquear ataques/dash durante hitstun;
- opcionalmente manter gravidade ativa;
- encerrar apos hitstun;
- permitir que `Dead` substitua `Hurt` quando a vida chegar a zero.

## Dados e autoria

Usar `ScriptableObject` para tunings principais:

- `PlayerMovementConfigSO`
- `PlayerAttackDefinitionSO`
- `PlayerDashDefinitionSO`
- futuramente `PlayerHurtDefinitionSO` ou dados vindos de `DamageProfileSO`

`PlayerMovementConfigSO` deve conter inicialmente:

- velocidade horizontal maxima;
- aceleracao no chao;
- desaceleracao no chao;
- controle aereo;
- altura/forca de salto;
- coyote time;
- jump buffer;
- gravidade de subida;
- gravidade de queda;
- multiplicador de queda;
- velocidade maxima de queda.

Definicoes de ataque devem conter inicialmente:

- duracao de reading/startup;
- duracao de execution/active;
- duracao de recovery;
- multiplicador de controle no chao;
- multiplicador de gravidade no ar;
- lift minimo no ar;
- nudge horizontal por fase;
- janelas de combo;
- janelas de Card Time;
- referencia a `DamageProfileSO`.

## Animacao e eventos

Animation events podem ser usados como fonte de timing, mas nao devem conter regra de gameplay.

Fluxo recomendado:

```text
Animation Event
-> PlayerAnimationEventRelay
-> PlayerActionRunner.NotifyAnimationEvent(eventId)
-> CurrentAction decide o que fazer
```

Eventos esperados:

- `EnableHitbox`
- `DisableHitbox`
- `OpenComboWindow`
- `CloseComboWindow`
- `OpenCardWindow`
- `CloseCardWindow`
- `EndAction`

As actions tambem devem funcionar por timer no primeiro prototipo, para nao depender de animacoes finais.

## Regras anti-overengineering

1. Comecar com uma action ativa por vez.
2. Nao criar classe concreta para cada combinacao `GroundedAttack1`, `AirborneAttack1`, etc.
3. Diferenciar chao/ar dentro da action apenas quando houver comportamento realmente diferente.
4. Nao implementar stack de actions antes de necessidade clara.
5. Nao criar behavior tree para o jogador.
6. Nao separar `Idle` de `Grounded` antes de necessidade de animacao ou regra concreta.
7. Nao adicionar hierarchy profunda de estados antes de o baseline de movimento estar jogavel.

## Primeiro slice de implementacao

Ordem recomendada:

1. Criar enums atualizados para `PlayerLocomotionState`, `PlayerActionState` e `PlayerActionPhase`.
2. Criar `PlayerInputSnapshot`.
3. Criar `LocomotionFrame`.
4. Criar contratos `IPlayerAction` e `ILocomotionOverride`.
5. Criar `PlayerMovementConfigSO`.
6. Criar `PlayerMotor2D`.
7. Criar `PlayerSensors2D`.
8. Criar `PlayerLocomotionController` com `Grounded` e `Airborne`.
9. Criar `PlayerActionRunner`.
10. Criar `DashAction` simples.
11. Criar `AttackAction` simples com fases por timer.
12. Ligar tudo em `PlayerController`.

## Testes e validacao

Testes EditMode recomendados para estruturas puras:

- `LocomotionFrame` recebe override de dash e zera velocidade vertical.
- `AttackAction` reduz gravidade no ar durante `Execution`.
- `HurtAction` substitui velocidade com knockback.
- `PlayerActionRunner` chama `Exit` ao substituir action.
- `PlayerActionRunner` limpa action quando `IsComplete`.

Validacao manual em cena:

- movimento horizontal no chao deve responder de forma firme;
- pulo deve respeitar coyote time e jump buffer;
- queda deve ser legivel e nao escorregadia;
- ataque no ar deve segurar queda sem virar voo livre;
- dash deve ser contido, horizontal e previsivel;
- hurt deve interromper acoes e aplicar knockback claro.

## Decisoes em aberto

- Se `WallSlide` tera override proprio ou sera locomocao pura.
- Se parry/defesa sera action propria ou subtipo de Card Time.
- Se Card Time sera action separada, modificador da action atual ou janela publicada por evento.
- Se ataques usarao hitboxes por componentes ativados, queries fisicas manuais ou assets dedicados de attack shape.
- Se dash podera cancelar ataques ou somente ser usado fora deles no primeiro prototipo.

