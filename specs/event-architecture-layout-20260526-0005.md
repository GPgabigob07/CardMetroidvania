# Event Architecture Layout - 20260526-0005

## Contexto

Esta especificacao complementa:

- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/unity-script-asset-file-layout-20260526-0000.md`

Ela define a organizacao fisica dos scripts de eventos em `Assets/Scrips/Architecture/Events`.

## Estrutura

Separar infraestrutura nativa/generica de implementacoes concretas criaveis no Inspector:

```text
Events/
  Natives/
    Bus/
    Listener/
  Concrete/
    Bus/
    Listener/
```

## Definicoes

### Natives

`Natives` contem tipos-base reutilizaveis, abstratos ou auxiliares. Eles formam o protocolo de eventos e nao representam um canal especifico de gameplay.

Exemplos:

- `EventChannelBaseSO`
- `EventChannelSO<TPayload>`
- `EventChannelListener<TPayload, TChannel, TUnityEvent>`
- `VoidEventChannelSO`, quando tratado como bus primitivo nativo
- `VoidEventChannelListener`, quando tratado como listener primitivo nativo
- `PayloadUnityEvents`

### Concrete

`Concrete` contem canais e listeners especificos do projeto. Eles sao os tipos que designers/programadores devem criar ou anexar para casos de uso concretos.

Exemplos:

- `BoolEventChannelSO`
- `GameStateEventChannelSO`
- `DamageEventChannelSO`
- `BoolEventChannelListener`
- `GameStateEventChannelListener`

## Bus e Listener

`Bus` contem ScriptableObjects/event channels que publicam eventos.

`Listener` contem MonoBehaviours/adapters que escutam canais e encaminham chamadas para UnityEvents, HUD, audio, camera ou outros sistemas.

## Regra Unity preservada

Mesmo dentro dessa hierarquia, todo `MonoBehaviour` ou `ScriptableObject` concreto deve continuar em arquivo homonimo:

- `Concrete/Bus/GameStateEventChannelSO.cs`
- `Concrete/Listener/GameStateEventChannelListener.cs`

