Título do Jogo

Subtítulo do jogo

Sumário

1. Histórico do Projeto	2

2. Resumo do Projeto	3

2.1 Conceito do Jogo	3

2.2 Características Gerais	3

2.3 Gênero	3

2.4 Público-alvo	4

2.5 Fluxo Básico do Jogo	4

2.6 Estilo Visual (Olhar e Sentir)	4

2.7 Escopo do Protótipo	4

3. Jogabilidade e Mecânicas	4

3.1 Estrutura de Jogabilidade	4

3.1.1 Progressão do Jogo	5

3.1.2 Estrutura de Desafios	5

3.1.3 Objetivos	5

3.1.4 Loop de Gameplay	5

3.2 Mecânicas Principais	6

3.2.1 Física do Jogo	6

3.2.2 Movimentação do Personagem	7

3.2.3 Habilidades	7

3.2.4 Interação com o Ambiente	9

3.2.5 Combate	10

4. Level Design	11

4.1 Estrutura do Mapa	11

4.2 Progressão Espacial	11

4.3 Áreas e Conexões	11

5. Interface e Controles	11

5.1 HUD	12

5.2 Câmera	12

5.3 Controles	12

6. Projeto Técnico	12

6.1 Plataforma-alvo	12

6.2 Ferramentas e Engine	12

6.3 Requisitos básicos	12

7. Considerações de Design	12

7.1 Relação Habilidade do Jogador x Habilidade do Personagem	12

7.2 Curva de Aprendizado	12

7.3 Dificuldade e Balanceamento	12

8. Equipe	12

🔴 Problemas Críticos (Alto Risco)	12

1. Sistema de Cartas + Combos (AA String)	13

2. Fórmula de Dano	14

3. Movimentação e Física	15

🟡 Problemas Moderados (Médio Risco)	16

1. Sistema de Vida (Hits)	16

2. Cartas Limitadas e "Descanço"	16

3. Cartas com Efeitos Complexos	16

4. Inimigos e Hazards	17

🟢 Problemas Menores (Baixo Risco, mas Importantes)	17

📌 Recomendações Gerais	18

1. Prototipação Rápida	18

2. Balanceamento Iterativo	18

3. Feedback Visual e Sonoro	18

4. Documentação para a Equipe	18

🎯 Resumo de Ações Prioritárias	19

# 1. Histórico do Projeto

| 24/04/2026 | Início das definições de projeto e escopo |
| --- | --- |
| 08/05/2026 | Características gerais e idealização das habilidades |
| 15/05/2026 | Denifição de escopo, estilo e level design |

# 2. Resumo do Projeto

Este projeto de jogo trata-se de um metroidvania com o objetivo de atingir uma gameplay mecanicamente simples, entretanto escalável e que requeira boa coordenação motora e estratégica do jogador.

Trata-se de um metroidvania 2D focado em combate corpo a corpo com alta mobilidade e gerenciamento de habilidades através de cartas.

## 2.1 Conceito do Jogo

O jogador controla um viajante interdimensional preso em um castelo medieval habitado por golens e entidades artificiais.

Sem energia suficiente para retornar ao seu mundo, o protagonista deve explorar o castelo em busca de uma fonte energética capaz de alimentar sua máquina dimensional.

Durante a jornada, o personagem descobre uma conexão entre a tecnologia de seu mundo e a “magia” local, permitindo o uso de cartas capazes de alterar suas habilidades, ataques e propriedades físicas.

## 2.2 Características Gerais

- Metroidvania 2D com foco em exploração e combate.
- Combate corpo a corpo de curta e média distância.
- Sistema de habilidades organizado através de cartas.
- Progressão baseada em domínio mecânico e exploração.
- Movimentação baseada em física com momentum.
- Combate aéreo incentivado.

## 2.3 Gênero

O projeto seguirá os princípios de metroidvania como base, elencando elementos como:

- Ação
- Plataforma
- Aventura
- Soulslike

## 2.4 Público-alvo

O público-alvo, dado a dificuldade esperada do projeto, são jovens-adultos, a partir dos 16 anos, sem nenhuma distinção imediata de gênero.

## 2.5 Fluxo Básico do Jogo

O jogo seguirá com duas abordagens de fluxo: sequência contínua de interação com agentes inimigos e plataformas, e combate dedicado (lutas contra chefes ou salas de desafios)

## 2.6 Estilo Visual (Olhar e Sentir)

O estilo visual do jogo será em pixel art, focando em formas mais primitivas para as estruturas, inimigos e interagíveis, bem como terá um contraste entre um futurista-limpo e medieval-fantástico.

O jogador precisa ser capaz de reconhecer a que temática estática de determinada cena apenas pela junção de cores e formas, onde o futurista se limita ao número total de arestas e cores com mais contraste, enquanto o medieval tende a ter mais cores e formas mais irregulares

## 2.7 Escopo do Protótipo

Considerando um jogo metroidvania, a este protótipo não caberia grande expansão da área explorável, resultando em uma quantidade mais controlada de inimigos e cenas.

O protótipo então, deverá contar com uma área/bioma completo, cerca de 5 inimigos diferentes e um chefe, e pelo menos 4 cartas (habilidades núcleo) básicas juntamente de 2 cartas em cada uma das outras categorias

# 3. Jogabilidade e Mecânicas

O jogo contará com mecânicas comuns ao gênero metroidvania, como movimentação 2D com platforming, movimentação baseada em física, combate de curta-média distância e será compatível com teclado e controle.

## 3.1 Estrutura de Jogabilidade

A jogabilidade está sendo pensada para que a agência do jogador seja a maior influenciadora da performance do personagem em relação ao mundo do jogo, permitindo e incentivando o crescimento do jogador, para que ele aprimore sua habilidade motora e cognitiva em relação ao jogo, e que junto a isso, consiga aproveitar das mecânicas de variadas formas ou extremamente especializada em uma exímia forma.

