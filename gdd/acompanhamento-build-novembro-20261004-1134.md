# Acompanhamento da build de novembro — 04/10/2026

## Contexto

Atualizacao da analise de cronograma com o relato do autor em 04/10 e a
implementacao do Gargoyle Sentinel posterior a leitura anterior. Preserva o
cronograma original; este registro complementa seu estado de acompanhamento,
sem alterar automaticamente datas ou assumir novas entregas.

Fontes:
- `gdd/cronograma-build-novembro-20260828-1401.md`.
- Relato do autor nesta conversa em 04/10/2026.
- `gdd/gargoyle-sentinel-design-20261003-1128.md`.
- `specs/gargoyle-sentinel-verification-20261003-1925.md`.
- `specs/gargoyle-sentinel-animation-draft-checkpoint-20261003-2002.md`.

## Estado confirmado pelo autor

| Frente | Estado | Pendencia |
| --- | --- | --- |
| Golem Charger e Bat Machine | Validacao pratica realizada; posicionamento produz o desafio esperado | Ajustes em ambos, principalmente inconsistencias do Bat Machine; causas ainda nao diagnosticadas |
| Movimento | Build distribuida a algumas pessoas | Coletar e consolidar feedback |
| Areas | O blockout existente terminara em uma sala de chefe apos a subida ingreme no fim da area rosa, com o elite | Concluir/integrar esse trecho e validar o percurso; o relato define o destino, sem afirmar que a sala ja esta pronta |
| Cartas | Parecem balanceadas para o momento atual | Avaliar balanceamento com o elite finalizado |
| Menu | Suficiente por enquanto | Revisar escolha de controle ou permitir troca automatica entre WASD + mouse, gamepad e setas + JK; alternativa ainda nao escolhida |
| Elite | Implementado desde a analise anterior | Validacao pratica em andamento |

## Evidencias recentes do repositorio

Gargoyle Sentinel possui implementacao e arena de teste. A verificacao de 03/10
registra 599 casos EditMode: 576 aprovados, 20 falhos e 3 ignorados; PlayMode:
9 aprovados. As identidades das 20 falhas coincidem com a baseline anterior,
segundo o registro. Estes resultados sao historicos, nao uma nova execucao.

O checkpoint de arte registra 25 animacoes em rascunho, ainda fora de Assets e
sem integracao automatica. Implementacao, aceite de gameplay e acabamento
visual permanecem etapas distintas.

## Impacto no cronograma

- A implementacao do elite prevista para 06–12/10 foi antecipada. Validacao e
  insercao no percurso ainda precisam ser fechadas.
- A validacao pratica dos inimigos comuns deixa de ser uma pendencia inicial;
  o trabalho restante e de ajuste, com prioridade para o Bat Machine.
- O movimento ja recebe playtest externo. Balanceamento das cartas depende
  do encontro com o elite, conforme o autor.
- O menu nao exige uma reformulacao neste momento; a pendencia identificada
  e o tratamento dos tres modos de controle.
- O plano antigo separa elite e chefe. O relato atual coloca o elite na sala
  de chefe do fim do blockout, mas nao confirma se um chefe adicional foi
  retirado do escopo. Nao contar um segundo inimigo final como obrigatorio
  nem considerar seu cancelamento confirmado sem esclarecer essa diferenca.
- Mantem-se como referencias os marcos de 26/10 (versao zeravel), 02/11
  (congelamento de escopo) e 24/11 (entrega).

## Prioridades sugeridas para acompanhamento

1. Concluir a validacao do elite e registrar ajustes de leitura e combate.
2. Reproduzir e corrigir as inconsistencias observadas do Bat Machine.
3. Integrar o encontro final ao percurso da area rosa e testar a rota completa.
4. Consolidar feedback de movimento e reavaliar cartas no contexto do elite.
5. Definir e validar a escolha/troca dos tres modos de controle.

As 20 falhas herdadas continuam exigindo triagem antes do aceite tecnico final;
o relato de playtest nao estabelece que tenham sido resolvidas.
