# Revisao do GDD-TIC

Data da revisao: 2026-05-26

Este documento analisa `.docs/GDD-TIC.md` em relacao a `.docs/TIC.md` e a referencias externas consultadas na web. Ele nao altera o GDD original; serve como trilha de decisao para a proxima passada de organizacao e complemento.

## Sumario executivo

O GDD ja possui uma identidade forte: metroidvania 2D com combate corpo a corpo, alta mobilidade, momentum, combate aereo e sistema de cartas como diferencial. A base teorica sustenta bem essa direcao, especialmente nos temas de nao linearidade guiada, ability gating, habilidade do jogador versus habilidade do personagem, backtracking, game feel e prototipagem por greybox.

O principal problema atual nao e falta de imaginacao, e sim falta de separacao entre camadas. O GDD mistura visao de jogo, especificacao de mecanica, apendice critico, sugestoes externas, repeticao de secoes e trechos incompletos. Para desenvolvimento em Unity, isso aumenta o risco de implementarmos uma mecanica interessante do jeito errado por falta de criterios de aceite, prioridades e limites numericos.

Recomendacao de alto nivel: reorganizar o GDD em uma versao canonica curta e navegavel, mover analises/riscos para apendices, e criar especificacoes separadas para os sistemas de maior risco: movimento, combate, Card Time, cartas, inimigos, camera/HUD e progressao espacial.

## Diagnostico do GDD

### O que esta faltando

1. Pilares de design explicitados

O GDD descreve a experiencia desejada, mas ainda nao declara 3 a 5 pilares que devem orientar qualquer decisao futura. Sugestao inicial:

- Maestria espacial: o jogador aprende o mapa e o atravessa melhor com habilidade real.
- Combate expressivo: cartas ampliam possibilidades, mas nao substituem execucao.
- Mobilidade com risco: permanecer no ar e manter momentum e poderoso, mas exige precisao.
- Nao linearidade guiada: o jogador sente liberdade, mas o prototipo tem fluxo controlado.
- Legibilidade acima de complexidade: todo estado, custo e janela precisa ser reconhecivel.

2. Core loop em formato operacional

O GDD cita plataforma, combate e repeticao, mas ainda falta um loop que oriente implementacao e testes:

Explorar sala -> encontrar barreira/desafio -> testar movimento/combate/cartas -> ganhar informacao, atalho, recurso ou carta -> retornar com nova capacidade -> dominar rota.

3. Vertical slice do prototipo

O escopo fala em 1 area/bioma, 5 inimigos, 1 chefe e cartas, mas falta definir a experiencia minima demonstravel:

- duracao alvo;
- numero aproximado de salas;
- habilidades obrigatorias;
- cartas obrigatorias;
- chefe ou mini-chefe;
- gates do prototipo;
- recompensas;
- criterio de "acabou o prototipo".

4. Matriz de progressao

Falta uma tabela que conecte habilidade, gate, tutorial, area de aquisicao, area de retorno e recompensa. Para metroidvania isso e mais importante do que uma lista solta de habilidades.

Exemplo de colunas:

| Habilidade/carta | Tipo de gate | Primeiro bloqueio visto | Onde aprende | Onde aplica | Recompensa | Permite sequence break? |
| --- | --- | --- | --- | --- | --- | --- |

5. Especificacao do sistema de cartas

O GDD tem muitas ideias de cartas, mas ainda faltam regras canonicas:

- deck, mao, slots e limite de cartas ativas;
- custo primario e custos alternativos;
- recuperacao de cartas comuns, limitadas e especiais;
- prioridade de resolucao quando duas cartas alteram a mesma acao;
- cancelamento, persistencia e expiracao de efeitos;
- feedback visual/sonoro por categoria;
- quais cartas pertencem ao prototipo.

6. Especificacao do Card Time

As janelas Alfa, Beta, Epsilon, Omega e Lambda sao promissoras, mas hoje parecem mais uma gramatica interna de combate do que uma experiencia ensinavel. Falta decidir:

- quais janelas existem no prototipo;
- se o jogador precisa ver nomes gregos na HUD ou apenas estados visuais;
- duracao base de cada janela;
- regra de slow motion;
- buffer de input;
- tolerancia para controle;
- falha esperada e consequencia.

7. Modelo de inimigos e encontros

O documento menciona 5 inimigos e 1 chefe, mas nao define papeis. Para testar combate e level design, os inimigos deveriam cobrir funcoes:

- batedor terrestre simples;
- inimigo de escudo/parry;
- inimigo aereo;
- inimigo de zona/projetil;
- elite que exige carta ou uso avancado de movimento.

8. Documento tecnico minimo

Ha uma secao tecnica, mas ainda muito curta para orientar Unity:

- versao exata da Unity e pacotes;
- arquitetura de cenas;
- estrategia de Tilemap/Grid;
- camadas de colisao;
- Input System;
- Cinemachine/camera;
- save/checkpoint;
- dados das cartas em ScriptableObjects;
- telemetria/debug;
- criterios de performance.

9. UX de mapa, marcadores e leitura espacial

Metroidvania depende de memoria espacial. Falta definir:

- se havera mapa;
- se salas descobertas aparecem automaticamente;
- se o jogador pode marcar gates;
- como portas/gates ficam codificados visualmente;
- como segredos sao sugeridos sem ficarem obvios.

10. Audio e feedback

O GDD cita feedback visual e sonoro no apendice, mas nao ha uma secao canonica de audio. Para um jogo de timing, isso e central:

- som de janela valida;
- som de carta invalidada;
- hit confirm;
- parry/defesa perfeita;
- estado de vida critica;
- perigo fora da tela;
- musica por bioma/combate/chefe.

11. Acessibilidade e configuracoes

Como o jogo mira dificuldade mecanica, acessibilidade nao deve ser confundida com "facilitar". Falta definir:

- remapeamento de controles;
- sensibilidade/coyote time/input buffer;
- contraste de HUD;
- reducao de flashes;
- volume separado;
- assistencias opcionais para leitura sem alterar o desafio principal.

12. Criterios de aceite

Para cada sistema, falta uma lista objetiva de quando esta "bom o suficiente". Exemplo:

- jogador atravessa 3 salas de teste sem perder controle do momentum;
- parry tem janela legivel e reproduzivel;
- uma carta nunca pode ser ativada sem feedback;
- chefe pode ser vencido sem cartas, mas cartas reduzem tempo/risco.

### O que esta insuficiente

1. Level Design

O conceito de regioes, areas e salas esta bom, mas insuficiente para construcao. Falta topologia: loops, atalhos, locks antes das keys, rotas de retorno, salas de respiro e salas de teste de habilidade.

2. Publico-alvo

"Jovens-adultos a partir de 16 anos" e pouco. O GDD precisa falar de perfil de jogador:

- familiaridade com Hollow Knight/Ori/Nine Sols;
- tolerancia a falha;
- preferencia por execucao tecnica;
- duracao de sessao;
- interesse em buildcrafting.

3. Estilo visual

A oposicao futurista-limpo x medieval-fantastico e boa, mas precisa virar regra de producao:

- paleta por faccao/bioma;
- silhuetas de inimigos;
- linguagem de gates;
- escala de sprites;
- densidade de detalhe;
- regra de legibilidade durante Card Time.

4. Movimento

O movimento e provavelmente o coracao do jogo, mas falta numeros de referencia:

- velocidade horizontal;
- aceleracao/desaceleracao;
- altura e tempo de pulo;
- gravidade subida/queda;
- coyote time;
- jump buffer;
- wall climb;
- double jump;
- regras de momentum aereo;
- teto de velocidade.

5. Combate

O combo de 3 a 4 golpes esta bem encaminhado, mas faltam frame data e prioridades:

- startup, active, recovery;
- cancel windows;
- hitstop;
- knockback;
- stun;
- dano por golpe;
- regra para whiff;
- regra para inimigos grandes/pequenos;
- comportamento ao acertar no ar.

6. Balanceamento

A formula de dano existe, mas o GDD ainda nao define faixas. Sem faixas, a formula vira uma caixa de amplificacao dificil de testar.

7. Projeto tecnico

Para Unity, a secao esta muito leve. Ela deve ser complementada antes do baseline, porque definira como implementar sistemas sem acoplamento excessivo.

### O que esta demasiado

1. Complexidade inicial do Card Time

Cinco janelas com nomes proprios, regras diferentes e cartas condicionais podem ser demais para o prototipo. Isso nao significa remover a ideia, mas o prototipo deve testar uma versao reduzida.

Proposta: prototipar 3 estados canonicos:

- Neutral: antes/fora de combo.
- Chain: durante combo.
- Finisher: fim de combo/4o golpe.

Alfa, Beta, Epsilon, Omega e Lambda podem voltar depois como refinamentos internos ou nomes de subestados.

2. Cartas com efeitos extremos antes da base estar validada

Expeditioner, Easymode, Storm, Berserk e Bullseye sao interessantes, mas muito perigosas para baseline. Devem ficar no banco de ideias ate movimento, dano, vida, recuperacao e feedback estarem estaveis.

3. Formula de dano cedo demais detalhada

A formula atual tenta resolver escala, buffs, flat damage, critico e bonus de golpe. Para prototipo, talvez seja melhor separar:

- dano base por golpe;
- multiplicadores temporarios;
- modificadores finais com teto;
- critico apenas se for habilidade executavel, nao sorte pura.

4. Analise critica dentro do corpo do GDD

A analise de riscos e util, mas hoje interrompe o documento, repete secoes e termina incompleta. Deve virar apendice limpo ou documento separado.

5. Repeticao da Parte 2

As secoes 4 a 8 aparecem duas vezes: primeiro de forma autoral, depois organizadas como "Parte 2". A versao organizada e mais util para agentes, mas precisa ser fundida com a original para evitar divergencia.

## De-para: web x teoria local x GDD

| Tema | Web consultada | Base teorica local | Estado no GDD | Acao recomendada |
| --- | --- | --- | --- | --- |
| Estrutura de GDD | Fontes de GDD reforcam visao, gameplay, historia, level design, UI, arte, audio, tecnico, producao, riscos e revisoes como secoes comuns. | TIC foca mais no argumento teorico do genero do que em producao. | GDD cobre varias secoes, mas audio, producao, riscos e especificacoes estao misturados ou vazios. | Criar GDD canonico + apendices + especificacoes por sistema. |
| Ability gating | Level Design Book trata gates como qualquer bloqueio de fluxo, incluindo hard/soft gates, shortcuts e lock/key; recomenda mostrar lock antes da key e lembrar o jogador do lock ao obter a key. | TIC sustenta ability gating, backtracking, nao linearidade guiada e "nos mentais". | GDD cita ability gating moderado, incisivo e sequence break. | Transformar em matriz de gates, locks, keys, shortcuts e recompensas. |
| Backtracking | Entrevista com Team Cherry/PC Gamer enfatiza mapa coerente, muitas conexoes e backtracking prazeroso. | TIC reforca backtracking significativo e recontextualizacao do espaco. | GDD quer 3 conexoes por area, mas sem rotas concretas. | Definir loops, atalhos e retorno no prototipo. |
| Movimento e game feel | Fontes de Unity/Physics 2D indicam que precisao fisica e performance dependem de configuracoes de simulacao, gravidade, iteracoes e velocidade maxima. | TIC cita Swink, RLD, metricas de salto, greyboxing e iteracao de game feel. | GDD define momentum em texto, mas sem metricas. | Criar especificacao de controlador 2D antes de construir fases. |
| Unity 6.3 LTS | Unity confirma Unity 6.3 LTS com suporte ate dezembro de 2027 e pacote de melhorias/estabilidade. | TIC aponta Unity como escolha acessivel para metroidvanias e gestao de cenas/prefabs. | GDD diz Unity 6, sem versao/pacotes. | Fixar Unity 6.3 LTS e listar pacotes base. |
| Tilemap/level iteration | Unity Tilemap facilita criar e iterar levels 2D com Tile Assets, Grid, Tilemap Renderer e Collider 2D. | TIC recomenda greyboxing e metrica espacial antes da arte final. | GDD nao define tecnica de construcao de mapa. | Usar Tilemap para blocagem/colisao 2D e separar camada visual/colisao/gates. |
| Input | Unity recomenda Input System Package como solucao mais nova e flexivel. | TIC nao aprofunda input, mas o GDD mira controle e teclado. | GDD lista controles, mas nao arquitetura de input. | Usar Input System com actions separadas por Gameplay, UI, Card Time e Debug. |
| HUD/camera | Fontes de GDD recomendam wireframes, HUD, camera e controle como partes explicitas. Unity/Cinemachine e relevante para camera dinamica. | TIC aborda feedback e legibilidade indiretamente. | HUD e camera existem, mas ainda sem wireframe, estados e comportamento de prioridade. | Criar UX spec com estados: exploracao, combate, Card Time, chefe, pausa. |

