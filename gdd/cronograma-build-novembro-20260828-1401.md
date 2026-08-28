# Cronograma Da Build De Novembro - 20260828-1401

## Contexto

Este documento atualiza o cronograma de producao da build prevista para
**24/11/2026**, a ultima terca-feira de novembro. Ele foi criado em
28/08/2026 depois de uma revisao do estado real do repositorio, para substituir
a suposicao de que os sistemas-base ainda precisariam ser iniciados do zero.

O planejamento considera uma capacidade pessimista de **8 horas por semana**
(sempre abaixo de 10 h), cerca de 100 horas restantes. A meta continua sendo
uma build curta, jogavel do inicio ao fim: uma area, dois inimigos
rank-and-file, um elite, um chefe, dez cartas e apresentacao
audiovisual/tecnica suficiente para demonstracao.

### Fontes usadas

- `gdd/gdd-canonico-20260526-2331.md`
- `specs/state-machine-owned-state-evolution-sdd-20260806-2146.md`
- `specs/golem-charger-enemy-sdd-20260806-2146.md`
- `specs/card-time-selection-ui-presentation-20260707-1027.md`
- `specs/player-facing-health-respawn-sdd-20260617-1717.md`
- Codigo, dados, prefabs e cenas presentes em `Assets/` em 28/08/2026.

## Diagnostico Da Baseline

O projeto ja possui uma base de gameplay superior a um prototipo inicial. A
prioridade deixa de ser criar arquitetura e passa a ser **validar em Play Mode,
transformar prototipos em encontros e produzir conteudo/apresentacao**.

### Ja adiantado no repositorio

- [x] Movimento, acoes do jogador, ataque em cadeia, hit detection, dano,
  knockback, hitstop, vida e respawn de prototipo possuem implementacao e
  testes de EditMode.
- [x] A arquitetura de maquina de estados tipada e a extensao com estados que
  recebem o dono existem em `Assets/Scrips/Architecture/StateMachines/`.
- [x] Os patrulheiros terrestre e aereo existem como prefabs legados
  (`GroundedPatrolEnemy.prefab` e `AerialPatrolEnemy.prefab`), mas estao
  depreciados e nao contam para a meta da build.
- [x] Existe a base do primeiro inimigo rank-and-file, Golem Charger: cerebro por estados,
  carga, politica de dano por estado/regiao, hitboxes, prefab, animacoes e uma
  cena de teste dedicada.
- [x] O Card Time possui runtime, inventario/catalogo, selecao por comando,
  HUD, indicadores e feedback de mundo.
- [x] Cinco cartas de dados ja foram criadas: `KnockbackCharges`,
  `ExtraJump`, `BaseDamageOvercharge`, `EscalatingDamage` e
  `DoubleHitEnergy`.
- [x] Ha assets preliminares de animacao do jogador, UI de HUD, conceitos dos
  inimigos e animacoes preliminares do Golem Charger.
- [x] Em 28/08/2026, `dotnet build TicGame.Architecture.csproj --no-restore`
  concluiu com 0 avisos e 0 erros.

### Ainda precisa de validacao ou producao

- [ ] Executar e registrar validacao em Play Mode do jogador, do Golem Charger
  e do Card Time. A existencia de codigo, testes
  e prefab nao substitui a leitura em jogo.
- [ ] Criar o segundo inimigo rank-and-file e definir seu papel de combate em
  contraste com o Golem Charger; nao reutilizar os patrulheiros depreciados.
- [ ] Produzir as cartas 6--10 e testar as dez no fluxo real de Card Time.
- [ ] Criar area conectada, salas, checkpoint, progressao curta e uma rota
  completa ate a arena do chefe.
- [ ] Implementar um inimigo elite e o chefe. O Golem Charger conta apenas
  como o primeiro dos dois rank-and-file.
- [ ] Fechar arte de producao, VFX, SFX, musica/ambiencia, menus e testes da
  build. Os assets atuais sao baseline, nao confirmacao de acabamento final.

## Regra De Escopo

Uma semana termina quando seu entregavel esta demonstravel em Play Mode, nao
quando apenas compila. Reservar aproximadamente **1 hora por semana** para
integracao, correcao e anotar decisoes de tuning.

