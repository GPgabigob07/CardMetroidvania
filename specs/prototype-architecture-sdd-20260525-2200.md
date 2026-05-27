# Prototype Architecture SDD - 20260525-2200

## Contexto

Este documento define a primeira especificacao tecnica do prototipo metroidvania 2D em Unity. Ele usa como fontes locais `.docs/GDD-TIC.md`, `.docs/TIC.md`, `gdd/gdd-review-20260525-2143.md` e as referencias oficiais da Unity consultadas para Unity 6.3 LTS, ScriptableObjects, event channels, Input System, 2D/Tilemap, URP e Unity Test Framework.

O projeto local esta em Unity `6000.3.16f1`, isto e, Unity 6.3 LTS. A Unity documenta que Unity 6.3 LTS tem suporte ate dezembro de 2027 e que releases LTS recebem suporte por dois anos. A pagina de upgrade do Unity 6.3 tambem remove o URP Compatibility Mode: projetos em 6.3 devem assumir URP Render Graph como caminho normal, evitando render features antigas baseadas em Compatibility Mode.

## Objetivo da arquitetura

Criar uma baseline escalavel para prototipo, sem depender de pacotes externos neste momento, usando:

- Unity 6.3 LTS;
- URP 2D ja presente no projeto;
- Input System ja presente no projeto;
- Tilemap e 2D packages ja presentes no projeto;
- ScriptableObjects para dados, configuracoes e canais de evento;
- interfaces pequenas para interacao por comportamento;
- state machines explicitas para GameState, PlayerMovement e EnemyAI;
- dados serializaveis para facilitar testes, balanceamento e autoria no Inspector.

## Principios

1. Modulos pequenos, nao "player controller blob".
2. Objetos conversam por contratos e eventos, nao por referencias rigidas sempre que possivel.
3. ScriptableObjects guardam definicoes, canais e metadados; MonoBehaviours executam runtime.
4. Transacoes entre sistemas devem carregar payloads claros.
5. Sistemas devem ser testaveis em C# puro quando nao dependem de cena/fisica.
6. Unity scene objects devem depender de interfaces como `IDamageable`, `IInteractable` e `ICapabilityProvider`.
7. Recursos de cena especificos de uma sala nao devem virar servicos globais.

## Modulos base

### Boot e Game Loop

Fluxo alvo:

`Boot -> MainMenu -> LoadGame -> Gameplay -> Pause -> Death -> Respawn`

Responsabilidades:

- `GameStateController`: controla estado global e publica mudancas por event channel.
- `GameStateEventChannelSO`: permite HUD, audio, pausa, save/load e fluxo reagirem sem acoplamento direto.
- Estados devem ser serializaveis por enum simples para facilitar logs e testes.

### Player Capability System

Habilidades e upgrades devem ser modelados como capacidades consultaveis:

- dash;
- wall jump;
- double jump;
- defense/parry;
- Card Time;
- ranged attack, se entrar;
- chaves mecanicas/gating tags.

Cada `AbilityDefinitionSO` deve expor:

- id;
- nome de exibicao;
- tipo;
- input action id;
- custo de stamina/recurso;
- cooldown;
- animation trigger;
- gating tags;
- se inicia desbloqueada.

Objetos do mundo nao devem fazer `if player has dash` hardcoded. Em vez disso:

- world gate pergunta para `ICapabilityProvider`;
- ability data possui `GameplayTagSO`;
- o gate compara tags requeridas contra capacidades desbloqueadas.

### Room e World Structure

O mundo deve ser tratado como rooms/areas conectadas por portas, transicoes, checkpoints e locks.

Comecar simples:

- rooms como cenas ou prefabs;
- metadados em `RoomDefinitionSO`;
- Tilemap/Grid para blockout e colisao;
- Camera bounds por sala;
- spawn points identificados por string.

`RoomDefinitionSO` deve conter:

- room id;
- area id;
- scene name;
- display name;
- camera bounds id;
- spawn ids;
- door ids;
- checkpoint ids;
- map reveal cell ids;
- room tags.

Addressables ficam fora da baseline ate load time/memoria justificarem.

### Data-driven Content

Usar ScriptableObjects para:

- abilities;
- damage profiles;
- gameplay tags;
- room metadata;
- event channels;
- futuramente: cards, enemy stats, attack data, item definitions e map cells.

Dados runtime ficam em componentes ou saves. Definicoes ficam em assets.

### Event Channels

Padrao base:

- `EventChannelBaseSO`: descricao e metadados comuns.
- `EventChannelSO<T>`: canal generico com payload.
- `VoidEventChannelSO`: sinal sem payload.
- canais concretos para bool, int, float, string, GameState, AbilityUnlock, Damage e Interaction.