## Complementos sugeridos por secao

### 2. Resumo do Projeto

Adicionar:

- elevator pitch de 1 frase;
- pilares de design;
- referencias de experiencia;
- promessa do prototipo;
- escopo negativo: o que nao entra no prototipo.

### 2.4 Publico-alvo

Complementar com perfil psicografico:

- jogadores de metroidvania/action-platformer;
- aceitam falha como aprendizado;
- gostam de dominio tecnico e rotas alternativas;
- preferem controle responsivo e combate expressivo;
- toleram narrativa ambiental fragmentada.

### 2.7 Escopo do Prototipo

Transformar em checklist:

- 1 bioma;
- 12 a 18 salas de greybox;
- 2 habilidades de movimento;
- 3 cartas prototipo;
- 3 inimigos comuns;
- 1 elite;
- 1 chefe;
- 2 gates obrigatorios;
- 2 segredos;
- 1 atalho desbloqueavel;
- 1 checkpoint;
- 1 tela de mapa simples.

### 3.1 Loop de Gameplay

Reescrever como loop primario e loops secundarios:

- Primario: explorar -> desafiar -> aprender -> desbloquear -> retornar.
- Combate: aproximar -> atacar -> abrir janela -> usar carta -> manter vantagem -> finalizar/recuar.
- Plataforma: ler obstaculo -> executar movimento -> corrigir momentum -> alcancar rota/recompensa.
- Falha: morrer -> retornar ao checkpoint -> aplicar conhecimento -> reduzir tempo/risco.

### 3.2 Movimento

Criar tabela de metricas:

| Metrica | Valor inicial | Observacao |
| --- | --- | --- |
| Velocidade horizontal | A definir | Deve ser ajustada por feeling. |
| Altura do pulo | A definir | Base para blocagem. |
| Tempo ate apex | A definir | Afeta precisao e responsividade. |
| Coyote time | A definir | Ajuda sem reduzir desafio. |
| Jump buffer | A definir | Essencial para controle. |
| Max fall speed | A definir | Evita queda incontrolavel. |

### 3.2.3 Cartas

Organizar cada carta em ficha:

| Campo | Descricao |
| --- | --- |
| Nome | Nome da carta |
| Categoria | Feitico, encantamento, magia, zenite |
| Janela valida | Neutral, Chain, Finisher ou subestado |
| Custo | Tempo, vida, carga, uso limitado |
| Efeito | Resultado mecanico |
| Cancelamento | O que encerra o efeito |
| Feedback | Visual/sonoro/HUD |
| Risco | Balanceamento/bug/compreensao |
| Entra no prototipo? | Sim/nao |