Depois de 02/11, nenhuma mecanica, inimigo, carta ou sala nova entra no
escopo. A partir dessa data, o trabalho so pode melhorar legibilidade,
estabilidade, balanceamento e apresentacao.

## Cronograma Semanal Atualizado

### 24--31 de agosto — consolidar a semana atual

- [x] Maquina de estados base e evolucao owner-aware implementadas.
- [x] Conceitos visuais de inimigos e prefabs de patrulheiros legados estao
  disponiveis como referencia, mas nao como conteudo da meta.
- [x] Base do Golem Charger, incluindo cena de teste e assets preliminares,
  iniciada antes do fim da semana.
- [ ] Fazer uma sessao curta de Play Mode do Golem Charger e registrar:
  movimento, deteccao, dano recebido, dano ao jogador, morte e legibilidade.
- [ ] Corrigir somente bloqueios encontrados nessa validacao; nao abrir novas
  variantes de inimigo.

**Saida:** o primeiro rank-and-file possui uma baseline demonstravel e uma
lista curta de ajustes priorizados.

### 1--7 de setembro — fechar o primeiro rank-and-file e especificar o segundo

- [ ] Validar o Golem Charger no fluxo completo: idle/patrol, windup, charge,
  dano, morte e leitura de seus telegraphs.
- [ ] Corrigir somente os bloqueios do Golem Charger encontrados em Play Mode.
- [ ] Definir o segundo rank-and-file: papel, uma acao de ataque, estados,
  requisito de arte e uma interacao que o diferencie do Golem Charger.

**Saida:** Golem Charger pronto como primeiro rank-and-file e segundo inimigo
especificado para producao.

### 8--14 de setembro — implementar o segundo rank-and-file

- [ ] Implementar prefab, estados, ataque, dano, morte e feedback provisório
  do segundo rank-and-file.
- [ ] Criar uma sala de teste com os dois rank-and-file e ajustar valores em
  Play Mode.
- [ ] Registrar o conceito minimo do elite, incluindo a interacao de combate
  que ele deve cobrar das cartas ou do movimento.

**Saida:** dois rank-and-file diferentes, demonstraveis e prontos para povoar
a area; elite definido, mas ainda nao implementado.

### 15--21 de setembro — expandir o conjunto para dez cartas

- [x] Catalogo, inventario, selecao e cinco cartas-base ja existem.
- [ ] Especificar as cartas 6--10 em uma lista curta: categoria, janela
  (Neutral/Chain/Finisher), custo, efeito, feedback e interacao pretendida.
- [ ] Implementar primeiro as variacoes que reutilizam efeitos/dados atuais;
  so criar um novo tipo de efeito se ele gerar uma decisao de combate distinta.

**Saida:** dez cartas cadastradas, ao menos cinco novas prontas para teste.

### 22--28 de setembro — fechar o Card Time como sistema de jogo

- [ ] Testar as dez cartas com teclado e gamepad nos tres contextos de Card
  Time.
- [ ] Corrigir falhas de leitura: abertura/fechamento de janela, comandos,
  custo, feedback de sucesso/falha e estados ativos.
- [ ] Fazer um passe de balanceamento que impeça cartas de substituir timing,
  posicionamento e ataque basico.

**Saida:** mecanismo de cartas completo e demonstravel, com dez cartas
utilizaveis na build.

### 29 de setembro--5 de outubro — blockout da area completa

- [ ] Desenhar a topologia minima: entrada/checkpoint, 2--3 salas de combate,
  trecho vertical, encontro dos rank-and-file, encontro do elite, atalho e
  arena do chefe.
- [ ] Construir o blockout em uma cena de area sem depender de arte final.
- [ ] Conectar respawn/checkpoint provisório e testar a rota do inicio ate a
  arena.

**Saida:** area cinza navegavel de ponta a ponta.

### 6--12 de outubro — elite, primeira versao

- [ ] Implementar um elite de escopo controlado: uma arena e dois padroes de
  ataque, reutilizando o sistema de dano, health e state machine existentes.
- [ ] Implementar estados, hitboxes, vida, morte e uma abertura clara para
  dano/carta.