### 3.1.1 Progressão do Jogo

O jogo contará com ability gate como o principal vetor de progressão mecânica, que estarão alinhadas com pontos específicos da exploração do mapa e da progressão lúdica atrelada a história.

### 3.1.2 Estrutura de Desafios

Os desafios apresentados no jogo devem permitir que o jogador use das ferramentas disponíveis bem como de seu conhecimento de forma livre, não limitando a apenas uma rota para o sucesso, que pode ser feita pela inclusão de elementos interagíveis nos cenários, conjunto de inimigos específicos, conjunto de plataformas e inimigos específicos.

Como o jogo tenderá muitas facetas de jogabilidade, todos os desafios atrelados a um fluxo de jogabilidade específico devem deixar claro para o jogador quais são os pré-requisitos para acessar o desafior, ex: uma plataforma que está longe o suficiente do jogador, impedindo que a alcance sem antes ter o pulo duplo liberado ou do uso do momentum vertical.

### 3.1.3 Objetivos

Tratando-se de um metroidvania, os objetivos do jogo devem estar atrelados com a progressão lúdica e espacial, de forma parcialmente elusiva: o jogador tem uma noção do que precisa ser feito em uma determinada área, mas a execução de tal tarefa poderá estar atrelada a um contexto separado, em outra área ou com possuir determinado conhecimento e/ou ferramenta.

### 3.1.4 Loop de Gameplay

O loop de gameplay pode ser divido em três etapas: plataforma, combate e repetição. O jogador travessará por um número de plataforma, encontrando inimigos eventuais que podem ou não serem combatidos, até que ele atinja o seu objetivo (seja empírico ou heurístico), repetindo esse processo caso ele falhe (perca toda sua vida).

Ao entrar em combate, projeta-se que a depender da familiaridade com as mecânicas, cartas e botões, o jogador será capaz de lidar rapidamente com a maioria dos inimigos menores, e possivelmente até mesmo com inimigos de elite.

Durante a progressão o jogador encontrará salas de combate, que ele só poderá avançar após finalizar uma sequência de ondas de inimigos, estas salas devem recompensar o jogador de acordo com a dificuldade proposta pelas mesmas: uma sala muito difícil deve recompensar com no mínimo uma carta ou upgrade de carta, em contrapartida, uma sala mais simples deve recompensar o jogador com itens consumíveis.

## 3.2 Mecânicas Principais

A parte das mecânicas de movimentação, o jogo contará com um elemento de gameplay chamado de Card Time, que reduzira a velocidade do jogo a 10%, e permitirá o jogador a usar “cartas-habilidade” para acumular efeitos na área de alcance da habilidade.

Exemplo da interface:

### 3.2.1 Física do Jogo

A física do jogo usará o básico fornecido pelo motor da Unity, e especificará a movimentação manualmente baseado em aceleração.

#### 3.2.1.1 Hibox e Hurtbox

A fim de promover um loop de gameplay balanceado, as hitboxes e hurtboxes do jogo devem ser feitar para que o jogador possua uma certa margem de erro, exemplo: a colisão principal de cenário do jogador deve ser apenas 80% da área real que o sprite preenche no mundo do jogo.

Hurtboxes deveram ser avaliadas caso a caso, mas para o jogador, ela possivelmente será do mesmo tamanho que o colisor conta ambiente, porém alguns inimigos certamente terão hurtboxes maiores do que aparentam, para dar essa margem de erro as hitboxes oriundas do personagem (como o ataque básico)

### 3.2.2 Movimentação do Personagem

Movimentação tradicional de 2 eixos, permitindo pulos, pulos duplos e escalada. O personagem acumulará momentum quando aplicável com terrenos ou outros elementos a fins de criar um dinamismo maior na travessia. Também poderá manter o momentum enquanto atinge alvos válidos enquanto no ar, permitindo sequencias de plataformas mais complexas e inimigos-chefes, incentivando o jogador a manter-se no ar o máximo de tempo possível.

### 3.2.3 Habilidades

As habilidades do jogo podem ser divididas em três grupos: Ativas, Passivas e Cartas! As habilidades Ativas pertencem ao conjunto esperado: pulo duplo, escalada, ‘Card Time’ e defesa. As passivas representam aquelas que impactam sem requerer ação do jogador, geralmente resultantes de manipulações de status básicos, como vida e ataque.

#### 3.2.3.1 Cartas!

As habilidades de Cartas visam ampliar as opções do jogador da forma a qual ele melhor preferir abordar o combate, seja mais estratégico ou mais brutal, o sistema de cartas deve permitir isso. Cartas propriamente ditas, terão suas subdivisões e o uso destas devem aprimorar o combate corpo a corpo, com alterações mecânicas mínimas. Para usar cartas, o jogador precisará pressionar o botão correspondente ao ‘Card Time’, e somente cartas pertencentes aquele card time específico (ou genéricas) poderão ser utilizadas.

Todas as cartas terão algum nível de ‘custo’	 para serem utilizadas, estes podem variar de consumir mais do card time atual, impedindo muitas combinações, consumindo vida ou consumindo ‘tempo’ (fazendo a carta entrar em recarga).

Cartas 	pertenceram a algum conjunto, formando Decks, que devem compartilhar uma ideia em comum. Juntamente, cartas serão segregadas em três tipos principais de efeito:

- Feitiço: tem como foco aprimorar os status do personagem por uma determinada duração
- Encantamentos: tem como foco aprimorar as habilidades do personagem por uma determinada duração
- Magias: tem como foco dar mais opções ao jogador (ex.: um projétil com explosão em área), e seja visualmente distinto.

#### 3.2.3.2 Card Time

O ‘Card Time’ refere-se à janela de tempo que o jogador poderá jogar e combinar suas cartas enquanto desfere ataques ou desvia de inimigos. Essa janela possui algumas divisões, a depender do momento em que o personagem se encontra, permitindo um aumento de possibilidades no combate.

