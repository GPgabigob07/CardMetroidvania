# Cronograma Da Build De Novembro - 20260908-1015

## Contexto

Esta revisao reorganiza `gdd/cronograma-build-novembro-20260828-1401.md`
em 08/09/2026. A mudanca reflete a conclusao do desenho e da baseline mecanica
do inimigo voador Bat Machine. A semana atual passa a priorizar Level Design;
o trabalho do inimigo voador fica registrado na semana anterior.

## Estado Confirmado No Repositorio

- [x] Movimento, combate, dano, knockback, hitstop, vida/respawn, Card Time,
  inventario/catalogo e cinco cartas-base possuem runtime e cobertura EditMode.
- [x] Golem Charger e sua cena de teste existem como primeiro rank-and-file.
- [x] Bat Machine foi desenhado e implementado como segundo rank-and-file:
  patrulha aerea, monitoramento, steering, dodge, windup preditivo, tiros,
  poise, stun/fall/recovery e projeteis defletiveis com dano convertido.
- [x] Prefab, dano, cena de teste e assets iniciais do Bat Machine existem.
- [x] O passe de arte atual inclui sete sheets de animacao do Bat e um sheet
  de cinco frames para o projetil; a integracao visual final ainda esta em
  progresso e deve ser validada em Play Mode.
- [ ] Registrar a validacao Play Mode do Golem Charger, Bat Machine e Card
  Time; codigo/prefab nao substituem leitura em jogo.

## Regra De Escopo

Uma semana termina quando o entregavel esta demonstravel em Play Mode. Depois
de 02/11 nao entram novas mecanicas, inimigos, cartas ou salas; o trabalho se
limita a legibilidade, estabilidade, balanceamento e apresentacao.

## Cronograma Semanal Reorganizado

### 1--7 de setembro — inimigo voador: design e mecanicas

- [x] Definir o Bat Machine como segundo rank-and-file, contrastando com o
  Golem Charger: ranged flyer moderado, poise, dodge e risco de queda.
- [x] Implementar estados, patrulha, engage, windup, evade, stun fall,
  grounded recovery, dano, poise e projeteis defletiveis.
- [x] Criar prefab, dados e cena de teste; iniciar telegraphs e sheets visuais.
- [ ] Validar visualmente no Play Mode: leitura do windup, dodge, queda,
  projetil, deflexao e escala dos sprites.

**Saida:** segundo rank-and-file mecanicamente pronto para teste; arte e
integracao visual em refinamento, sem declarar acabamento final antes de Play
Mode.

### 8--14 de setembro — Level Design: area e encontros base

- [ ] Desenhar a topologia minima: entrada/checkpoint, duas ou tres salas de
  combate, trecho vertical, atalho e espaco reservado para elite/chefe.
- [ ] Construir blockout navegavel sem depender de arte final e conectar
  respawn/checkpoint provisório.
- [ ] Colocar Golem Charger e Bat Machine em encontros de teste que ensinem,
  respectivamente, charge terrestre e controle de espaco aereo/projetil.
- [ ] Fazer uma rota do inicio ate a futura arena do chefe; registrar
  bloqueios, softlocks e necessidades de tuning dos inimigos.

**Saida:** rota cinza navegavel com os dois rank-and-file em contexto real de
Level Design.

### 15--21 de setembro — cartas 6--10

- [ ] Especificar e implementar cinco cartas adicionais, priorizando efeitos
  que reutilizem dados e runtime existentes.
- [ ] Testar as dez cartas nos contextos Neutral, Chain e Finisher.

**Saida:** dez cartas cadastradas e prontas para balanceamento no fluxo real.

### 22--28 de setembro — Card Time e encontros

- [ ] Corrigir leitura de comandos, custo, feedback e transicoes do Card Time.
- [ ] Ajustar os dois encontros rank-and-file usando as dez cartas.

**Saida:** Card Time e encontros demonstraveis no blockout da area.

### 29 de setembro--5 de outubro — consolidar area e definir elite

- [ ] Fechar blockout principal, atalhos e progressao curta.
- [ ] Definir o elite: arena, dois padroes e abertura clara para carta/movimento.

### 6--12 de outubro — elite

- [ ] Implementar e provar o elite em arena isolada, depois inserir na area.

### 13--19 de outubro — chefe

- [ ] Implementar chefe controlado com dois padroes e arena isolada.

### 20--26 de outubro — integracao e playthrough

- [ ] Integrar chefe, elite, dois rank-and-file e recompensa/gate.
- [ ] Executar primeiro playthrough completo e corrigir bloqueios.

### 27 de outubro--2 de novembro — apresentacao e primeira build

- [ ] Arte/animacao/VFX/SFX essenciais, menu, encerramento e primeira build
  instalavel. Congelar escopo ao final da semana.

### 3--23 de novembro — playtest, estabilidade e QA

- [ ] Repetir playthroughs, corrigir bloqueadores, balancear e testar a build
  de distribuicao.

### 24 de novembro — entrega

- [ ] Gerar, testar, arquivar e registrar a build final e seu hash/commit.

## Marcos De Controle

- [ ] **14/09:** rota blockout inicial com Golem Charger e Bat Machine em
  encontros de teste.
- [ ] **28/09:** dez cartas funcionais e legiveis no Card Time.
- [ ] **26/10:** primeira versao zeravel com area, rank-and-file, elite e chefe.
- [ ] **02/11:** primeira build instalavel e congelamento de escopo.
- [ ] **16/11:** candidata estavel para entrega.
- [ ] **24/11:** build final entregue.

## Historico

Substitui o cronograma de 28/08 apenas como memoria de planejamento atual;
o documento anterior permanece preservado. Esta revisao foi baseada no estado
do repositorio e no progresso do Bat Machine em 08/09/2026.