- [ ] Criar o encontro isolado e provar que pode ser vencido.

**Saida:** elite derrotavel em arena de teste.

### 13--19 de outubro — chefe, primeira versao

- [ ] Implementar um chefe de escopo controlado: uma arena e dois padroes de
  ataque, reutilizando sistemas existentes.
- [ ] Implementar estados, hitboxes, vida, morte e uma abertura clara para
  dano/carta.
- [ ] Criar o encontro isolado e provar que pode ser vencido.

**Saida:** chefe derrotavel em arena de teste.

### 20--26 de outubro — integrar encontros e progressao

- [ ] Inserir chefe, elite e os dois rank-and-file na area segundo os papeis
  de cada encontro.
- [ ] Adicionar uma recompensa/gate simples apos o chefe e garantir que a
  rota principal termina corretamente.
- [ ] Realizar o primeiro playthrough completo cronometrado; corrigir somente
  bloqueios, softlocks e picos de dificuldade evidentes.

**Saida:** Alpha de conteudo: build zeravel com todos os requisitos de
gameplay presentes.

### 27 de outubro--2 de novembro — arte, audio e primeira build completa

- [ ] Substituir blockout apenas na rota principal: tiles/props, arena,
  silhuetas dos inimigos e sinais dos ataques.
- [ ] Finalizar os frames/animacoes essenciais que comunicam locomocao, os
  tres ataques, dano/morte e ataques especiais dos inimigos/chefe.
- [ ] Produzir VFX minimos para acerto, dano, Card Time, carta ativa,
  interrupcao do elite e morte do chefe.
- [ ] Adicionar SFX essenciais, uma faixa musical/ambiencia reutilizavel, menu
  simples, tela de encerramento e configuracao de build.
- [ ] Gerar a primeira build instalavel e completar um playthrough fora do
  Editor.

**Saida:** Beta de conteudo/apresentacao. A partir daqui, escopo congelado.

### 3--9 de novembro — playtest e balanceamento

- [ ] Fazer pelo menos tres playthroughs completos, incluindo um sem usar
  cartas e um usando-as ativamente.
- [ ] Corrigir bugs de progressao, morte/respawn, colisao, input, Card Time e
  chefe.
- [ ] Ajustar vida/dano/tempos para que cartas ampliem execucao, sem dominar o
  combate.

**Saida:** Release Candidate 1, terminavel sem bug bloqueador conhecido.

### 10--16 de novembro — estabilidade tecnica e acabamento

- [ ] Testar em configuracao de distribuicao: nova instalacao, inputs,
  resolucao, audio, inicio e encerramento.
- [ ] Otimizar apenas gargalos observados; remover logs e elementos de
  depuracao visiveis.
- [ ] Fazer o passe final de HUD, contraste, textos e feedback audiovisual.

**Saida:** Release Candidate 2, com lista de riscos curta e priorizada.

### 17--23 de novembro — QA final

- [ ] Repetir o fluxo completo varias vezes e registrar defeitos por
  severidade.
- [ ] Corrigir apenas bloqueadores, falhas de salvamento/progressao, crashes e
  problemas graves de leitura.
- [ ] Empacotar, nomear e fazer backup da candidata final.

**Saida:** build final candidata validada.

### 24 de novembro — entrega

- [ ] Gerar a build de entrega a partir do estado validado.
- [ ] Testar instalacao e uma partida curta na build final.
- [ ] Arquivar o pacote, notas de versao e o hash/commit correspondente.

## Marcos De Controle

- [ ] **14/09:** dois rank-and-file funcionais em Play Mode; elite definido.
- [ ] **28/09:** dez cartas funcionais e legiveis no Card Time.
- [ ] **26/10:** primeira versao zeravel com area, dois rank-and-file, elite e
  chefe.
- [ ] **02/11:** primeira build instalavel; congelamento de escopo.
- [ ] **16/11:** candidata estavel para entrega.
- [ ] **24/11:** build final entregue.

## Historico

Este e um novo documento de memoria, criado em vez de alterar o GDD canonico.
Ele preserva as decisoes de design existentes e registra um plano de producao
recalibrado pelo estado do repositorio em 28/08/2026.