Os momentos ‘Card Time’ são os seguintes:

- Alfa: ‘Sempre ativo’, antes do 1° golpe básico. Dura infinito se no chão, 3 segundos se no ar)
- Beta: Durante o 3° golpe básico. Dura desde o início da animação, até o ataque conectar, invalidando a carta se não conectar.
- Épsilon: Janela rápida, entre o 1° e 2° gole básico ou defesa. Dura o tempo da animação ~1 segundo.
- Ômega: Durante a janela de recuperação do 3° golpe básico (permite golpes Zênite). Duração variável, enquanto na animação de recuperação (que pode ser reduzida)
- Lambda: Após o 4° golpe (zênite). Quick Time Event condicional.

A janela de tempo dos variados card times podem ser ampliadas (ou reduzidas) com outras cartas e com habilidades passivas, permitindo um âmbito estratégico ainda maior.

#### 3.2.3.3 Cartas

Algumas das cartas que devem ser implementadas (mais a serem adicionadas):

- Sweeper (encantamento): Permite desviar ataques inimigos (dá frames de invulnerabilidade e invalida aquela hurtbox) ao acertar um ataque no ataque do inimigo, 3 tentativas por carta, reinicia ao acertar, máximo de 3 tentativas (apertar o botão) e até 9 parries em sequência (erro, erro, acerto (reinicia), acerto, erro, erro, erro, fim do efeito), sem tempo limite (a ser avaliado). Só pode ser jogada durante o 'card time': Beta, cancela se esquivar.
- Dance (encantamento): Ao lançar no Épsilon, resolve a animação do ataque imediatamente e o ataque subsequente será o 1o novamente, e aumentará levemente a velocidade de ataque (acumulável enquanto não errar), cancela ao esquivar ou pular.
- Heat Up (encantamento): Anula uma vez o cancelamento de uma carta jogada ao mesmo tempo (permitiria esquivar ou pular e continuar com os benefícios da Dance ou Sweeper)
- Frensi (feitiço): Concede buff% por x segundos enquanto um golpe for acertado a cada 1 segundo até y acúmulos. Atingir o máximo de acúmulos provê o dobro do benefício, reinicia a duração, não poderá ser acumulado por x tempo. Carta limitada.
- Berserk (feitiço): Os próximos 15 ataques consumirão 1 HP para executar, e curarão 2 HP ao acertar, se executar um próximo ataque em até 1s após o último, aumentando a velocidade de ataque em 10% por acerto. Dura 10 segundos ou até não cumprir com a condição de ativação. Cancela se o personagem estiver no ar. Carta limitada.
- Glass Cannon (feitiço): Por 5 segundos, dá 100 de DANO%, e triplica o dano recebido.
- Bullseye (magia): Enquanto não for atingido e não errar ataques, aumenta a CHANCE CRÍTICA em 50%, e o alcance dos ataques em 100% (visualmente também). Consome 1HP para cada 3 segundos. Cancela ao recuperar HP (de qualquer fonte)
- Expeditioner (magia): Comete suicídio. A sua versão de uma outra linha do tempo (após respawnar), ganhará 5 variados buffs (pequenos, aletaórios, acumuláveis e repetíveis), mas infelizmente não consiguirá voltar para casa (perde todas as moedas, e dificulta a visibilidade, e não pode usar viagem rápida), até morrer. Carta limitada a 1 por descanço (reinicia).
- Easymode (magia): Aumenta o tamanho e o alcance do ataque em 500%, reduz a velociade de ataque em 80%, 30% de chance de receber dano (corrigido) do proprio ataque, até morrer.
- Reaper (zênite - 4o golpe): Usa uma foice para se agarrar ao inimigo e atravessá-lo (se possível), dá BonusG% até pular/encostar no chão, acumulável.
- Ascendency (zênite - 4o golpe): (precisa-se estar no chão, usar essa carta no ar imediatamente joga o personagem ao chão e cancela a ação) Crava a arma no chão, e, quando o botão de ataque for pressionado (concede até 150 de DANO%, a depender do timing), avança para frente golpeando todos os inimigos uma vez a cada 0.15s (permite multiplos acertos em inimigos 'grandes') ao longo da tragetória da arma.
- Storm (Lambda): Dá 300 de DANO% e golpeia todos os inimigos no alcance da camera UMA vez, e depois golpeia aleatoriamente mais 10 vezes. Encerra todas as cartas atualmente ativas, e impede de usar cartas por um tempo.

//MAIS CARTAS.

### 3.2.4 Interação com o Ambiente

O jogo contará com algumas interações com o ambiente, como poder quebrar algumas estruturas do mapa, plataformas que que requerem um ataque aéreo para dar um benefício aéreo ao jogador (como permitir mais um pulo) e objetos e paredes que só podem ser quebradas com uma combinação específica de cartas Alfa.

### 3.2.5 Combate

O combate será baseado em:

- Ataques encadeados
- Combate aéreo
- Momentum
- Gerenciamento de cartas
- Posicionamento
- Timing

A sequência de ataques do personagem deve seguir a seguinte estrutura:

- 3~4 golpes
- Conjunto inicial: dois golpes em aro frontais, movendo o jogador levemente para frente, 3° golpe uma estocada, empurrando-o para trás. Se após o segundo golpe o personagem avançou 0.5 unidades, ao acertar o 3° golpe, durante a recuperação da animação, o personagem deve ter recuado 0.75 unidades, terminando 1/4 de unidade mais longe horizontalmente da posição inicial.
- 4° golpe possível com uma carta: durante a recuperação do 3° golpe, o jogador deve jogar uma carta específica que permite um 4° golpe, esse jogando o personagem até 1.5 (base) unidades para frente, até conectar com algo interagível por esse 4° golpe ou inimigo. Acertar um inimigo com o 4° enquanto no ar, nega a gravidade por um momento e dá um pouco de momentum vertical ao personagem, permitindo uma sequência virtualmente infinita aérea até o personagem ser atingido ou o jogador errar algum comando. Tempo de recuperação e frames de acerto a serem determinados por "feeling" durante o desenvolvimento.
- Por base, os impactos dos 1o e 2o golpe devem empurrar inimigos menores pelo mesmo tanto que o jogador é empurrado para frente. O 3° deve empurrar até mesmo inimigos um pouco maiores (escala a ser determinada). O 4o (base) não deve empurrar inimigos, já que o objetivo é manter-se agressivo no inimigo.

