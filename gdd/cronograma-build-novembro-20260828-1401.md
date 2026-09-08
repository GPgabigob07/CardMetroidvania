# Cronograma Da Build De Novembro

## Contexto

Cronograma unico e vivo da build prevista para **24/11/2026**. Git preserva
suas versoes anteriores; este arquivo representa sempre o plano atual.

## Estado Confirmado No Repositorio

- [x] Movimento, combate, dano, knockback, hitstop, vida/respawn, Card Time,
  inventario/catalogo e cinco cartas-base possuem runtime e testes EditMode.
- [x] Golem Charger existe como primeiro rank-and-file com cena de teste.
- [x] Bat Machine foi desenhado e implementado como segundo rank-and-file:
  patrulha aerea, monitoramento, steering, dodge, windup preditivo, tiros,
  poise, stun/fall/recovery e projeteis defletiveis com dano convertido.
- [x] Prefab, dano, cena de teste e assets iniciais do Bat Machine existem.
- [x] O passe de arte atual inclui sete sheets do Bat e cinco frames do
  projetil; integracao visual final continua dependente de Play Mode.
- [ ] Registrar validacao Play Mode do Golem Charger, Bat Machine e Card Time.

## Regra De Escopo

Uma semana termina quando o entregavel esta demonstravel em Play Mode. Depois
de 02/11 nao entram novas mecanicas, inimigos, cartas ou salas.

## Cronograma Semanal

### 1--7 de setembro — inimigo voador: design e mecanicas

- [x] Definir e implementar o Bat Machine: estados, patrulha, engage, windup,
  evade, stun fall, grounded recovery, dano, poise e projeteis defletiveis.
- [x] Criar prefab, dados, cena de teste, telegraphs e sheets visuais iniciais.
- [ ] Validar em Play Mode windup, dodge, queda, projetil, deflexao e escala.

**Saida:** segundo rank-and-file mecanicamente pronto para teste.

### 8--14 de setembro — Level Design: area e encontros base

- [ ] Desenhar topologia: entrada/checkpoint, 2--3 salas de combate, trecho
  vertical, atalho e espaco para elite/chefe.
- [ ] Construir blockout navegavel e conectar respawn/checkpoint provisório.
- [ ] Colocar Golem Charger e Bat Machine em encontros que ensinem charge
  terrestre e controle de espaco aereo/projetil.
- [ ] Testar a rota ate a futura arena do chefe e registrar bloqueios.

**Saida:** rota cinza navegavel com os dois rank-and-file em contexto real.

### 15--21 de setembro — cartas 6--10

- [ ] Especificar, implementar e testar cinco cartas adicionais.

### 22--28 de setembro — Card Time e encontros

- [ ] Corrigir leitura de comandos/custos/feedback e ajustar encontros.

### 29 de setembro--5 de outubro — consolidar area e definir elite

- [ ] Fechar blockout principal, atalhos e conceito minimo do elite.

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

- [ ] **14/09:** blockout inicial com ambos os rank-and-file.
- [ ] **28/09:** dez cartas funcionais e legiveis.
- [ ] **26/10:** primeira versao zeravel.
- [ ] **02/11:** primeira build instalavel e congelamento de escopo.
- [ ] **24/11:** build final entregue.
