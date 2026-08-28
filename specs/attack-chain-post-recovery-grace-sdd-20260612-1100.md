# Attack Chain Post-Recovery Grace SDD - 20260612-1100

## Contexto

Esta versao complementa:

- `specs/attack-chain-buffer-and-speed-sdd-20260612-1022.md`

O playtest mostrou que a janela dentro da animacao ainda exige precisao
excessiva. O combo agora preserva memoria por um curto periodo depois do fim de
`Recovery`.

## Decisao

Cada comportamento de animacao pode configurar:

```text
postRecoveryBufferGraceDuration
sequenceRestartCooldown
```

Valores iniciais para os tres estados de `Recovery`:

```text
postRecoveryBufferGraceDuration = 0.5 s
sequenceRestartCooldown = 0.5 s
```

As janelas correm em paralelo a partir do fim de `Recovery`:

- input durante a grace continua para o proximo golpe do combo;
- depois da grace, o follow-up anterior expira;
- input antes do fim do cooldown nao inicia um novo combo;
- input quando o cooldown termina inicia `Attack1`;
- `Attack3` nao possui follow-up, mas respeita o cooldown antes de reiniciar.

Com ambos os valores em `0.5`, nao existe intervalo morto: antes de `0.5 s` o
input continua o combo; a partir de `0.5 s` ele inicia uma nova sequencia.

Esses tempos sao segundos de gameplay e ficam separados das janelas
normalizadas internas da animacao. Uma futura politica de attack speed pode
decidir explicitamente se tambem escala esses tempos; por enquanto eles nao
escalam.