Sistema de dano:

Dano = ((Golpe% + BonusG%) * (Ataque * (1 + buffs%)) + dano flat) * (1 + DANO%) * CritValue

Decompõe-se como:

- Dano = A quantidade de vida a ser reduzida de uma entidade
- Golpe% = Indicador percentual de quanto um golpe representa o ataque do personagem
- BonusG% = Somatória representando aumento aditivo direto ao Golpe%, usado para consolidar que um golpe agora é mais forte, por qualquer motivo.
- Ataque = Status significando a força de ataque do personagem
- BonusATQ% = Somatória representando aumento multiplicativo do Status de ataque
- Dano Flat = Quantia extra de dano que não deve participar do ganho de status nem de Golpe
- DANO% = Somatória de multiplicador de dano final
- CritValue = Redutiva em relação a capacidade do jogador de acertar um Golpe Crítico

# 4. Level Design

O mapa será segregado em regiões, áreas e salas, sendo região um conjunto de áreas que compartilham semelhanças estéticas e lúdicas, áreas sendo as subdivisões de uma região e salas cada parte transitável de uma área.

Blocagem inicial da primeira região do jogo, contento quatro áreas e diversas salas:

## 4.1 Estrutura do Mapa

O mapa do jogo deverá ser estruturado de acordo com a capacidade de movimento e agilidade do personagem, contendo sequencias verticais e horizontais de proporções elevadas, permitindo que o jogador use de todo sua habilidade e coordenação para chegar aonde deseja.

Ao escopo do protótipo, o jogo conterá ao menos uma seção verticalizada e uma seção bastante horizontalizada, diversas seções mais estáveis (nem tão verticais, nem tão horizontais).

## 4.2 Progressão Espacial

A progressão espacial se dará pela descoberta de novas áreas e pelo uso do ability gating moderado. Áreas novas, em sua suma maioria, não devem ser acessadas antes que o jogador tenha atingido determinadas condições, entretanto, para aquisição de itens, consumíveis, moeda ou lore, será utilizado um ability gating mais incisivo, porém permitindo sequence break (que o jogador acesse tal área ou segredo antes da hora “correta”)

## 4.3 Áreas e Conexões

O jogo contará com ao menos 10 áreas distintas, em que cada área deve se ligar a uma outra de pelo menos três formas diferentes, que podem ou não ser descobertas pelo jogador.

Para o protótipo, apenas uma área d e tamanho médio será desenvolvida.

# 5. Interface e Controles

O jogo será desenvolvido e balanceado para ser jogado com um controle por preferência de design, porém deverá funcionar corretamente com mouse e teclado.

## 5.1 HUD

O HUD será dinâmico:
	Exibição fixa (estará sempre visível):

- Vida
- Energia?

Exibição dinâmica:

- Card Time
- Cartas do atual card time

## 5.2 Câmera

A câmera deve ser dinâmica, acompanhando o jogador dentro dos limites do cenário, porém deve ser capaz de criar ênfase em determinados cenários, áreas ou ações, quando necessário

## 5.3 Controles

Esquema padrão (editável)

Esquema do card time (editável talvez):

# 6. Projeto Técnico

## 6.1 Plataforma-alvo

O projeto está incialmente organizado para computadores.

## 6.2 Ferramentas e Engine

Será desenvolvido usando a Unity Engine 6, com suporte artístico de softwares como Paint.net, Blender e Aseprite.

## 6.3 Requisitos básicos

O projeto deve ser otimizado para hardwares mais simples, aumentando significativamente a amostragem possível de jogadores.

# 7. Considerações de Design

## 7.1 Relação Habilidade do Jogador x Habilidade do Personagem

Parte crucial do protótipo e pivô central de desenvolvimento. Toda e qualquer mecânica, interação, progressão e ação deve priorizar a capacidade do jogador de aprender com o jogo e executar com exímio as ações que lhe é solicitada, nunca permitindo que só seja possível atingir um objetivo, que tenha em seu requerimento o uso de habilidades do personagem, de forma sintética, o jogo deve promover o desafio em todas as circunstâncias de forma que, um jogador bem alinhado com as mecânicas e com a disposição de habilidades (do personagem) em determinado momento seja capaz de superar os desafios mais rapidamente. Compromissos poderão ser tomados a fim de priorizar que o jogador tenha agência sobre a execução de determinada ação.

Um exemplo bom a ser usado como régua é o jogador ser capaz de com muita rapidez atravessar uma área e eliminar os inimigos necessários, sem perder o momentum do movimento ou sequer ser atingido, ao mesmo tempo que seria possível a conclusão deste mesmo desafio de forma mais analítica e espaçada.

## 7.2 Curva de Aprendizado

A curva de aprendizado é bastante íngreme. Dividindo a escala de aprendizado do jogador de 0 a 10, será necessário cerca de 6 para conseguir iniciar a progressão efetiva do jogo, levando o jogador a ter uma taxa de falhas elevada que tende a reduzir gradativamente conforme ele se acostuma com o jogo.

## 7.3 Dificuldade e Balanceamento

A dificuldade do jogo deve estar atrelada principalmente a capacidade motora e analítica do jogador, de usar as ferramentas a sua disposição da forma mais efetiva e pragmática possível.

