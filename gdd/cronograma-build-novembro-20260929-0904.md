# Cronograma Da Build De Novembro — Estado Em 29/09/2026

## Contexto E Historico

Esta versao atualiza `gdd/cronograma-build-novembro-20260828-1401.md` com o
trabalho integrado ate 29/09. Preserva as datas da build prevista para
**24/11/2026** e distingue implementacao, blockout e validacao em Play Mode.
Fontes adicionais: `specs/blue-area-tutorial-corridor-20260911-1500.md`,
`specs/gameplay-scene-ownership-sdd-20260920-1853.md`,
`specs/neutral-chain-card-baseline-sdd-20260921-1107.md`,
`specs/card-time-opportunity-identity-and-neutral-rearm-sdd-20260922-1010.md`,
`specs/environmental-hazard-checkpoint-recovery-sdd-20260922-1616.md`,
`specs/environmental-hazard-checkpoint-recovery-timing-20260928-1822.md`,
as cenas e assets salvos no repositorio e os testes em Play Mode relatados
pelo autor nesta conversa.

## Estado Confirmado

- [x] Movimento, combate, dano, knockback, hitstop, vida, Card Time,
  inventario/catalogo e cartas demonstrativas possuem runtime. Golem Charger
  e Bat Machine existem como dois rank-and-file com prefabs e cenas de teste.
- [x] Cinco cartas Neutral/Chain planejadas foram implementadas: Grounded
  Double Jump, Dash Enabler, Jump Boost, Poise Damage e Growing Reach. O Golem
  Charger recebeu poise. O fluxo de Card Time recebeu correcao para rearmar
  Neutral no chao e distinguir oportunidades Chain consecutivas.
- [x] A sala inicial e o corredor tutorial Blue foram bloqueados, com estagios
  de Charger, desbloqueio de Card Time e gates de melee melhorado. O autor
  relatou testes em Play Mode dessas partes; o passe completo de encontros
  ainda precisa de registro.
- [x] A cena Gameplay passou a manter jogador, camera e HUD, enquanto Blue e
  Pink sao cenas de area. O carregamento direcional permite a travessia sem
  porta de loading obrigatoria e descarrega Blue antes do cruzamento geometrico
  com Pink. A migracao e o mecanismo de carregamento foram testados pelo autor.
- [x] O perimetro Pink e o corredor inicial estao salvos em blockout. O trecho
  vertical, os encontros e o caminho ate a arena final ainda nao formam uma
  rota completa validada.
- [x] Pink possui checkpoint e hazard configuravel. O contato aplica dano e
  recupera o jogador no marcador ativo sob cobertura preta. O tempo minimo
  total e configuravel e agora vale **0,75 s**; o autor confirmou em Play Mode
  o contato, a travessia do trigger e o retorno ao checkpoint Pink.
- [ ] Registrar validacao completa do Golem Charger, Bat Machine, gates e
  cartas em encontros de area, inclusive controles/feedback e repeticao apos
  morte ou troca de cena.
- [ ] Fechar o blockout navegavel ate a futura arena do chefe, com atalhos,
  encontros e primeira passagem de ponta a ponta.

## Regra De Escopo

Uma semana termina quando o entregavel esta demonstravel em Play Mode. Depois
de 02/11 nao entram novas mecanicas, inimigos, cartas ou salas. Os marcos de
14/09 e 28/09 abaixo permanecem como referencias originais: ambos ficaram
parcialmente abertos, sem declarar conclusao retroativa.

## Cronograma Semanal

### 1--7 de setembro — inimigo voador: design e mecanicas

- [x] Definir e implementar o Bat Machine: estados, patrulha, engage, windup,
  evade, stun fall, grounded recovery, dano, poise e projeteis defletiveis.
- [x] Criar prefab, dados, cena de teste, telegraphs e sheets visuais iniciais.
- [ ] Validar em Play Mode windup, dodge, queda, projetil, deflexao e escala;
  registrar os resultados.

**Saida:** segundo rank-and-file mecanicamente pronto para teste; validacao
integral ainda pendente.

### 8--14 de setembro — Level Design: area e encontros base

- [x] Bloquear e testar a sala inicial e o corredor tutorial Blue.
- [x] Definir a separacao das rotas Blue/Pink e bloquear o perimetro Pink.
- [x] Integrar cena Gameplay persistente e carregamento de areas.
- [x] Conectar um checkpoint Pink e recovery por hazard; Play Mode confirmou
  o retorno ao marcador com o tempo minimo de 0,75 s.
- [ ] Completar a topologia jogavel: salas de combate, trecho vertical,
  atalhos e espaco para elite/chefe.
- [ ] Colocar Golem Charger e Bat Machine em encontros que ensinem charge
  terrestre e controle de espaco aereo/projetil; validar ritmo em Play Mode.
- [ ] Testar a rota ate a futura arena do chefe e registrar bloqueios.

**Saida atual:** Blue tutorial testado e Pink com perimetro/recuperacao; ainda
nao ha rota cinza completa com os dois inimigos em contexto.

### 15--21 de setembro — cartas 6--10

- [x] Implementar as cinco cartas Neutral/Chain planejadas para substituir o
  conjunto demonstrativo no prototipo.
- [ ] Especificar, implementar e testar cinco cartas adicionais para atingir
  o marco original de dez cartas distintas. As cinco acima nao completam este
  item automaticamente.

### 22--28 de setembro — Card Time e encontros

- [x] Corrigir o rearmamento Neutral e a identidade de oportunidades Chain.
- [x] Corrigir o comportamento de gatilhos/ground sensing observado nos
  testes do checkpoint Pink.
- [ ] Fechar leitura de comandos, custos e feedback das cartas em Play Mode
  e ajustar os encontros Blue/Pink.

### 29 de setembro--5 de outubro — consolidar area e definir elite

- [ ] Fechar o blockout principal e atalhos, priorizando a rota ate a futura
  arena e a colocacao dos dois rank-and-file em contexto.
- [ ] Definir conceito minimo do elite.
- [ ] Registrar uma passagem de ponta a ponta e os bloqueios encontrados.

### 6--12 de outubro — elite

- [ ] Implementar elite em arena isolada e inserir na area.

### 13--19 de outubro — chefe

- [ ] Implementar chefe controlado com dois padroes e arena isolada.

### 20--26 de outubro — integracao e playthrough

- [ ] Integrar encontros, recompensa/gate e primeiro playthrough completo.

### 27 de outubro--2 de novembro — apresentacao e primeira build

- [ ] Arte/animacao/VFX/SFX essenciais, menu e build instalavel; congelar escopo.

### 3--23 de novembro — playtest, estabilidade e QA

- [ ] Repetir playthroughs, corrigir bloqueadores e balancear.

### 24 de novembro — entrega

- [ ] Gerar, testar e arquivar a build final e seu hash/commit.

## Marcos

- [ ] **14/09:** blockout inicial com ambos os rank-and-file em contexto.
  Blue tutorial e Pink perimetro estao presentes; o marco completo continua
  aberto.
- [ ] **28/09:** dez cartas funcionais e legiveis. As cinco novas cartas
  Neutral/Chain existem; o total-alvo e a validacao de leitura permanecem
  abertos.
- [ ] **26/10:** primeira versao zeravel.
- [ ] **02/11:** primeira build instalavel e congelamento de escopo.
- [ ] **24/11:** build final entregue.