Uso esperado:

- Input levanta intencoes.
- Player/combat levanta eventos de dano, hit, morte, unlock.
- HUD/audio/camera escutam os canais relevantes.
- Sistemas se desinscrevem no `OnDisable`.

### Interfaces de comportamento

Contratos iniciais:

- `IIdentified`: objeto com id estavel.
- `IInteractable`: objeto que pode ser consultado e acionado por um contexto de interacao.
- `IDamageable`: objeto que recebe `DamageContext` e retorna `DamageResult`.
- `IDamageSource`: objeto capaz de descrever origem/tags de dano.
- `ICapabilityProvider`: objeto consultavel por capacidades/tags.
- `IGameplayModule`: modulo inicializavel e desligavel por fluxo.

### State Machines

State machines explicitas:

- `GameState`: Boot, MainMenu, LoadGame, Gameplay, Pause, Death, Respawn.
- `PlayerMovementState`: Grounded, Airborne, WallSlide, Dash, Attack, Hurt, Dead, Interact.
- `EnemyAIState`: Idle, Patrol, Alert, Chase, Attack, Stagger, Dead.

Regras:

- state machine pura em C#;
- estados implementam `IState<TStateId>`;
- MonoBehaviours adaptam Unity Update/FixedUpdate para a state machine.

### Testing Strategy

Testes iniciais recomendados:

- regras de unlock de abilities;
- comparacao de gating tags;
- calculo de dano;
- transicao de GameState;
- reveal de mapa;
- migracao de save data quando existir.

Usar Unity Test Framework. A Unity documenta `runTests` como argumento de CLI para executar testes em projeto e `testPlatform` para selecionar EditMode ou PlayMode.

## Pacotes e componentes

### Ja presentes no projeto

- `com.unity.inputsystem`: input moderno e remapeavel.
- `com.unity.render-pipelines.universal`: URP 17.3.0.
- `com.unity.test-framework`: testes EditMode/PlayMode.
- `com.unity.2d.tilemap`, `com.unity.2d.tilemap.extras`: rooms/blockouts.
- `com.unity.2d.spriteshape`: terreno organico/cavernas.
- `com.unity.2d.animation`: opcional para rig 2D.
- `com.unity.2d.aseprite`: pipeline de sprites/animacoes Aseprite.

### A considerar depois

- Cinemachine 3 para cameras 2D, confiners, boss arenas e transicoes. Ainda nao esta no manifest local.
- Newtonsoft JSON para saves/debug exports. Ainda nao esta no manifest local.
- Localization para UI/dialogo quando houver texto final.
- Addressables apenas se cenas/prefabs por room causarem custo de memoria/load.

## Baseline de pastas de codigo

Codigo inicial em:

`Assets/Scrips/Architecture/`

Observacao: o caminho segue a solicitacao atual do usuario (`Scrips`). Caso o projeto adote a convencao Unity comum, uma versao futura pode migrar para `Assets/Scripts/`, preservando historico.

Subpastas:

- `Core`: tags, contratos, enums e tipos compartilhados.
- `Data`: ScriptableObjects de definicao.
- `Events`: event channels e listeners.
- `StateMachines`: state machine pura e enums.
- `Runtime`: componentes MonoBehaviour de runtime.

## Decisoes da versao

1. Nao adicionar pacotes externos agora.
2. Nao instalar Cinemachine/Newtonsoft/Localization nesta etapa.
3. Criar somente contratos e estruturas base, sem player controller completo.
4. Preferir payloads serializaveis para transacoes.
5. Deixar cards especificos para uma proxima spec dedicada.

## Fontes consultadas

- Unity 6 release support: https://unity.com/releases/unity-6/support
- Unity 6.3 upgrade guide: https://docs.unity.cn/6000.3/Documentation/Manual/UpgradeGuideUnity63.html
- ScriptableObject architecture e-book: https://unity.com/resources/create-modular-game-architecture-scriptableobjects-unity-6
- ScriptableObjects as event channels: https://unity.com/how-to/scriptableobjects-event-channels-game-code
- Unity Input System: https://docs.unity.cn/Packages/com.unity.inputsystem%401.14/manual/index.html
- Cinemachine package manual: https://docs.unity3d.com/Manual/com.unity.cinemachine.html
- Unity Tilemaps: https://docs.unity3d.com/Manual/Tilemap.html
- Tilemap Collider 2D reference: https://docs.unity3d.com/Manual/tilemaps/work-with-tilemaps/tilemap-collider-2d-reference.html
- Unity 2D overview: https://unity.com/features/2d
- Unity Test Framework CLI: https://docs.unity.cn/Packages/com.unity.test-framework%402.0/manual/reference-command-line.html