O balanceamento será bastante rígido, e a percepção do balanceamento estará ligada também a capacidade analítica do jogado, as Cartas e outros aprimoramentos devem servir para abrir portas para que o jogador tenha a liberdade de usar mais das mecânicas elementares, ao invés apenas de promover benefícios numéricos (como aumento de status). Estes por sua vez, devem ser balanceados para que um excelente jogador consiga “quebrar o jogo”, tornando desafios em situações que aparentam triviais, porém requerem alta precisão e conhecimento.

Um fator importante atrelado ao balanceamento está presente no núcleo do jogo: Card Time. As janelas de tempo serão minuciosamente controladas com pouca expansibilidade a fim de limitar o descontrole de possibilidades.

Cartas devem ser balanceadas para permitir um aumento na performance do personagem, de forma que se usadas adequadamente em cadeia pelo jogador, culmine em uma efetividade maior.

# 8. Equipe

Gabriel Onzi Benachio: Game Desiner, Artista e Desenvolvedor.

## 🔴 Problemas Críticos (Alto Risco)

### 1. Sistema de Cartas + Combos (AA String)

#### Problemas:

- Overload de janelas (Card Time):
  - Você tem 5 janelas diferentes (Alfa, Épsilon, Beta, Ômega, Lambda) com regras distintas. Isso pode:
    - Confundir o jogador (muitos estados para memorizar).
    - Dificultar o balanceamento: Cartas como Dance (que reinicia o combo) ou Heat Up (que anula cancelamentos) podem quebrar a fluidez se mal ajustadas.
    - Bugs de sincronismo: Se o jogador usar uma carta no Card Time Beta que afete o Épsilon, como o jogo vai gerenciar a ordem de execução? (Ex.: Heat Up + Dance no mesmo frame).
- Combo infinito no ar:
  - A mecânica de negar gravidade ao acertar o 4º golpe no ar pode:
    - Quebrar o jogo: Se o jogador tiver precisão perfeita, pode ficar no ar indefinidamente, ignorando mecânicas de plataforma.
    - Problemas de hitbox/hurtbox: Como o jogo vai lidar com colisões se o personagem está em "gravidade zero"?
    - Balanceamento: Inimigos aéreos ou chefes podem se tornar impossíveis ou triviais dependendo da implementação.
- Custo de cartas x Recuperação:
  - Você menciona que cartas têm custo variável e podem ser recuperadas durante a gameplay, mas não define:
    - Como a recuperação funciona (tempo? acertos?).
    - Limites: Se o jogador puder spammar cartas como Glass Cannon ou Berserk, o jogo pode virar um button masher.

#### Sugestões:

✅ Reduzir janelas de Card Time:

- Unificar Alfa e Beta em uma única janela pré-ataque.
- Ômega e Lambda podem ser fundidas em uma janela de "fim de combo" (ex.: Finisher Time).
- Teste de usabilidade: Se jogadores não conseguem usar as janelas corretamente, o sistema está complexo demais.

✅ Limitar combos aéreos:

- Adicionar um contador de momentum (ex.: máximo de 3 negativas de gravidade seguidas).
- Dano reduzido em combos aéreos longos (ex.: após 5 acertos no ar, dano -50%).

✅ Definir regras claras para recuperação de cartas:

- Exemplo:
  - Cartas comuns: Recuperam 1 por segundo (máximo de 5 no deck).
  - Cartas limitadas (ex.: Storm): 1 por descanço (checkpoint).
  - Cartas últimas (ex.: Expeditioner): 1 por morte.

### 2. Fórmula de Dano

#### Problemas:

- Complexidade matemática:
  - Dano = ((Golpe% + buffMV%) * (Ataque * (1 + buffs%)) + (dano flat)) * (1 + DANO%) * CritValue
  - Risco de overflow: Se DANO% ou CritValue forem muito altos (ex.: Glass Cannon + Bullseye), o dano pode ultrapassar limites de variáveis (em C#, int ou float podem estourar).
  - Difícil de balancear: Buffs multiplicativos (1 + DANO%) podem escalar exponencialmente (ex.: 3 cartas de +100% DANO% = 8x dano base).

#### Sugestões:

✅ Simplificar a fórmula:

- Usar soma de buffs aditivos (ex.: Dano = (Ataque + buffsFlat) * (1 + buffs%) * CritValue).
- Limitar multiplicadores: Capar DANO% em +300% (para evitar one-shot em chefes).

✅ Testar casos extremos:

- O que acontece se o jogador usar Glass Cannon (+100% dano) + Bullseye (+50% chance crítica) + Storm (+300% dano)?
- Solução: Adicionar diminuição de retornos (ex.: cada buff multiplicativo após o 2º tem efeito reduzido).

### 3. Movimentação e Física

#### Problemas:

- Momentum e anulação:
  - Você menciona que o jogador pode anular momentum, mas não define:
    - Como (botão? carta?).
    - Custo (se houver).
  - Risco de jank: Se o jogador puder parar instantaneamente no ar, a física pode ficar pouco intuitiva.
- Tamanho do personagem (1 unidade = 64px²):
  - Colisões com hitboxes complexas: Se inimigos ou hazards tiverem hitboxes menores que 1 unidade, pode haver problemas de detecção de colisão (ex.: o jogador passa "por dentro" de um inimigo).
  - Câmera (21 unidades horizontais):
    - Em PixelArt, 21 unidades = 1344px (21 * 64). Isso pode ser muito largo para telas pequenas (ex.: mobile) ou pouco detalhado em telas grandes (ex.: 4K).

#### Sugestões:

✅ Definir regras claras para momentum:

- Exemplo:
  - Anulação: Apenas com uma carta específica (ex.: Brake, custo alto).
  - Fricção no ar: Reduzir velocidade gradualmente (ex.: -10% por frame).

✅ Ajustar escala da câmera:

- Testar em diferentes resoluções:
  - Se 21 unidades forem muito, reduzir para 16-18 unidades.
  - Adicionar zoom dinâmico (ex.: câmera mais próxima em áreas fechadas).

✅ Hitboxes:

- Usar grid de colisão (ex.: dividir o personagem em 4 hitboxes menores para precisão).
- Hurtboxes: Tornar 10% menores que as hitboxes para evitar cheap hits.

## 🟡 Problemas Moderados (Médio Risco)

### 1. Sistema de Vida (Hits)

- Problema: Vida baseada em hits (ex.: 5 hits = morte) pode:
  - Tornar o jogo muito difícil se o jogador não tiver feedback claro de quanto dano está levando.
  - Dificultar balanceamento de chefes (ex.: um chefe com 20 hits pode ser tedioso se cada ataque do jogador der apenas 1 hit).
- Sugestão:

✅ Adicionar barra de vida visual (mesmo que seja em hits).

✅ Dano em números: Mostrar o valor de cada hit (ex.: "3/5 HP").

### 2. Cartas Limitadas e "Descanço"

- Problema: O conceito de descanço (recuperação de cartas) não está claro:
  - É um cooldown global? Um sistema de energia?
  - Como o jogador sabe quando pode usar uma carta limitada novamente?
- Sugestão:

✅ Definir "descanço" como um cooldown por carta:

  - Exemplo: Storm = 1 uso a cada 30 segundos.

✅ Feedback visual: Ícone da carta escurecido + temporizador.

### 3. Cartas com Efeitos Complexos

- Problemas:
  - Expeditioner: Suicídio + buffs aleatórios pode:
    - Frustrar jogadores (perder progresso sem garantia de recompensa).
    - Quebrar o jogo: Se os buffs forem muito fortes (ex.: invulnerabilidade), o jogador pode abusar da mecânica.
  - Easymode: +500% alcance pode:
    - Tornar o jogo trivial (ataques cobrindo a tela toda).
    - Bugs de hitbox: Como o jogo vai detectar colisões com um ataque tão grande?
  - Berserk: Consumir HP para atacar pode:
    - Matar o jogador sem aviso (se ele não perceber que está perdendo vida).
    - Sinergia quebrada: Com Glass Cannon (triplica dano recebido), o jogador pode morrer em 1 hit.
- Sugestões:

✅ Limitar aleatoriedade:

  - Expeditioner: Buffs pré-definidos (ex.: +20% velocidade, +15% dano, etc.).

✅ Ajustar Easymode:

  - Reduzir para +200% alcance e adicionar penalidade de movimento (ex.: -50% velocidade).

✅ Feedback claro para Berserk:

  - Efeito visual (ex.: personagem fica vermelho).
  - Aviso sonoro ao perder HP.

### 4. Inimigos e Hazards

- Problema:
  - Hazards com hitbox reaproveitável podem:
    - Ficar invisíveis se a hitbox for menor que o sprite.
    - Causar frustração se o jogador não entender por que está levando dano.
- Sugestão:

✅ Mostrar hitboxes em modo debug (para testadores).

✅ Adicionar tells visuais (ex.: hazard pisca antes de atacar).

## 🟢 Problemas Menores (Baixo Risco, mas Importantes)

| Item | Problema | Sugestão |
| --- | --- | --- |
| Pulo Duplo | Se for uma passiva, pode desbalancear a mobilidade. | Tornar uma carta (custo: 1 energia por uso). |
| Defesa Ativa | "Defesa perfeita" pode ser difícil de executar em um jogo rápido. | Adicionar janela de parry (ex.: 0.2s antes do hit). |
| Cartas de 4º Golpe | Reaper e Ascendency têm regras muito específicas (ex.: Ascendency cancela se usado no ar). | Unificar regras (ex.: todos os 4º golpes requerem estar no chão). |
| Partículas | Uso excessivo pode reduzir performance em dispositivos fracos. | Limitar número de partículas por frame (ex.: máximo de 50). |
| Cenários 3D | Misturar 2D e 3D pode causar clipping (objetos 3D aparecendo na frente do 2D). | Usar camadas de profundidade (sorting layers no Unity). |
| Animações por Rigging | Rigging em 2D pode aumentar o tempo de desenvolvimento. | Testar com 1 personagem primeiro antes de aplicar a todos. |

## 📌 Recomendações Gerais

### 1. Prototipação Rápida

- Priorize implementar:
  - Sistema de combo básico (3 golpes + 4º opcional).
  - 2-3 cartas simples (ex.: Sweeper, Dance, Glass Cannon).
  - Movimentação + física (pulo, dash, momentum).
- Objetivo: Testar se a fluidez e o feeling estão bons antes de adicionar complexidade.

### 2. Balanceamento Iterativo

- Ferramentas úteis:
  - Planilha de dano: Calcular o dano médio por combo com diferentes cartas.
  - Testes com jogadores: Observar se:
    - As janelas de Card Time são intuitivas.
    - Os combos aéreos são divertidos ou frustrantes.
    - As cartas limitadas são justas.

### 3. Feedback Visual e Sonoro

- Adicionar:
  - Efeitos de luz ao usar cartas (ex.: Glass Cannon = brilho vermelho).
  - Sons distintos para cada Card Time (ex.: som de tick para Épsilon).
  - Vibração do controle (se aplicável) em parries ou hits críticos.

### 4. Documentação para a Equipe

- Clarificar:
  - Regras de colisão (ex.: hitbox do 4º golpe é maior?).
  - Prioridades de execução (ex.: se o jogador usar Dance e Heat Up no mesmo frame, qual tem precedência?).
  - Valores numéricos (ex.: quanto é buffMV% em Reaper?).

## 🎯 Resumo de Ações Prioritárias

| Ação | Por que? | Como? |
| --- | --- | --- |
| Simplificar Card Time | Reduzir complexidade para o jogador. | Unificar janelas (ex.: Alfa + Beta = "Pre-Attack Time"). |
| Limitar combos aéreos | Evitar soft locks (jogador preso no ar). | Adicionar contador de momentum ou dano reduzido. |
| Ajustar fórmula de dano | Evitar overflow e balanceamento quebrado. | Usar buffs aditivos e capar multiplicadores. |
| Definir "descanço" | Clarificar como cartas limitadas são recuperadas. | Usar cooldowns por carta + feedback visual. |
| Testar hitboxes/hurtboxes | Evitar bugs de colisão. | Usar grid de colisão e mostrar hitboxes em debug. |
| Prototipar combo básico | Validar se a mecânica é divertida. | Implementar 3 golpes + 1 carta de 4º golpe. |

Tiago, acho que esses são os principais pontos de atenção. O seu design tem potencial enorme, mas precisa de ajustes para evitar frustração ou bugs. O que você acha? Quer que eu detalhare algo específico ou sugira alternativas para alguma mecânica?

# 📜 Game Design Document (GDD) – Metroidvania 2D com Cartas (Parte 2)

Versão 1.0 – Organizado em 16/05/2026

Continução do documento principal

## 4. Level Design

### 4.1 Estrutura do Mapa

- Hierarquia: Região > Áreas > Salas
  - Região: Conjunto de áreas com semelhanças estéticas e lúdicas (ex.: Castelo Principal, Floresta Corrompida)
  - Área: Subdivisão de uma região (ex.: Sala do Trono, Masmorras)
  - Sala: Parte transitável de uma área (ex.: Sala com plataformas móveis)
- Design:
  - Mapa estruturado para movimentação ágil (sequências verticais e horizontais longas)
  - Protótipo:
    - 1 seção verticalizada (ex.: torre)
    - 1 seção horizontalizada (ex.: corredor longo)
    - Várias seções estáveis (equilíbrio entre vertical/horizontal)

### 4.2 Progressão Espacial

- Ability Gating Moderado:
  - Áreas novas só acessíveis após desbloqueio de habilidades (ex.: pulo duplo para alcançar plataformas altas)
- Itens/Secretos:
  - Ability gating mais incisivo (ex.: carta específica para quebrar parede)
  - Permite sequence break (jogador acessar áreas antes da "hora correta")

### 4.3 Áreas e Conexões

- Jogo Completo:
  - Mínimo 10 áreas distintas (cada uma com identidade visual e mecânica única)
- Conexões Múltiplas:
  - Cada área deve se conectar a pelo menos 3 outras de formas diferentes (ex.: porta, túnel secreto, queda livre)
  - Conexões ocultas (para recompensar exploração)
- Protótipo:
  - 1 área de tamanho médio (com todas as mecânicas nucleares)

## 5. Interface e Controles

### 5.1 HUD (Heads-Up Display)

Princípio: Dinâmico (exibe apenas o necessário no momento)

| Elemento | Tipo | Detalhes | Posição Sugerida |
| --- | --- | --- | --- |
| Vida | Fixo | Barra de vida (ou contagem de hits se preferir estilo Soulslike) | Canto superior esquerdo |
| Energia | Fixo (opcional) | Barra de energia (se implementada) | Canto superior esquerdo |
| Card Time | Dinâmico | Indica a janela ativa (Alfa, Beta, Épsilon, Ômega, Lambda) | Centro superior |
| Cartas Ativas | Dinâmico | Exibe as cartas disponíveis para o Card Time atual | Centro inferior |
| Buffs/Debuffs | Dinâmico | Ícones de efeitos ativos (ex.: Frensi, Glass Cannon) | Lateral direito |
| Moedas/Itens | Fixo | Contador de moedas e itens consumíveis | Canto superior direito |

### 5.2 Câmera

- Tipo: Dinâmica (acompanha o jogador)
- Limites:
  - Horizontais: 21 unidades (1344px em pixel art 64x64)
  - Verticais: Ajustados para evitar clipping com o cenário
- Ênfase:
  - Câmera pode se afastar/aproximar para destacar:
    - Cenários importantes (ex.: chefe aparecendo)
    - Ações do jogador (ex.: ataque Zênite)
  - Zoom dinâmico em áreas fechadas

### 5.3 Controles

#### Esquema Padrão (Editável)

| Ação | Teclado | Controle (Xbox/PS) | Notas |
| --- | --- | --- | --- |
| Movimentação | WASD / Setas | Left Stick | - |
| Pular | Espaço / W | A / X | - |
| Ataque Básico | Mouse Esquerdo / J | RT / R2 | - |
| Defesa | Mouse Direito / K | RB / R1 | - |
| Card Time | Shift / L | LT / L2 | Ativa janela para usar cartas |
| Selecionar Carta | 1-4 (Números) | D-Pad | Seleciona carta no deck atual |
| Esquivar | Q / Ctrl | B / Circle | - |
| Interagir | E | Y / Triangle | Portas, objetos, NPCs |
| Menu/Pausa | ESC | Start / Options | - |

#### Esquema Alternativo (Mouse + Teclado)

- Card Time: Botão do meio do mouse
- Seleção de Cartas: Roda do mouse (cicla entre cartas)

## 6. Projeto Técnico

### 6.1 Plataforma-Alvo

- Principal: PC (Windows, Linux, Mac)
- Possível Expansão: Console (após protótipo estável)

### 6.2 Ferramentas e Engine

| Categoria | Ferramenta | Uso |
| --- | --- | --- |
| Engine | Unity 6 | Desenvolvimento do jogo (2D) |
| Arte Pixel | Aseprite | Criação de sprites e animações |
| Arte 3D | Blender | Modelagem de assets 3D (se necessário, ex.: partículas) |
| Edição de Imagem | Paint.NET / Photoshop | Ajustes finos em sprites e UI |
| Áudio | BFXR / Audacity | Criação de sound effects e trilha sonora |
| Versionamento | Git (GitHub/GitLab) | Controle de versão do código |

### 6.3 Requisitos Básicos

- Hardware Mínimo (PC):
  - Processador: Dual Core 2.0 GHz
  - Memória RAM: 4 GB
  - Placa de Vídeo: Integrada (Intel HD 4000+) ou Dedica (GeForce GTX 650+)
  - Armazenamento: 500 MB (prototipo) / 2 GB (jogo completo)
- Otimização:
  - Foco em hardwares simples para ampliar a base de jogadores
  - Limites:
    - Máximo de 50 partículas por frame (evitar lag em PCs fracos)
    - Texturas em resolução baixa (para pixel art)

## 7. Considerações de Design

### 7.1 Relação: Habilidade do Jogador x Habilidade do Personagem

Princípio Central:

"O jogo deve desafiar o jogador, não o personagem."

Regras:

1. Agência do Jogador:
  a. Toda mecânica deve priorizar a capacidade do jogador de aprender e executar ações com precisão
  b. Nunca bloquear progressão somente por falta de status do personagem (ex.: vida baixa)
2. Desafio Justo:
  c. Um jogador bem alinhado com as mecânicas deve ser capaz de:
    i. Superar desafios rapidamente (ex.: speedrun)
    ii. Usar habilidades de forma criativa (ex.: sequence breaks)
3. Compromissos:
  d. Priorizar agência sobre randomness (ex.: dano crítico deve ser controlável)
  e. Evitar pay-to-win: Itens ou cartas não devem substituir skill

Exemplo Prático:

- Bom Design:
  - Jogador pode atravessar uma área rapidamente sem perder momentum ou ser atingido
  - Mesmo desafio pode ser superado de forma analítica (lenta e estratégica) ou ágil (rápida e precisa)
- Mau Design:
  - Área que obriga o uso de uma carta específica (ex.: só passa com Storm)
  - Inimigo que só morre com combo de 10 golpes (tedioso)

### 7.2 Curva de Aprendizado

- Dificuldade: Íngreme (para público hardcore)
- Escala de Aprendizado (0-10):
  - 0-5: Jogador aprende as mecânicas básicas (movimentação, combo, cartas simples)
  - 6: Jogador consegue iniciar progressão efetiva (superar primeiros desafios)
  - 7-10: Jogador domina mecânicas avançadas (combos aéreos, sequence breaks, sinergias de cartas)
- Taxa de Falhas:
  - Alta no início (jogador morre com frequência)
  - Reduz gradativamente conforme o jogador se acostuma com o jogo

Estratégias para Suavizar a Curva:

- Tutoriais Imersivos:
  - Não usar textos longos (ensinar via gameplay)
  - Exemplo: Sala com inimigos fracos para praticar parry (Sweeper)
- Checkpoints Frequentes:
  - Evitar frustração com perda de progresso excessiva
- Feedback Imediato:
  - Sons e efeitos visuais para confirmar ações (ex.: som de "clique" ao usar carta no Card Time correto)

### 7.3 Dificuldade e Balanceamento

Princípios:

1. Dificuldade = Skill do Jogador:
  a. O jogo deve ser difícil por exigir precisão e conhecimento, não por:
    i. Dano alto demais (ex.: inimigos matam em 1 hit sem aviso)
    ii. Hitboxes injustas (ex.: ataques que acertam "por trás")
2. Balanceamento Rígido:
  b. Cartas e Habilidades:
    iii. Devem abrir possibilidades mecânicas, não apenas aumentar números (ex.: +10% de dano é menos interessante que novas opções de combo)
  c. Exceção: Buffs numéricos devem ser balanceados para não quebrar o jogo (ex.: Glass Cannon deve ter custo alto)
3. Meta de Balanceamento:
  d. Um jogador excelente deve conseguir:
    iv. "Quebrar o jogo" (tornar desafios triviais com skill e sinergias)
    v. Superar chefes sem levar dano (com prática)

Fatores Críticos:

- Card Time:
  - Janelas minuciosamente controladas (pouca expansibilidade para evitar descontrole)
  - Exemplo: Beta (3° golpe) deve ser curto o suficiente para exigir timing, mas não tão curto a ponto de ser impossível
- Cartas:
  - Sinergias testadas (ex.: Berserk + Glass Cannon não deve ser overpowered)
  - Custos proporcionais ao poder (ex.: Storm deve ter recarga longa)

## 8. Equipe

| Nome | Função | Responsabilidades |
| --- | --- | --- |
| Gabriel Onzi Benachio | Game Designer, Artista, Desenvolvedor | Design de mecânicas, pixel art, programação (Unity), balanceamento |
| Tiago Ficagna | Organizador do GDD | Estruturação do documento, revisão de mecânicas, sugestões de balanceamento |

Notas:

- Equipe Pequena: Foco em prototipação rápida e validação de mecânicas antes de escalar
- Colaboração: Uso de Git para versionamento e Trello/Notion para gestão de tarefas

## 9. 🔴 Apêndice: Análise Crítica e Soluções

Baseado nos problemas identificados no documento original

### 🔴 Problemas Críticos (Alto Risco)

#### 1. Sistema de Cartas + Combos (AA String)

Problemas:

| Issue | Risco | Impacto |
| --- | --- | --- |
| Overload de Janelas (Card Time) | 5 janelas (Alfa, Beta, Épsilon, Ômega, Lambda) com regras distintas | Confunde jogador, dificulta balanceamento, bugs de sincronismo |
| Combo Infinito no Ar | Negar gravidade com 4° golpe permite sequências aéreas infinitas | Quebra o jogo (jogador ignora platforming), problemas de hitbox |
| Custo de Cartas x Recuperação | Regras de recuperação não definidas | Spam de cartas (ex.: Glass Cannon repetidamente) |

Soluções Propostas:

| Ação | Como Implementar | Benefício |
| --- | --- | --- |
| Reduzir Janelas de Card Time | Unificar Alfa + Beta = "Pre-Attack Time |  |
