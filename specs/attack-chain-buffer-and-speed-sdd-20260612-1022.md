# Attack Chain Buffer and Speed SDD - 20260612-1022

## Contexto

Esta versao revisa o momento de commit do proximo golpe. O documento anterior
mantinha o ataque bufferizado ate o fim completo de `Recovery`; o comportamento
correto permite iniciar o proximo ataque quando a action atual alcanca 50% de
`Recovery`.

Isso preserva integralmente `WindUp`, `Execution` e a primeira metade de
`Recovery`, enquanto a metade final de `Recovery` funciona como tempo
cancelavel somente para um follow-up de combo ja bufferizado.

## Historico e fontes

Esta especificacao substitui, como decisao corrente:

- `specs/attack-chain-buffer-and-speed-sdd-20260612-1015.md`

As demais decisoes, fontes e requisitos de attack speed daquele documento
continuam validos, exceto onde este documento os altera explicitamente.

## Decisoes canonicas

### Janelas de input e commit

Buffer e commit sao permissoes diferentes:

| Fase | Progresso normalizado | Aceita buffer | Pode iniciar follow-up |
|---|---:|---|---|
| Reading/WindUp | `0.0 .. 1.0` | Nao | Nao |
| Execution | `0.0 .. < 0.5` | Nao | Nao |
| Execution | `0.5 .. 1.0` | Sim | Nao |
| Recovery | `0.0 .. < 0.5` | Sim | Nao |
| Recovery | `0.5 .. 1.0` | Sim | Sim, se houver buffer |

Consequencias:

- um input entre 50% e 100% de `Execution` fica guardado;
- um input na primeira metade de `Recovery` fica guardado;
- ao atingir 50% de `Recovery`, um buffer existente inicia o proximo ataque;
- um input recebido depois de 50% de `Recovery` pode iniciar o proximo ataque
  imediatamente;
- sem buffer, a action atual toca `Recovery` ate o fim;
- cada follow-up sempre entra em seu proprio `WindUp` desde o inicio.

Os thresholds iniciais devem ser authorable por ataque:

```text
executionBufferStartNormalized = 0.5
recoveryCommitStartNormalized = 0.5
```

### Semantica do cancelamento

O commit do follow-up e um cancelamento controlado da metade final de
`Recovery`. Ele nao e um cancelamento generico de action.

Somente o proximo ataque valido da sequencia pode usar essa permissao. Dash,
Hurt, cartas e outras actions continuam seguindo suas proprias prioridades e
politicas de cancelamento.

`Attack3` continua sem follow-up no combo basico e, portanto, toca seu
`Recovery` completo salvo interrupcao por outro sistema autorizado.

### Estado publicado pelo Animator

`PlayerActionFrame` deve distinguir as duas permissoes:

```csharp
PlayerActionPhase Phase;
float NormalizedPhaseTime;
bool CanBufferFollowUp;
bool CanCommitFollowUp;
bool EndAction;
bool HasAnimatorAuthority;
```

Para os estados iniciais:

```text
Execution:
    CanBufferFollowUp = normalizedTime >= 0.5
    CanCommitFollowUp = false

Recovery:
    CanBufferFollowUp = true
    CanCommitFollowUp = normalizedTime >= 0.5
```

Comparacoes devem usar `>=`, com o progresso limitado a `0..1`.

### Fluxo de runtime

```text
valid attack input
    -> CanBufferFollowUp?
    -> store one follow-up
    -> CanCommitFollowUp?
    -> consume buffer
    -> exit current action early
    -> enter next attack at WindUp
```

O runtime deve verificar commit:

- quando recebe um input valido;
- quando o frame de animacao cruza o threshold de commit.

As duas verificacoes sao necessarias. A primeira permite commit imediato para
inputs tardios; a segunda consome um input que ja estava bufferizado antes de
Recovery chegar a 50%.

O buffer deve ser consumido atomicamente ao iniciar o follow-up, evitando que
mais de uma transicao seja disparada no mesmo frame.

## Attack speed

As regras de attack speed da versao anterior permanecem:

- velocidade e capturada no inicio de cada action;
- duracoes efetivas usam `baseDuration / resolvedAttackSpeed`;
- buffs e debuffs recebidos durante um golpe afetam o proximo golpe;
- janelas usam progresso normalizado, nao segundos;
- valores sao limitados por clamps positivos.

Assim, attack speed altera quanto tempo real leva para chegar a 50% de
`Recovery`, mas nao altera a proporcao protegida da fase.

Exemplo:

```text
base Recovery = 0.20 s
speed 2.0      = commit possivel apos 0.05 s de Recovery
speed 0.5      = commit possivel apos 0.20 s de Recovery
```

## Fallback por timer

Quando o Animator nao possui autoridade, `AttackAction` deve reproduzir a
mesma regra:

```text
recoveryProgress = elapsedInRecovery / effectiveRecoveryDuration
CanCommitFollowUp = recoveryProgress >= recoveryCommitStartNormalized
```

O fallback nao deve esperar `effectiveRecoveryDuration` completa quando existe
um follow-up e o threshold de commit ja foi alcancado.

## Testes revisados

### EditMode

- WindUp nunca aceita buffer nem commit;
- Execution em `0.49` rejeita buffer;
- Execution em `0.50` aceita buffer, mas nao permite commit;
- Recovery em `0.49` aceita buffer, mas nao permite commit;
- Recovery em `0.50` aceita buffer e permite commit;
- buffer criado durante Execution e consumido ao cruzar Recovery `0.50`;
- input recebido em Recovery `0.75` comita imediatamente;
- sem buffer, Recovery termina normalmente;
- o commit inicia o proximo ataque em WindUp;
- o buffer e consumido uma unica vez;
- Attack3 nao comita follow-up basico;
- Hurt e Dead limpam o buffer antes de qualquer commit;
- fallback por timer respeita os mesmos thresholds em diferentes speeds.

### PlayMode

- uma pressao na segunda metade de Execution encadeia em 50% de Recovery;
- uma pressao na primeira metade de Recovery encadeia em 50% de Recovery;
- uma pressao na segunda metade de Recovery encadeia imediatamente;
- uma pressao antecipada nao fica armazenada;
- sem input, a animacao de Recovery toca ate o fim;
- o proximo WindUp sempre toca por completo;
- o comportamento permanece proporcional com buffs e debuffs de attack speed.

## Ordem de implementacao revisada

1. Publicar `NormalizedPhaseTime`, `CanBufferFollowUp` e
   `CanCommitFollowUp`.
2. Configurar Execution para buffer a partir de `0.5`.
3. Configurar Recovery para buffer completo e commit a partir de `0.5`.
4. Fazer o combo runtime armazenar somente um follow-up.
5. Avaliar commit tanto na entrada de input quanto na atualizacao do frame.
6. Consumir o buffer atomicamente e iniciar o proximo `WindUp`.
7. Reproduzir a regra no fallback por timer.
8. Integrar o snapshot de attack speed descrito na versao anterior.
9. Cobrir thresholds, interrupcoes e diferentes velocidades com testes.