### 4. Level Design

Adicionar matriz:

| Sala | Funcao | Mecanica ensinada/testada | Gate | Recompensa | Retorno |
| --- | --- | --- | --- | --- | --- |

### 5. Interface e Controles

Adicionar:

- wireframe textual do HUD;
- estados de exibicao;
- prioridades de alerta;
- remapeamento;
- input buffer;
- layout de controle recomendado;
- modo debug de hitbox/hurtbox/Card Time.

### 6. Projeto Tecnico

Adicionar baseline para Unity:

- Unity 6.3 LTS;
- URP 2D;
- Input System;
- Cinemachine;
- Tilemap + Grid + Tilemap Collider 2D;
- ScriptableObjects para cartas, inimigos e ataques;
- scene loading por area/sala quando necessario;
- eventos desacoplados para combate/HUD/audio;
- ferramentas de debug para hitboxes, estados e dano;
- testes/play mode para formulas e regras de cartas.

### 7. Consideracoes de Design

Manter como manifesto do projeto, mas mover exemplos muito detalhados para apendice. Esta secao deve responder: "como decidimos quando duas ideias entram em conflito?"

### 8. Equipe

Complementar com workflow:

- versao do documento;
- responsavel por decisao final;
- pipeline de playtest;
- criterio de aceite de milestones;
- ferramenta de tarefas;
- convencoes de branch/commit.

## Perguntas para o usuario

1. O Card Time deve ser o diferencial principal do jogo ou apenas uma camada de expressao sobre um combate que funciona bem sem cartas?

2. Para o prototipo, voce prefere manter os nomes Alfa/Beta/Epsilon/Omega/Lambda visiveis ao jogador, ou tratar isso como nomenclatura interna e mostrar apenas estados mais intuitivos?

3. O protagonista deve ser mais proximo de "guerreiro tecnico com cartas" ou "viajante tecnologico que traduz magia como cartas"? Isso muda arte, HUD, narrativa e feedback.

4. O prototipo deve mirar qual duracao jogavel: 10-15 minutos, 20-30 minutos ou 45-60 minutos?

5. O movimento deve pender mais para Ori/MIO, com fluidez e ar/momentum, ou para Hollow Knight/Nine Sols, com combate mais preciso e leitura de inimigo?

6. Cartas devem ser recurso de deck/build antes da luta, ou ferramenta moment-to-moment durante o combo?

7. O jogo deve permitir sequence break no prototipo, ou isso fica como objetivo para depois da baseline?

8. A dificuldade desejada e "exigente mas ensinavel" ou "hardcore desde o inicio"?

9. O mapa do prototipo deve incluir tela de mapa jogavel desde cedo, ou podemos iniciar com marcadores/debug e implementar mapa depois?

10. Voce quer que eu agora edite o GDD original fundindo Parte 1 + Parte 2, ou prefere que eu crie uma nova versao canonica `GDD.md` preservando `GDD-TIC.md` como fonte historica?

## Fontes externas consultadas

- Unity 6 release support: https://unity.com/releases/unity-6/support
- Unity 6 release page: https://unity.com/releases/unity-6
- Unity Input manual: https://docs.unity3d.com/Manual/Input.html
- Unity Physics 2D reference: https://docs.unity3d.com/Manual/class-Physics2DSettings.html
- Unity Tilemaps manual: https://docs.unity3d.com/Manual/Tilemap.html
- Level Design Book, Gates: https://book.leveldesignbook.com/process/layout/typology/gates
- Game Design Document overview: https://www.game-developers.org/glossary/game-design-document-gdd
- PC Gamer, Metroidvania map design / Hollow Knight interview: https://www.pcgamer.com/how-to-design-a-great-metroidvania-map/

