# Unity Script Asset File Layout - 20260526-0000

## Contexto

Esta especificacao complementa `specs/prototype-architecture-sdd-20260525-2200.md` e registra uma regra obrigatoria para scripts Unity.

## Regra

Todo script que declare um tipo concreto derivado de `MonoBehaviour` ou `ScriptableObject` deve ter uma declaracao top-level em um arquivo com o mesmo nome da classe.

Exemplos:

- `GameStateController` deve ficar em `GameStateController.cs`.
- `AbilityDefinitionSO` deve ficar em `AbilityDefinitionSO.cs`.
- `BoolEventChannelSO` deve ficar em `BoolEventChannelSO.cs`.
- `BoolEventChannelListener` deve ficar em `BoolEventChannelListener.cs`.

## Aplicacao

1. Nao agrupar multiplos `MonoBehaviour` concretos no mesmo arquivo.
2. Nao agrupar multiplos `ScriptableObject` concretos no mesmo arquivo.
3. Classes abstratas de infraestrutura podem compartilhar padroes genericos, mas devem preferir arquivo homonimo quando herdam de `MonoBehaviour` ou `ScriptableObject`.
4. Tipos auxiliares que nao sao script-assets, como enums, payloads, structs e `UnityEvent<T>` serializaveis, podem compartilhar arquivos quando fizer sentido.

## Motivo

O Unity Editor usa o nome do arquivo e da classe para resolver scripts que podem ser anexados a GameObjects, criados como assets ou exibidos corretamente no Inspector. Manter um script-asset por arquivo evita scripts quebrados, assets nao criaveis pelo menu e problemas de serializacao/referencia.

## Impacto na arquitetura atual

Os event channels concretos e listeners concretos devem ser separados em arquivos homonimos:

- `BoolEventChannelSO.cs`
- `IntEventChannelSO.cs`
- `FloatEventChannelSO.cs`
- `StringEventChannelSO.cs`
- `GameStateEventChannelSO.cs`
- `AbilityUnlockEventChannelSO.cs`
- `DamageEventChannelSO.cs`
- `InteractionEventChannelSO.cs`
- `BoolEventChannelListener.cs`
- `IntEventChannelListener.cs`
- `FloatEventChannelListener.cs`
- `StringEventChannelListener.cs`
- `GameStateEventChannelListener.cs`

