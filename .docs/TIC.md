# Design de Jogos e Entretenimento Digital

UNIVERSIDADE DO VALE DO ITAJAÍ

CENTRO DE CIÊNCIAS SOCIAIS APLICADAS - COMUNICAÇÃO TURISMO E LAZER

CURSO DE DESIGN DE JOGOS E ENTRETENIMENTO DIGITAL

Gabriel Onzi Benachio

RELATÓRIO DO TIC - 6º PERÍODO

NOME DO JOGO

Balneário Camboriú (SC), Abril de 2026

Gabriel Onzi Benachio

RELATÓRIO DO TIC - 6º PERÍODO

Relatório técnico apresentado como requisito parcial para obtenção de aprovação na disciplina Trabalho de Iniciação Científica, no Curso de Design de Jogos e Entretenimento Digital, na Universidade do Vale do Itajaí.

Orientado por Tiago Ficagna.

Balneário Camboriú (SC), abril de 2026

Resumo

Esta pesquisa tem como foco compreender a construção e ornamentação de jogos digitais do gênero metroidvania, explorando história do gênero, os elementos que o compõe e quais as aplicações técnicas e práticas das teorias de game design podem ser encontradas na filosofia por trás desse gênero, bem como discutir o balanço da dificuldade mecânica e cognitiva, comumente presente nos jogos do gênero.

Índice de Ilustrações

Número da figura – Descrição da figura .................................Número da página

Sumário

1 Introdução	7

1.1 Ontologia e Classificação Taxonômica do Subgênero	8

1.2 O Conceito de Não Linearidade Guiada	9

2 História do gênero Metroidvania	10

2.1 Os Precursores e a Terceira Geração	10

2.2 A Era de Ouro: Super Metroid e Symphony of the Night	10

3 Habilidade do personagem X Habilidade do jogador	10

4 Mercado de Jogos Metroidvania	13

4.1 Alcance	14

4.3 Público	16

5 Análise de jogos do gênero	16

5.1 Hollow Knight: Silksong	17

5.1.1 Contexto	17

5.1.2 História	17

5.1.3 Progressão	17

5.1.3.1 Habilidades	17

5.1.3.2 Espacialidade	18

5.1.4 Conclusão	19

5.2 Nine Sols	19

5.2.1 Contexto	19

5.2.2 História	20

5.2.3 Progressão	20

5.2.3.1 Habilidades	20

5.2.3.2 Espacialidade	21

5.2.4 Conclusão	22

5.3 MIO: Memories in Orbit	22

5.3.1 Contexto	22

5.3.2 História	22

5.3.3 Progressão	23

5.3.3.1 Habilidades	23

5.3.3.2 Espacialidade	24

5.3.4 Conclusão	25

5.4 ENDER LILIES: Quietus of the Knights	26

5.4.1 Contexto	26

5.4.2 História	26

5.4.3 Progressão	27

5.4.3.1 Habilidades	27

5.4.3.2 Espacialidade	27

5.4.4 Conclusão	28

5.5 Ori and the Will of the Wisps	28

5.5.1 Contexto	29

5.5.2 História	29

5.5.3 Progressão	29

5.5.3.1 Habilidades	30

5.5.3.2 Espacialidade	30

5.5.4 Conclusão	31

6 Criação de um Metroidvania	31

6.1 Definição de História e Escopo	31

6.2 Definição do cunho artístico	32

6.3 Definição da tematização e trilha sonora	32

6.4 Escolha das tecnologias adequadas	32

6.4.1 Gestão de Memória e Carregamento Dinâmico de Cenas.	33

6.4.2 Algoritmos de Pathfinding para Ambientes de Plataforma 2D.	33

6.5 Metodologias de Espacialidade	33

6.5.1 Level Design	34

6.5.2 World Building	34

6.5.3 Conciliação	34

6.6 Execução	34

6.6.1 Prototipagem Rápida e Greyboxing.	34

6.6.2 Análise de Feedback e Iteração do Game Feel.	35

7 Conclusão	35

8 Bibliografias:	36

# 1 Introdução

A análise acadêmica dos videojogos contemporâneos identifica o Metroidvania como um framework sofisticado de design que articula espaço, mecânica e narrativa de forma indissociável. Historicamente, o termo emerge como um portmanteau das franquias Metroid e Castlevania, servindo como uma taxonomia para jogos de ação e aventura centrados na exploração não linear e na progressão guiada por habilidades (BYCER, 2025; RODRÍGUEZ et al., 2020). No contexto da pesquisa científica brasileira, o gênero é frequentemente estudado através da lente da "Search Action", enfatizando a agência do jogador como motor da experiência lúdica (MENDES, 2019; PRADO; LAZARINI; FÁVARO, 2020).

Diferente de outros gêneros de plataforma, onde a progressão é segmentada por níveis discretos, o Metroidvania propõe uma topologia contínua. O acesso a novas áreas é condicionado pela aquisição de ferramentas que expandem o vocabulário de ações do avatar (RODRÍGUEZ et al., 2020; PRADO; LAZARINI; FÁVARO, 2020). Esta relação dialética entre o ambiente e o conjunto de habilidades do jogador cria um ciclo de feedback onde a exploração gera capacidade, e a capacidade, por sua vez, permite uma exploração mais profunda (ANDRIANO; CARACCIOLO, 2025).

## 1.1 Ontologia e Classificação Taxonômica do Subgênero

A definição científica de um gênero de jogo exige a identificação de padrões recorrentes de mecânicas e dinâmicas que moldam a experiência estética no modelo MDA — Mechanics, Dynamics, Aesthetics (HUNICKE; LEBLANC; ZUBEK, 2004). No caso do Metroidvania, a ontologia é definida pela não linearidade guiada, um conceito que descreve a tensão entre a liberdade exploratória oferecida ao jogador e a estrutura de "portões" (gates) estabelecida pelo designer (PRADO; LAZARINI; FÁVARO, 2020).

A literatura acadêmica identifica pilares fundamentais que compõem a estrutura central do gênero:

| Componente Sistêmico | Definição Acadêmica | Função Lúdica |
| --- | --- | --- |
| Mapa Interconectado | Estrutura contígua de salas e biomas interligados sem divisões arbitrárias (PRADO et al., 2020). | Facilita a navegação espacial e a memorização de obstáculos. |
| Progressão por Habilidades | Sistema de gating onde obstáculos são superados por novas competências mecânicas (RODRÍGUEZ et al., 2020). | Transforma o avatar em uma "chave" viva para o mundo. |
| Backtracking | Retorno a áreas visitadas anteriormente com novas ferramentas (ANDRIANO; CARACCIOLO, 2022). | Recompensa a percepção do jogador e expande a utilidade do mapa. |
| Mão Invisível | Uso de design de luz e arquitetura para guiar o jogador sem marcadores explícitos (PRADO et al., 2020). | Preserva a agência enquanto evita a desorientação excessiva. |

A interdependência destes componentes sugere que o gênero é uma investigação sobre a maestria do espaço. O jogador inicia em um estado de vulnerabilidade e limitação espacial, e através do processo de exploração, converte o desconhecido em território dominado. Esta transição não é apenas estatística, mas cognitiva e motora (ANDRIANO; CARACCIOLO, 2022).

## 1.2 O Conceito de Não Linearidade Guiada

A "não linearidade guiada" é um dos aspectos mais sofisticados do design de Metroidvanias. Segundo estudos publicados no SBGames, esta técnica permite que o jogador experimente a ilusão de liberdade total enquanto é sutilmente direcionado para uma ordem de progressão específica (PRADO; LAZARINI; FÁVARO, 2020). Isso evita o fenômeno da paralisia de escolha ou da frustração por desorientação, mantendo o jogador no "canal de fluxo" (PADUANO, 2025).

A eficácia deste guia invisível depende da criação de "nós mentais": o jogador encontra uma barreira insuperável, o que gera uma interrogação interna. Quando o jogador adquire a habilidade correspondente, ocorre uma epifania lúdica onde o conhecimento do mapa e a nova mecânica se fundem, motivando o retorno à área bloqueada (PRADO; LAZARINI; FÁVARO, 2020).

# 2 História do gênero Metroidvania

A trajetória histórica do Metroidvania é marcada por uma evolução contínua da complexidade espacial e narrativa. Embora o gênero tenha sido nomeado a partir de sucessos dos anos 90, suas fundações foram estabelecidas na terceira geração de consoles (MENDES, 2019).

## 2.1 Os Precursores e a Terceira Geração

Metroid (1986) é amplamente reconhecido como o catalisador do gênero. Sua inovação consistiu em aplicar a lógica de exploração em um plano de plataforma side-scrolling, substituindo itens consumíveis por melhorias permanentes de mobilidade (MENDES, 2019). Contudo, a pesquisa histórica aponta para títulos contemporâneos como Dragon Slayer II: Xanadu (1985) como exemplos seminais de RPGs de ação que já integravam mapas complexos e necessidade de ferramentas específicas para avanço (MENDES, 2019).

A franquia Castlevania seguiu um caminho divergente até a década de 90. Inicialmente linear, a série experimentou com estruturas de aventura em Castlevania II: Simon's Quest (1987), mas retornou à linearidade nos títulos subsequentes até a intervenção de Koji Igarashi (MENDES, 2019).

## 2.2 A Era de Ouro: Super Metroid e Symphony of the Night

A maturação ocorreu na década de 90. Super Metroid (1994) refinou a densidade narrativa ambiental e o design de níveis que permitia a descoberta orgânica de técnicas avançadas (MENDES, 2019). Em 1997, Castlevania: Symphony of the Night introduziu elementos de RPG e sistemas de experiência em um castelo interconectado (MENDES, 2019; PRADO; LAZARINI; FÁVARO, 2020).

Em 1997, Castlevania: Symphony of the Night introduziu elementos profundos de RPG, como sistemas de experiência e equipamentos variados, em uma estrutura de castelo interconectado (MENDES, 2019; PRADO; LAZARINI; FÁVARO, 2020). A integração de sistemas de progressão baseados em estatísticas (stat-based) com o bloqueio por habilidades (ability-gating) definiu o padrão para as décadas seguintes.

# 3 Habilidade do personagem X Habilidade do jogador

O que tornam jogos divertidos é a relação da intrínseca capacidade humana de aprender e reconhecer padrões com o contínuo fornecimento de estímulos e quebra-cabeças provenientes de um ciclo de jogabilidade, agnóstico a gênero e tema (KOSTER, 2013).

A Lente do Desafio define que é possível arquitetar uma experiência engajante e divertida através da continua apresentação de desafios, sejam mecânicos ou lúdicos, é comumente aplicada em jogos de ação e exploração, como metroidvanias, de forma que a superação dum desafio traga uma recompensa a altura (SCHELL, 2008), com foco em complementar a experiência do jogador, sejam novas habilidades ou ferramentas, novas regiões no mapa, revelações atreladas a jornada ou história, dentre outras (RODRÍGUEZ et al., 2020).

Ao analisar jogos Metroidvania através da Lente do Desafio em conjunto com as idealizações de Koster, pode-se compreender que a existência de desafios bem arquitetados em seus respectivos contextos, juntamente com as justas consequências cognitivas dos memos: o aprendizado, domínio das mecânicas e reconhecimento de padrões, cria-se um fluxo divertido e engajante.

Metroidvanias possuem uma tendência de apresentar desafios contínuos durante toda as etapas da progressão do jogo, seja espacial, lúdica ou iterativa. Como consequência, a travessia pelas diversas regiões do jogo costuma se mostrar “agressiva” para o jogador, com desafios de platforming, que pode ser definido como a rota resultante da movimentação de um personagem dentro de um determinado espaço em ordem de superar obstáculos e atingir objetivos, e desafios de combate, que podem ser apresentados como a presença de agentes não-jogadores que agem contra o personagem do jogador. Estes por sua vez podem ser dispostos de inúmeras formas, dentro de um Metroidvania é comum a existência de salas de inimigos, que só permitem o avanço após a derrota de uma sequência de inimigos, inimigos-chefes ou elites, que são inimigos mais complexos e recorrentemente requerem maior habilidade e experiência do jogador, ou sequências de plataformas que podem ou não incluir inimigos ao longo do caminho.

A culminação destes ocasionam ciclos de jogabilidade de falha e aprendizagem contínua, onde o jogador é levado a dominar o uso das habilidades do personagem e a compreensão dos padrões impostos pelos desafios, sejam combates, inimigos-chefe, sequencias complexas de plataformas etc.

Diversos jogos do gênero exploram esse contexto de formas diferentes, apesar de possuírem semelhanças bastante objetivas. Avaliando alguns metroidvanias mais recentes, Ori in the Blind Forest em contraste com Hollow Knight: Silksong, os desafios apresentados na travessia têm suas similaridades, como o requerimento na precisão de pulos e pulos duplos, bem como o uso das respectivas habilidades de planeio. Entretanto, em Silksong, muitos desses desafios tem uma dificuldade mecânica maior dado a personagem ser extremamente ágil, fazendo o timing, isto é, o intervalo entre uma ação-reação e outra, ser mais estrito.

ENDER LILIES aborda esse cenário de forma mais conservadora, tendo a movimentação de personagem mais lenta, e por consequência, o requerimento de timing é bastante leniente. Em contrapartida, os desafios de combate apresentados são inversamente mais restritos, pois tanto a personagem como os inimigos são mais lentos se comparados com Silksong ou Ori, trazendo aspectos de outros jogos como Darksouls para o gênero, que por sua vez focam muito mais no aprendizado de padrões e na capacitação de usar esses padrões contra o inimigo.

Outro fator importante na expressão da habilidade do jogador está em compreender os cenários e contextos em que seu personagem se encontra, o conceito knowledge-based exploration game estipula que o conhecimento adquirido pelo jogador é parte da força motriz no incentivo da exploração do jogo (MEHDI, 2025). MIO: Memories in Orbit aplica esse conceito após o jogador desbloquear as habilidades de movimentação, que permitem escalar paredes e lançar-se na direção de obstáculos e inimigos, de forma que uma habilidade permite anular o momentum e a outra adicioná-lo, o que dá ao jogador uma forma diferente de observar conjuntos de plataformas já visitadas anteriormente ou recém apresentadas, onde levando em consideração a física da movimentação do jogo, o timing e o conhecimento aplicado dessa habilidades, permite superar sequências desafiadoras e alcançar novos objetivos, capitalizando a progressão espacial e lúdica, muitas vezes com recompensas extras.

Apesar das recompensas palpáveis fornecidas após a superação de algum determinado desafio, existe também o sentimento de conquista embarcado. Este, por sua vez, é uma força motriz bastante presente em jogos famigerados pela sua dificuldade mecânica, ou seja, os desafios requerem muito mais da habilidade motora do jogador. É bastante comum para jogos de plataforma, mas especialmente para metroidvanias, que haja desafios de speedrun, isto sendo dominar e superar algum desafio o mais rápido possível, muitas vezes sendo o desafio de completar a campanha do jogo dentro de um determinado intervalo. Esta modalidade de desafio costuma requer profundo conhecimento das mecânicas principais do jogo, da movimentação e interação das habilidades do personagem com a física e cenários, conhecimento dos cenários propriamente ditos, bem como as rotas e direções adequadas, que no fim culminam na habilidade motora e cognitiva do jogador para aplicar a resultante de todos esses fatores.

Csikszentmihalyi (1990) afirma que a satisfação é uma resultante da superação de desafios em que a habilidade do jogador necessária para o superar é condizente com o contexto da situação, onde o jogo promove formas de superar o desafio, que mesmo com falhas contínuas se torna interessante, equilibrando o investimento de habilidade e tempo do jogador com a resolução do desafio e recompensa resultantes. Um desafio perceptível como desafiador impacta positivamente a experiência do jogador, incentivando-o a seguir tentando (Juul), entretanto desafios que fujam do balanço entre habilidade requerida contra habilidade executada são tediosos e maçantes, que eventualmente desmotiva e frustra o jogador.

Falhar é a melhor parte. Uma falha pode ser percebida de diversas formas, mas em metroidvanias são comumente atribuídas a progresso. Ao falhar, o jogador certamente acumulará frustração, é inevitável, entretanto necessário uma vez que a falha inicialmente, indica uma oportunidade de aprendizado (KOSTER, 2013), que se somado ao fluxo de um desafio justo (CSIKSZENTMIHALYI, 1990), cria esse pequeno paradoxo de em que perder é muitas vezes tão bom quanto vencer (JUUL, 2013), e não menos importante. E para metroidvanias, existem cenários que o jogador pode tornar os desafios do jogo propositalmente mais injustos ou difíceis, em desejo de aplicar o conhecimento adquirido, naquele mundo com aquelas mecânicas, em algo que satisfaça sua ânsia pela conquista.

Outra abordagem desafiadora auto infligida comum em metroidvanias, são os desafios limitadores, como no hit ou no-tools, em que o jogador deve finalizar um jogo sem sofrer danos de nenhuma fonte, como inimigos e cenários, e a outra onde o jogador deve finalizar o jogo sem uma ou mais habilidades ou ferramentas do personagem. No Hit costuma requer absoluto conhecimento das mecânicas chaves do jogo, padrões de inimigos e cenários, rotas de travessia, pontos chaves etc., e a punição por falhar é recomeçar do zero, capitalizando o aprendizado contínuo (KOSTER, 2013) resultante da falha contínua (JUUL, 2013) em busca da conquista do desafio visto como justo pelo jogador (CSIKSZENTMIHALYI, 1990). No Tools é um tanto mais leniente, apesar de quebrar completamente o fluxo e balanceamento esperado do jogo, já que é exatamente esse o objetivo, ser artificialmente mais difícil, por escolha do próprio jogador.

Parte desses desafios auto infligidos muitas vezes acontecem por acaso, uma vez que a progressão de um metroidvania se dá principalmente pela aquisição de novas habilidades do personagem, é comum que alguns jogos do gênero possam ser finalizados sem que o jogador descubra todas as habilidade e ferramentas possíveis, seja por opção ou pela natureza com a qual estas são postas no fluxo da progressão. Um exemplo interessante a ser analisado nesse contexto são os dois irmãos Hollow Knight e Silksong. Ambos possuem mais de uma forma lúdica de finalizar a jornada do personagem, e o considerado final verdadeiro de ambos, isto é, o final narrativo mais correto dentro do contexto imposto, só é alcançável através de uma trava de progressão não convencional. Em ambos, é necessário atingir alguns objetivos e adquirir algumas habilidades e ferramentas para poder permitir alcançar o derradeiro final. Hollow Knight é mais direto, após atingir as condições necessárias ao enfrentar o chefe final o jogo permite desafiar o verdadeiro chefe final, enquanto Silksong, o jogo adiciona algumas camadas narrativas que incitam a retroceder o mapa do jogo e encontrar novos desafios, somente após a conclusão desses desafios é possível desafiar o verdadeiro chefe final.

As habilidades faltantes nesses exemplos influenciam bastante no poderio do personagem, o que ocasiona os desafios até a aquisição das habilidades ou completar objetivos chaves serem mais exigentes que os anteriormente vistos. Em Silksong é especialmente visível, já que diversos inimigos normais e elites recebem uma alteração visual e ganham algumas habilidades extras, e alguns dos inimigos chefes mais complexos só podem ser desafiados após determinado ponto de ignição narrativo.

# 4 Mercado de Jogos Metroidvania

## 4.1 Alcance

O mercado para jogos do gênero metroidvania têm crescido rapidamente desde o lançamento de Metroid e Castlevania, que de forma contrária ao cenário atual são jogos que vem de grandes empresas. Apesar do crescimento contínuo do gênero e seu reconhecimento, jogos como Hollow Knight e Ori impulsionaram significativamente a popularização do gênero nas últimas duas décadas.

A partir de 2015, pode-se observar um aumento no lançamento de jogos metroidvania, com um aumento gradual a partir de 2018, e em 2021, o número mais que dobra com o grande impacto de lançamentos como Hollow Knight em 2017, provando que é possível entregar um bom jogo em um subgênero acatado como nicho e ainda alterar a percepção da indústria, provando que havia um espaço a ser preenchido no mercado.

[Figura 1: Gráfico dos lançamentos de jogos tagueados como metroidvania na Steam]

Fonte: SteamDB, 2025: https://steamdb.info/stats/releases/?tagid=1628

A maioria dos jogos metroidvania recentes vem de pequenos estúdios, muitas vezes indies, estima-se que dentre a média de 10.000 jogos lançados anualmente na Steam, de 3% a 8% representam jogos que possuem o identificador de metroidvania. Ainda assim, sua relevância excede sua participação quantitativa, uma vez que a diversos títulos do gênero figuram entre os jogos independentes de maior sucesso comercial e crítico, como Ori e  Hollow Knight, corroborando que para o gênero, a qualidade e impacto de poucos eleva a percepção geral do mercado.

Outro fator contribuinte para a ascensão do gênero, principalmente em países em desenvolvimento como o Brasil, é a acessibilidade destes jogos, principalmente pelo fato da grande maioria serem indie, que costuma atribuir um valor relativamente baixo de compra quando normalizado para a moeda local. Analisando Hollow Knight: Silksong que lançou recentemente e atingiu o 7º jogo mais vendido na Steam em 2025 (STEAM, 2025), seu valor em dólares americanos é de U$D: 19,99, comparando com o valor em real brasileiro BRL: 59,99 (STEAMDB, 2025), existe uma perceptível intenção da desenvolvedora de tornar o jogo mais acessível no Brasil. Juntamente, a qualidade do produto entregue tem capitaliza ainda sobre o público, que entende que pode conseguir experiências valiosíssimas com um investimento monetário razoável, o que inclui possuir um equipamento que é capaz de performar bem nestes jogos, que por diversos fatores, são bastante leves e otimizados comparando com outros jogos de outros gêneros.

Estratégias de precificação regional são utilizadas para ajustar o valor de produtos digitais às condições econômicas locais, ampliando o acesso e potencializando a adoção em mercados emergentes (VALVE, 2023).

A conhecida low hardware barrier, que é a barreira erguida pela existência da variação de poder computacional na parte inferior do espectro, é um dos elementos que mais populariza jogos metroidvania, pois permite maior penetração em mercados emergentes. Como são jogos comumente de escopo controlado, com uma precisão maior na execução do núcleo das experiências providas, tornam o produto-jogo mais leve em diversos aspectos, seja no custo de armazenamento ou computacional, que quanto menores alavancam exponencialmente a capacidade do jogo de ser jogado por um público maior, principalmente em jogos que vieram originalmente de consoles portáteis, e mantiveram muito das características de jogos similares.

4.2 Tecnologias

Como consequência, pode-se observar dois fatores: as escolhas tecnológicas tomadas para garantir um desenvolvimento simples e barato (muitas vezes sem custo), assim como a escala e precisão dos jogos. Na Steam em 2024, estima-se que 70% dos jogos lançados foram desenvolvidos usando a Unity Engine, devido principalmente pela sua acessibilidade e baixo custo inicial. Assim sendo, a maioria dos jogos metroidvanias modernos, por serem comumente de estúdios pequenos e muitas vezes com pouco (ou nenhum) orçamento, acabam por serem desenvolvidos com a Unity.

Outros motores comuns, como a Godot Engine e o Game Maker, também representam uma parcela boa dos jogos metroidvania. A Unreal Engine aparece muito pouco em jogos metroidvanias, dado que sua excelência está em jogos 3D e de larga escala, mas existem alguns que também a usa, com ENDER LILIES.

É interessante realçar a presença emergente da Godot por ser um motor gráfico de código aberto, ocasiona um custo zero para os desenvolvedores que estão entrando no mercado ou tentando fugir do contínuo aumento nos custos de desenvolvimento de jogos. O crescimento do uso dela também pode ser atribuído a decisões estratégicas de outros motores acessíveis como a Unity, que em 2023 tentou mudar seu modelo de negócio ocasionando uma intensa migração de desenvolvedores solo e indies para outros motores (GODOT, 2024).

Segundo o Unity Game Development Report de 2026, as desenvolvedoras estão preferindo projetos menores com um escopo de negócio maior, focando em parcerias externas ao jogo na tentativa de alcançar um público maior, assim como a preferência pelo desenvolvimento para PC e Mobile ante os consoles tradicionais, somando a já maior base de usuários junto de emergência de gaming handhelds mais modernos, englobando a versatilidade de um PC num chassi portatil com bom desempenho.

Segundo Alyssa Kollgaard (COO, Akupara Games): “É-se limitado em alcance e escopo se miras em apenas uma plataforma, é sempre melhor ir até onde os jogadores estão, do que fazê-los virem até você”.

Jogos com menores exigências de hardware apresentam maior potencial de penetração em mercados emergentes, ao reduzirem limitações técnicas de acesso por parte dos consumidores (NEWZOO, 2022).

## 4.3 Público

O público-alvo para jogos metroidvania é bastante variado, mas controlado, uma vez que geralmente são jogadores que buscam mais que em jogos que apenas jogá-los, e está disposto a arriscar um jogo menos conhecido, indo contra a tendência do jogador comum que joga poucos jogos, e apesar de nichado, o gênero exerce influência significativa no design de jogos contemporâneos de grande impacto, normalmente Triple-A, ocasionando que muitos jogos acabam por herdar elementos de metroidvania, que já os herdara de outros também.

Um fator impactante para metroidvanias está na intrínseca dificuldade em alcançar públicos maiores. Se excluirmos os grandes nomes do gênero, como Hollow Knight ou Ori, raramente haverá uma grande comunidade dedicada aos jogos, o que dá ao público de metroidvanias um papel importantíssimo na capacidade de descoberta de outros jogadores, pela propaganda falada ou apresentada de uma pessoa a outra, muitas vezes é maior do que a visibilidade do marketing dos estúdios, que é limitado por orçamento e escala.

Apesar do alcance limitado, o público deste gênero possui uma tendência a se inteirar mais pelo jogo, querendo conhecer o mundo, mecânicas, histórias e músicas mais a fundo, adicionando uma carga bem grande as desenvolvedoras, esse público gosta da coesão entre ação e informação, enfatizando que a diversão nos jogos digitais está diretamente associada à capacidade do jogador de aprender e dominar padrões dentro do sistema do jogo (KOSTER, 2004) e que jogadores são motivados principalmente por fatores como conquista, exploração e progressão, sendo estes elementos fundamentais para o engajamento em jogos digitais (YEE, 2006).

Outras características que podem ser observadas sobre os consumidores do gênero, são a alta resiliência e tolerância a falha (JUUL, 2013), alto interesse em aprender o jogo e não apenas consumi-lo como conteúdo e o forte engajamento com mecânicas simples e profundas, jogos que oferecem sistemas profundos e consistentes tendem a engajar jogadores por meio do domínio progressivo de suas mecânicas, criando experiências duradouras baseadas em aprendizado e superação (SCHELL, 2008).

# 5 Análise de jogos do gênero

## 5.1 Hollow Knight: Silksong

Hollow Knight: Silksong (T. CHERRY, 2025) é a segunda iteração na aclamada franquia de Hollow Knight, desenvolvida pelo estúdio indie Team Cherry, composta pelo incrível número de 3 integrantes principais. Foi lançado em 2025 para PC, PlayStation, Xbox e Switch, após uma espera pouco maior de 7 anos.  Foi muito bem recebido pelo público gamer em geral, e recebeu excelentes notas da crítica especializada, marcando 90 pontos (de 100) no Metacritic.

### 5.1.1 Contexto

Silksong acompanha a jornada da Hornet, personagem que teve uma participação importante no primeiro jogo, guiando de forma elusiva o cavaleiro, bem como desafiá-lo algumas vezes. Neste jogo, exploramos uma outra cidade-estado no mundo de Hollow Knight chamada de ‘Farloom’ que remete a “tear distante”, que combina bem com a proposta do jogo, bem como se fixa bem a história a ser contada.

### 5.1.2 História

A história do jogo é bem definida e concisa, com uma boa estrutura narrativa e é relativamente direta, recebe-se informações da história através de diálogos e cutscenes ao decorrer do jogo, com uma boa quantidade de informação provida ao jogador sem requerer ler todos os itens ou jornais do jogo.

Começa com Hornet sendo capturada por algumas criaturas desconhecidas e aprisionada em uma carruagem que sela seus poderes. Ao passar por uma ponte, fios de seda emaranham-se na mesma e puxam-na para baixo, quebrando a ponte e roubando parte dos poderes “mágicos” da personagem.

Esta cidade-estado que estamos, é lar da espécie de criatura que era a mãe de Hornet, uma “tecelã” (aranha), sendo um contraste bem interessante considerando que a maioria das criaturas do mundo do jogo são insetos ou derivados, e aranhas não sendo insetos cria-se uma distinção natural na relação entre eles. Tecelãs governavam Farloom e detinham habilidades de cunho divino, até que uma catástrofe assolou o reino e amaldiçoou seus habitantes. Durante a jornada, pode-se aprender mais sobre a maldição se aprofundado em áreas secundárias, que se feito, engrandecem o entendimento da resolução da história.

## 5.1.3 Progressão

#### 5.1.3.1 Habilidades

Hornet sendo meio-aranha dá a ela habilidades acrobáticas acima da média, que é transparecido para a jogabilidade de forma bastante natural. É uma personagem rápida, uma caçadora (como aranhas), que pode usar de diversas habilidades de seda ou mágicas juntamente de variados conjuntos de ataques base para manter a pressão sobre seus inimigos, incentivando uma agressividade extra quanto em combate, que se executado com maestria criam vantagens e aberturas nos inimigos. Em contraparte, para a existência do balanceamento, os inimigos são mais agressivos e costumam dar mais dano (se comparado com o jogo anterior).

Existem sete diferentes conjuntos de ataques base (brasão) disponíveis para o jogador, cada um com uma abordagem única de combate, de ataques frenéticos que recompensam o jogador por não usar habilidades mágicas e por não tomar dano, até ataques média distância que permite um uso maior de ferramentas e magias. Cada brasão possui diferentes espaços de habilidades mágicas e ferramentas, além de alterar a habilidade passiva da personagem.

[Figura: Seletor de brasões da Hornet]

Fonte: Captura de tela de Hollow Kinght: Silksong

O jogo conta com três tipos de ferramentas: ataque, suporte e miscelânea. Ferramentas de ataque comumente aumentam a capacidade da Hornet de causar dano, seja aplicando uma melhoria na mesma ou disparando uma adaga, ferramentas de suporte tendem a aprimorar a sobrevivência e a movimentação, de exemplo a ferramenta “Garralonga” aumenta o alcance dos ataques, e as ferramentas miscelâneas costumam dar outros benefícios passivos, como mostrar a posição atual da personagem no mapa de mão.

Há também as habilidades de seda, que usam de um recurso chamado de “seda” que é recuperado principalmente ao atingir inimigos com ataques básicos. Estas são habilidades bastante poderosas e de grande área de efeito, a habilidade de ceda inicial é uma agulhada gigante, “Lança de Seda”, que causa um grande dano perfurante na direção que a personagem estiver olhando com alcance elevado e limitado, existem um total de seis disponíveis, que podem ser encontradas explorando o mapa, ou adquiridas como recompensa durante a progressão da história.

#### 5.1.3.2 Espacialidade

O mapa de Silksong é bastante vasto e foi meticulosamente construído, com 22 áreas diferentes para serem exploradas no total, e na moda metroidvania, muitas dessas só se abrem para o jogador se ele sair do “caminho feliz” e buscar segredos e concluir solicitações e interações de NPCs.

O jogo é divido em três atos, em que algumas áreas só estão disponíveis após determinados pontos de ignição, como concluir um ato ou concluir uma seção de quebra-cabeças. Seguindo bem o gênero, o jogo possui mais de uma rota para se seguir, sendo possível ir para o segundo ato do jogo de pelo menos duas formas diferentes: caminho peregrino, que seria o caminho “correto”,  seguindo a jornada dos peregrinos que desejam alcançar a capital e serem livrados da maldição ou o caminho do pântano, em que o jogador percorre áreas de nível superior ao esperado no momento do ato 1 e é desafiado por um quebra-cabeça espacial em um nevoeiro, precisando seguir mariposas de seda até sair.

### 5.1.4 Conclusão

Pode-se concluir que Silksong é um dos grandes sucessos do gênero no século, com excelente recepção e captação, assim como altíssima qualidade de produto e de fácil acesso, dado ao valor baixo do jogo se comparado com jogos similares em relação ao que é entregue. Não por menos que foi indicado ao jogo do ano de 2025 por diversos meios como The Game Awards e The Indie Game Awards, competindo com jogos de peso como Clair Obscur: Expedition 33 e outros grandes títulos.

Quanto a herança de metroidvania, Silksong aplica bem todos os aspectos chaves e forças motrizes esperadas de um jogo moderno no gênero, podendo dar destaque principalmente aos aspectos de “ability-gating” muitas vezes opcionais e sendo um pouco mais pesado na dificuldade mecânica que a maioria e conseguindo capitalizar a excelência do primeiro Hollow Knight e expandir a franquia para além.

## 5.2 Nine Sols

Nine Sols (R. CANDLE, 2023) é o primeiro jogo do estúdio Red Candle, trazendo uma estética cyberpunk junto de algumas qualidades distintas da cultura chinesa em sua concepção, com personagens representando animais importantes para a cultura deles, bem como a retratação do campo marcial e científico atrelados aos chineses e seus milênios de história, e não menos importante, a trilha sonora misturando instrumentos tradicionais e eletrônicos.

### 5.2.1 Contexto

O jogo segue a jornada sangrenta pela vingança de Yi, uma raposa que se destacou em sua civilização alienígena pelo seu exímio intelecto e habilidades marciais, que foi traído pela sua mestra e deixado para morrer no fundo de um penhasco, onde definhou e ficou “morto” por bastante tempo, sendo revivido pela sua ligação com um poder metafísico que se manifesta através das raízes de uma árvore.

Yi e seu povo são originários de um planeta distante, e vieram até a terra em uma época primitiva em busca de soluções para o seu planeta, que estava morrendo.

### 5.2.2 História

Após o prologo inicial do jogo, Yi acorda em “mais um dia” na sua nova rotina em uma pequena vila de humanos, fazendo amizade com uma criança chamada Shan-Shuang. Em determinado ponto, um ritual rotineiro para os aldeões começa, e algumas pessoas da vila são selecionadas para “ascenderem” até os “Sóis”, da perspectiva deles, deuses.

Entretanto esse ritual é um abatedouro para humanos, e neste em específico Shan-Shuang havia sido selecionado, e Yi, apesar de demonstrar indiferença pelas vidas alheias, decide salvá-lo e acaba caindo na “fábrica” subterrânea, sendo sua porta de entrada para seu “velho mundo”.

A presença dessa fábrica é discutia ao longo do jogo, e entendemos que Yi junto dos outros nove Sóis a construíram de forma a captar “recursos” e converter uma essência mística em algo usável para tentar restaurar a ecologia do seu planeta natal, ele só não esperava que o uso da tecnologia que passou anos estudando e desenvolvendo acaba-se por se tornar um abatedouro de humanos.

A história é bastante densa e profunda, expandindo o passado do Yi, suas descobertas e sua relação com sua família, amigos e os Sóis. Durante a jornada é possível aprender o que aconteceu com o planeta natal de Yi, bem como o porquê de a fábrica ter saído do controle e ter sido usada para tais fins, também compreender alguns segredos da espécie dele, que o jogo dá a entender ser a origem dos gatos e raposas do nosso mundo.

### 5.2.3 Progressão

#### 5.2.3.1 Habilidades

Yi sendo um exímio espadachim e entendedor das forças naturais do universo, faz o uso do Qi para a maioria de suas habilidades básicas. O Qi é usado tanto nos ataques básicos, quanto em algumas passivas e na habilidade ativa de talismã. Nine Sols é um jogo mais paciente em relação aos confrontos com inimigos, sendo a mecânica defensiva básica o desvio, ou parry, que resulta em uma gameplay mais analítica antes de qualquer coisa: é necessário aprender os movimentos do inimigo para poder usar dos ataques recebidos como uma vantagem.

O desvio é o carro chefe das mecânicas, as cargas de talismã são recarregadas através de desvios precisos, bem como ignorando qualquer dano que irias ser recebido. Por consequência, o jogo tem a tendencia a ser mais “difícil” imediatamente, ao invés de escalar a dificuldade com o tempo, dado que desvios imprecisos acumulam “dano interno”, que ao receber qualquer dano, o dano interno é convertido em dano real somado com o dano original. Os inimigos também acumulam dano interno conforme o jogador acerta desvios no tempo correto, e ao usar talismãs, o dano interno é convertido em dano real. Uma faca de dois gumes.

Nine Sols é um jogo relativamente mais lento em relação a movimentação e a travessia, mas o dado ao núcleo do combate ser abusar de aberturas durante os ataques e movimentos inimigos, o ciclo do jogo é bastante brutal e de difícil execução. Yi possui uma árvore de habilidades que podem ser desbloqueadas a medidas que experiência é adquirida. Morrer em Nine Sols é bastante punitivo, perdendo totalmente a experiência acumulada não gasta, que pode ser recuperada se chegar ao local de falecimento sem morrer novamente. Morrer enquanto tenta recuperar a experiência perdida resulta na perda definitiva da contagem que ficara acumulada no último túmulo.

[Figura: Árvore de habilidades de Yi]

Fonte: Captura de tela de Nine Sols

O jogo possui um sistema de equipamento relativamente simples, que ata a relação biomecânica de Yi com as técnicas de Qi. Os equipamentos são chamados de Jades, e cada jade possui um valor computacional. Yi possui uma determinada quantidade de capacidade computacional, que pode ser expandida ao longo do jogo ao coletar um item e interagir com um NPC específico. As Jades abrem portas para criação de configurações específicas de combate, como permitir o uso desenfreado de talismãs para uma gameplay focada em habilidade, como também capitalizar o máximo em cima de dano interno no oponente, podendo acumular quantidades altíssimas de dano interno nos oponentes e finalizá-los com apenas um talismã.

Em seu arsenal Yi também possui um arco e flecha, que pode ser considerado uma habilidade suprema, dado as seu número limitado de usos e sua potência. Existem vários tipos de flechas, que são úteis em vários cenários diferentes, permitindo ao jogador uma boa variedade de combinações para lidar com quaisquer tipos de desafios.

#### 5.2.3.2 Espacialidade

Nine Sols possuí 14 regiões exploráveis, onde a maioria possui subdivisões espaciais, porém ainda dentro da estética e conceito da região em questão. O jogo faz uso de fundos híbridos de 2D e 3D, primeiro plano com várias camadas e alguns detalhes de paralaxe.

Apesar de ser um metroidvania, a progressão das áreas do jogo é bastante linear e bastante apagada a progressão lúdica do jogo, onde algumas áreas só são acessadas após pontos de ignição na história que resulta na aquisição de alguma habilidade ou chave necessária.

O jogo pode ser finalizado de duas maneiras, um sendo o final canônico e outro o final inacabado. Para poder atingir algum final, o jogador precisa antes enfrentar todos os Sóis e conseguir deles a todo custo o seu “selo Sol”, que permite abrir a ponte até a nave que permitiria retornar ao planeta natal deles. Para atingir o final canônico é necessário progredir as missões internas de todos os personagens interagíveis, assim como explorar os confins do laboratório e compreender para o que a Mestra de Yi utilizava a fábrica, as pesquisas e os equipamentos construídos pelos Sois.

### 5.2.4 Conclusão

Apesar de não ter tido tanta visibilidade no público main stream, para os amantes do gênero, Nine Sols compete com os grandes como Hollow Knight e Ori, sendo extremamente bem avaliado na Steam, mesmo sendo percebido como muito difícil e com uma estética nichada.

O jogo veste bem o que define um metroidvania moderno, sendo bastante sóbrio no uso do “backtracing” e tendo um “ability-gating” bem simplificado, assim como ser bastante coeso com seu “world building” em relação as ligações de “lore” e “plot” aos eventos executados pelo jogador.

## 5.3 MIO: Memories in Orbit

MIO: Memories in Orbit (D. DIXIÈMES, 2026) é um dos metroidvanias lançados mais recentemente na data deste documento. Desenvolvido pelo estúdio francês Douze Dixièmes e publicado pela Focus Entertainment (Styx, Space Marines 2), MIO traz uma estética futurista pós-apocalíptica, com gameplay sidescroller porém ambientado em um mundo tridimensional.

### 5.3.1 Contexto

O jogo acompanha a história de Mio, um pequeno robô que é despertado após um grande colapso no mundo, a fim de arrumar as grandes máquinas “órgãos” que sofreram danos durante o colapso e não estão funcionando corretamente. O mundo este que é habitado apenas por máquinas e robôs que fariam parte desse sistema “anatômico”.

### 5.3.2 História

A história começa com o despertar de Mio por um robô-lacraia desconhecido, que se alegra por Mio ainda estar viva, e logo foge. Seguimos um pequeno caminho até encontrar e reativar Shi, um “vigia”, especificamente, “o vigia da coluna”. Shi contextualiza Mio sobre o que aconteceu com os órgãos e solicita a sua ajuda para reunir as “vozes” deles.

O jogo foca bastante em eventos do passado para poder descrever a situação do presente, Mio em sua jornada vislumbra fragmentos de memória de outros robôs e dos órgãos para compreender o que estava acontecendo. Em determinado momento, Mio encontra Ahti, outro robô do mesmo tipo de Mio, que até o momento não era claro para o jogador, mas  Ahti e Mio são “guardiões”, robôs responsáveis por manter um determinado órgão em funcionamento.

Ahti é contra a jornada de Mio, pois ela haveria percorrido a mesma jornada em um tempo passado, a fim de restaurar o mundo do colapso, porém, ela desistiu por um certo motivo.

Após encontrarmos um fragmento do “Olho”, Mio entende que ela era a guardiã do Olho, e que por não estar presente quando o colapso aconteceu, não pode ajudar ele a se manter funcionando.

Durante a jornada, Mio aprende um pouco mais sobre o mundo que se encontra, o que o levou a situação atual, apendendo que realidade, o mundo dela é uma nave, construída por uma civilização chamada “os viajantes”, que teriam sido exterminados como parte do colapso. A nave teria como objetivo encontrar um novo lar para os viajantes, e os órgãos da nave, existiam para aprender com eles e da forma deles serem capazes de manter a embarcação funcionando adequadamente.

O colapso em si fora terrível e danificou todos os sistemas da embarcação, mas o tempo, foi o maior vilão. Durante a etapa da jornada em que Mio se reencontra com o que sobrou do Olho, ela pode ler alguns registros da embarcação que datam centenas de milhares de anos de tentativas de atracar em outros centenas de milhões de planetas, porém, cada falha adicionava cada vez mais carga no órgão principal, o coração.

Como um bom metroidvania, o jogo possui dois finais canônicos independentes. Um em que enfrentamos Ahti, Guardiã do Coração e a derrotamos, levando as vozes coletadas até aquele ponto, junto da voz de Ahti, e devolvemos para o coração, na cuja permite que o coração se recupere um pouco e consiga seguir viagem, mesmo com os sistemas da nave não funcionais. Não é explicito, mas entende-se que esse final impede que a história continue. O segundo final é acessível após superar alguns desafios na parte inferior da nave e desativar o “crisol”, um grande robô que fabricava “pérolas” (memórias dos robôs), e estava descartando todas as pérolas. Com o crisol desabilitado, Mio então pode acessar a biblioteca, local onde todos os registros ficam salvos, e lá entender o que causou o grande colapso e o extermínio dos viajantes, precisando enfrentar Shi, para poder avançar.

### 5.3.3 Progressão

#### 5.3.3.1 Habilidades

Por ter sido reativada de forma “ilegal” (não seguindo os protocolos da embarcação), Mio acorda com pouquíssimo conhecimento de tudo e todos, tendo dificuldade para andar e pular.

A maior parte das habilidades de Mio são atreladas a sua característica principal, ela possui alguns tentáculos na cabeça (que parecem cabelo), e a grande maioria das habilidades adquiríveis, e dos itens equipáveis, melhoram o seu uso.

As habilidades principais de movimentação atuam em cima do “ability-gating” do gênero, com pulo duplo, “grapple”, escalada, todos esses são principais e fundamentais para a progressão, e todos usam os cabelos da personagem como vetor visual.

A movimentação da personagem é baseada em física e o “platforming” é o ponto principal do jogo, sendo possivelmente o mais difícil desta lista. O jogador precisa aprender a usar o momentum da personagem para alcançar novos locais, e muitas vezes, para adquirir vantagem durante o combate, que pode parecer mais lento inicialmente já que Mio não possuí habilidades comuns como esquivas ou defesas inicialmente.

O modus operandi de MIO, em relação as habilidades e ferramentas é bastante similar ao de Silksong. Após determinado ponto chave ou evento, as habilidades chaves, como a escalada e planeio, são adquiridas. Estar por sua vez, são de altíssima importância dado a inata dificuldade das sequencias de plataforma e até mesmo em alguns combates. MIO promove abertamente o combate áerio, reduzindo a efetividade da gravidade e permitindo seguir no ar caso acerte alvos válidos, incluindo inimigos.

As ferramentas de MIO são tratadas como equipáveis, sendo todas habilidades passivas que vão de alterar o funcionamento de alguma das habilidades principais a aumentar a exibir a vida restante dos inimigos.

A ‘Vida’ de Mio é um dos fatores de progressão mais importantes a destacar. Mio possui um determinado número de “camas de proteção”, que atuam como pontos de vida, e, ao esgotarem, há um último ponto de vida (que seria a derradeira vida da personagem). Essas camadas de proteção não só indicam quantos ataques Mio pode sofrer, como também indicam o estado atual do mundo em relação a história, pois conforme o jogador avança, o mundo sofre um grande impacto, e Mio sendo uma máquina que faz parte deste, tem uma camada de proteção corrompida naquele momento, efetivamente reduzindo a vida máxima de Mio. É possível adquirir mais algumas camadas ao longo do jogo, e ao finalizar, é possível ter até quatro camadas corrompidas, alinhado o desenvolvimento da história e da personagem com elementos de gameplay.

#### 5.3.3.2 Espacialidade

A espacialidade de MIO extremamente criativa e o level design foi meticulosamente pensado para complementar a experiência lúdica do jogador, usando de elementos simples nas ligações do mapa, que, quando percebidas, geram um grande impacto ao jogador.

O mapa de MIO é divido principalmente em 2 grandes áreas, superior e inferior, e apesar da inferior ser menor, o tempo gasto na exploração é equivalente em ambas, uma vez que a inferior apresenta diversos desafios de combate e de platforming. Somando as subdivisões há cerca de dezessete regiões para explorar, com variados pontos de interesse que são ressaltados com artes no menu do mapa.

A grande virada de compreensão acontece após avançar o suficiente na história e tendo explorado bastante a parte inferior da embarcação, onde encontra-se um mecanismo que permite alterar as entradas e saídas da área rotacionando o mapa, que permite avançar na exploração da área superior ao conectar um elevador que antes permitia uma conexão, e agora leva a um lugar completamente diferente, bem como permite a exploração do crisol, área de extrema importância para a história e para o ‘final derradeiro’, após subir por um dos elevadores escalando-o e então descendo por outro elevador que ficará desconectado da parte superior.

[Figura 2: Captura de tela do mapa finalizado]

Fonte: MIO: Memories in Orbit

Após o jogador atingir determinado ponto na história e ter explorado a maior parte do mapa, liberando os acessos e atalhos principais, o mapa se torna cíclico, podendo-se dar uma volta completa por ele.

Por ser um jogo tridimensional, MIO usa de multiplos planos para aprimorar a experiencia e a progressão do jogador, algo um tanto incomum. É possivel escalar objetos que estão no fundo das cenas se estes forem do tipo escaláveis e, para acessar a área do encontro final no ‘final derradeiro’, é necessário escalar uma correnteza de fluidos no fundo do mapa que são bastante elusivos em aparência e posicionamento, a fim de não deixar explícito a sua importância ou uso.

### 5.3.4 Conclusão

MIO é mais uma excelente iteração nos jogos metroidvanias, com um “ability-gating” bem dinâmico e conciso, com uma dificuldade de “platforming” bastante elevada, que traz embarcado uma boa refrescância em cima das mecânicas equivalentes herdadas do gênero, como seu “backtracking” extremamente criativo ao fazer o mapa dar uma volta em si, que lembra um pouco o “level design” de Sekiro: Shadow dies twice (FROMSOFTWARE, 2019).

Apesar de ser excelente jogo, MIO passou despercebido na grande mídia, principalmente por ser “mais um indie metroidvania” cujo espaço mental é ocupado principalmente por Hollow Knight ou Ori, o jogo criou uma boa tração nos fãs do gênero e adquiriu uma comunidade para si.

## 5.4 ENDER LILIES: Quietus of the Knights

ENDER LILIES: Quietus of the Knights (L. WIRE, AGLOBE, 2021) é o jogo resultante de um time já experiente no mercado atuando agora em um metroidvania. O estúdio Live Wire foi estabelecido em 2018 por veteranos da indústria que anteriormente pertenciam a agora extinta Neverland Company, conhecida pela franquia Rune Factory. ENDER LILIES traz uma estética gótica sudo-medieval e melancólica ao gênero, com uma história profunda e trilha sonora de primeira linha, culminando numa experiência brutal e etérica.

### 5.4.1 Contexto

O jogo acompanha a jornada de Lily, uma sacerdotisa branca recém desperta no reino de Land’s End, uma terra outrora próspera agora acometida pela “Chuva da Morte”, que transforma todos os seres vivos em criaturas amaldiçoadas e hostis. Lily pertence linhagem de sacerdotisas, que foram criadas pelo primeiro rei para purificar a “corrupção” e manter a paz no reino.

### 5.4.2 História

Lily começa sua jornada após acordar na ruína de uma igreja desolada e após vagar pela área, encontra o Cavaleiro, que está acometido pela maldição e já sem salvação. Lily purifica o espírito do Cavaleiro, este que então a reconhece e passa acompanhá-la, como sua principal fonte de proteção.

O desenrolar da história segue a rota de falhas memórias que podem ser recuperadas ao longo da jornada, ao concluir pontos chaves, derrotar determinados chefes ou ao purificar determinados personagens. Na sequência do cavaleiro, Lily encontraria Siegrid, a Guardiã, que também estava acometida pela corrupção, e após derrotá-la e purificá-la, uma nova memória é adquirida. Esse fluxo se repete para a maioria dos chefes primários da história.

O objetivo final de Lily é livrar o reino da corrupção, porém ao fazê-lo, toda maldição “purificada” é acumulada nela mesma, criando uma carga física perceptível. Conforme mais purificação é realizada, a aparência de Lily é afetada, bem como sua força e a força de seus espíritos.

ENDER LILIES possui bastante profundidade a seu world building, que com a interpretação do jogador coaliza na compreensão dos fatos pela visão dele, permitindo uma história multifacetada com variada capacidade de entendimento, que agrega um excelente valor de descoberta.

O jogo pode ser finalizado de três formas diferentes, e é possível fazer todos os finais em um mesmo arquivo de salvamento. O final A é entendido como o final neutro, sendo possível atingi-lo precocemente ao escolher deixar o fardo de salvar o reino para trás, libertando os espíritos que carrega e dando-lhes o descanso final. O final B é tido como o final ruim, que após chegar no epicentro da corrupção, Lily encontra sua mãe acometida pela possessão do Rei das Maldições, a criatura vil responsável por todo o sofrimento causado aquela terra. Lily o enfrenta, mas por ser muito jovem e o inimigo ser muito poderoso, ao purificá-lo Lily torna-se então o epicentro da corrupção, mantendo o atual ciclo, porém agora sem mais chances de sucesso, já que era a última sacerdotisa viva. O final C é o final “canônico”, que após explorar os laboratórios subterrâneos e compreender um pouco mais da linhagem das sacerdotisas, Lily forja um amuleto de proteção que permite eliminar a maldição após ela acumulá-la em seu corpo, assim purificando o Rei das Maldições e salvando não só o reino, como também sua mãe.

### 5.4.3 Progressão

#### 5.4.3.1 Habilidades

Lily por ser uma humana jovem e frágil, não possui habilidades de combate para poder avançar, isto é delegado aos espíritos purificados que a seguem. Cada espírito possui uma habilidade ativa que pode ser uma invocação persistente ou de uso único, ou um combo de ataques leves e carregados. A partir de um determinado ponto no jogo, é possível usar uma habilidade de carga, que requer um determinado número de acertos para carregar, sendo uma “habilidade suprema”, que varia dependendo do espírito usado para invocá-la.

Se comparado com outros jogos no gênero, o sistema de combinação de equipamentos e espíritos é relativamente simples em sua concepção, permitindo uma gameplay mais focada em execução do que preparação. ENDER LILIES também usa o sistema de equipáveis, itens que podem ser achados durante a exploração, que permitem benefícios passivos (mais vida, mais recuperação, etc) e alguns ativos (como parry), mas não alteram de forma significativamente a abordagem ao combate como em Silksong.

#### 5.4.3.2 Espacialidade

ENDER LILIES segue um padrão de espacialidade um pouco mais antigo, bastante similar aos primeiros Castlevanias, em que o mapa não né especialmente detalhado por área ou região, mostrando apenas as salas e as interconexões conhecidas, dito isso não faz o jogo pequeno ou inexplorável, pelo contrário, acaba por incentivar ainda mais o senso de exploração do jogados, pois o jogo colore as áreas do mapa entre laranja e azul, mostrando áreas que foram completamente exploradas, e todos os segredos encontrados, e áreas incompletas nesses aspectos

[Figura 3: Captura de tela, pequeno recorte do mapa de ENDER LILIES]

Fonte: ENDER LILIES: Quietus of the Knights

### 5.4.4 Conclusão

ENDER LILIES: Quietus of the Knights transcendeu sua origem como o projeto de estreia de uma nova publicadora para se tornar um marco de design atmosférico e narrativa melancólica. Através da colaboração entre a Adglobe e a Live Wire, o título demonstrou que é possível unir a eficiência de uma estrutura corporativa à visão artística de um projeto autoral. O sucesso de 1,5 milhão de cópias e a criação de uma sequência ainda mais aclamada indicam que a "Série Ender" possui uma identidade sólida, fundamentada na simbiose entre arte visual, música de alta qualidade e uma exploração recompensadora (OBEDKOV, 2024). Para o analista de metroidvanias, ENDER LILIES permanece como um estudo de caso essencial sobre como a atmosfera e a emoção podem elevar mecânicas de gênero tradicionais a um patamar de excelência artística.

## 5.5 Ori and the Will of the Wisps

### 5.5.1 Contexto

Ori and the Will of the Wisps (M. STUDIOS, 2020) é o segundo jogo da aclamada franquia do estúdio, expandindo o universo do jogo a uma nova região, Niwen, que havia perdido seu espírito do salgueiro e fora acometida pela “decadência”, similar aos acontecimentos do primeiro jogo. Desenvolvido pelo estúdio Moon Studios, e publicado pela Xbox Games Studios, o segundo jogo da franquia teve uma tração bem marcante na indústria devido a sua estética e trilha sonora.

### 5.5.2 História

A história do jogo começa após o nascimento de Ku, uma pequena coruja filha de Kuro (antagonista do primeiro jogo), que nasce com uma de suas asas atrofiadas, a impedindo de voar normalmente. Ori e sua família aceitaram cuidar de Ku, em honra ao sacrifício final de Kuro no primeiro jogo, decidem persistir através da deficiência de Ku e ajudá-la a voar.

Em um desses treinamentos de voo, Ori e Ku são pegos em uma tempestade violenta, e levados a cair e separar-se um da outra na região de Niwen. Após acordar Ori começa a explorar a região na tentativa de encontrar sua irmã, e descobre a situação atual do salgueiro de Niwen, que havia perdido seu espírito e este havia se dividido em pequenos fogos-fátuos espalhados por toda a região, e como Ori também é um espírito do salgueiro “filhote”, decide ajudar Seir (o espírito de Niwen) a reunir suas forças para restaurar a terra e afugentar a “decadência”.

Ao encontrar Ku novamente, eles se deparam com uma ameaça não premeditada, Shriek (Grito), uma coruja que nasceu desolada no “Bosque Silencioso” e possui uma visível característica fisiológica atípica, que os ataca e fere gravemente Ku. Shriek é uma das presenças avassaladoras durante a jornada de Ori para recuperar os fragmentos de Seir, e por diversas instâncias tenta impedi-lo.

A partir de um certo ponto, Ori encontra Kwolok, o sapo guardião, e os Moki, um povo local de Niwen, que o auxilia a compreender Shriek e guiá-lo até os outros guardiões dos biomas, como Mora, a aranha, no bioma da escuridão.

Após a jornada árdua para reunir os fragmentos, e após alguns sacrifícios, Ori finalmente chega até o salgueiro, a fim de restaurar a ordem em Niwen, porém Seir é sequestrado por Shriek, culminando no embate final entre eles. Após derrotá-lo a muito custo, Ori finalmente restauraria Seir e o salgueiro, mas, Seir comenta que a época dele passou, e que alguém precisaria assumir o seu manto, e Ori sendo uma “luz” do salgueiro, aceita esse último trabalho.

### 5.5.3 Progressão

#### 5.5.3.1 Habilidades

Diferentemente do primeiro jogo da franquia, este agora usa uma sistemática de combate ativa, na qual o próprio personagem é o ator das ações e habilidades, usando agora principalmente uma espada “espectral”, e também elevando a velocidade de ataque média da personagem, enfatizando que Ori amadureceu desde os eventos do primeiro jogo. Assim como um bom metroidvania, o jogo possui várias habilidades que são adquiridas ao longo da jornada, como um “dash” e um “bash” (permite mudar a direção de Ori no ar ao interagir com elementos específicos ou projéteis e conservar parte do momentum), que são obrigatórias. Possui várias habilidades secundárias que podem ser adquiridas opcionalmente também, como a “lança” ou a “sentinela”.

Ori possui um sistema de equipamentos relativamente simples, parecido com o de ENDER LILIES, onde algumas runas proporcionam aprimoramentos passivos, enquanto outras adicionam benefícios ativos. Estes permitem o jogador escolher a forma que mais lhe agrade na hora de explorar ou combater inimigos, permitindo certa capacidade de buildcrafting.

[Figura: Inventário de equipamentos de Ori]

Fonte: Captura de tela de Ori and the Will of the Wisps

#### 5.5.3.2 Espacialidade

Ori and the Will of the Wisps possui um dos level design mais sofisticados dos jogos no gênero, sendo um excelente exemplo da “mão invisível”, com ênfase no uso da iluminação para guiar o olhar (affordance) do jogador. Todas as áreas do jogo são desenhadas de forma a dar a sensação de fluidez e naturalidade, fazer parecer que tudo pertence àquele lugar, com o uso de verticalidade e de inúmeras camadas de paralaxe para criar ambientes que parecem massivos e profundos, promovendo conexões orgânicas entre as diversas áreas do mapa, bem como um “backtracking” bastante natural.

O jogo também é uma referência em design baseado em física, em que muitos cenários são pensados para que o jogador nem precise tocar no chão, que possa seguir no ar através do uso combinado das diversas habilidades de movimento, criando uma experiência fluída e rápida, saindo um pouco da natureza mais lenta do gênero para uma bastante dinâmica e ativa.

### 5.5.4 Conclusão

A franquia Ori consolidou-se como um dos maiores sucessos comerciais do gênero Metroidvania moderno. Mesmo sendo um título disponível no serviço de assinatura Xbox Game Pass desde o lançamento, o que teoricamente poderia reduzir as vendas diretas, a série atingiu excelentes números, cerca de 15 milhões de unidades vendidas (VGCHARTZ, 2025).

Ori and the Will of the Wisps é frequentemente citado como uma obra-prima técnica e artística. No agregador Metacritic, o jogo mantém uma pontuação média de 90 a 93 (variando por plataforma), garantindo o selo "Must-Play" (METACRITIC, 2026).

# 6 Criação de um Metroidvania

A criação de um Metroidvania exige uma abordagem sistêmica, onde o desenvolvimento não segue um modelo puramente linear, mas sim uma integração constante entre mecânica e espaço (SUBTRACTIVE DESIGN, 2013). Diferente de outros gêneros, a arquitetura do jogo atua como o motor principal da experiência, exigindo que o designer preveja as capacidades lúdicas do avatar antes mesmo da construção geométrica final das áreas (BYCER, 2-25; RODRÍGUEZ et al., 2020).

## 6.1 Definição de História e Escopo

A narrativa em um Metroidvania deve ser estruturada como uma "história espacial", onde o enredo é frequentemente fragmentado e depende da progressão exploratória para ser reconstruído pelo jogador (JENKINS, 2004). O escopo deve ser definido não pela quantidade de salas, mas pela densidade de interações possíveis; em projetos de iniciação científica, recomenda-se o foco na "não linearidade guiada", assegurando que o jogador tenha liberdade sem sofrer de paralisia de escolha (PRADO; LAZARINI; FÁVARO, 2020).

Além disso, a estrutura de "micronarrativas" permite que o designer insira informações de lore em objetos e arquiteturas, tornando o ato de explorar uma investigação arqueológica (JENKINS, 2004). Isso reforça o backtracking, pois áreas visitadas podem revelar novas camadas interpretativas após eventos chaves (MALEYI, 2025).

Henry Jenkins (2004) propõe que o game design pode ser entendido como uma forma de "Arquitetura Narrativa". No Metroidvania, isso se manifesta na forma como a história é fragmentada e distribuída pelo ambiente, exigindo que o jogador a reconstrua através da exploração.

| Modelo de Narrativa | Aplicação Prática | Efeito no Jogador |
| --- | --- | --- |
| Narrativa Evocativa | Uso de biomas góticos ou industriais familiares. | Cria imersão imediata via tropos de gênero. |
| Narrativa Embutida | Itens de lore, murais e design de ruínas. | Recompensa a curiosidade e o backtracking. |
| Narrativa de Encenada | Eventos ativados por portões de habilidade. | Integra mecânica de jogo com progressão de plot. |
| Narrativa Emergente | Liberdade de rota e subversão de sequência. | Gera histórias pessoais de descoberta e superação. |

## 6.2 Definição do cunho artístico

O cunho artístico de um Metroidvania transcende a estética visual, servindo como uma ferramenta funcional de wayfinding. Cores, iluminação e silhuetas de cenário devem ser projetadas para comunicar intuitivamente ao jogador se uma barreira é transponível ou se requer uma habilidade futura, operando como uma "mão invisível" que orienta sem o uso de interfaces intrusivas (PRADO; LAZARINI; FÁVARO, 2020).

A semiótica ambiental deve ser consistente: se um portão é vermelho, o jogador deve associá-lo a uma habilidade específica (TOTTEN, 2019). Essa "legibilidade do mundo" garante que o jogador sinta que o erro de navegação é sua responsabilidade, e não uma falha de comunicação do jogo (SWINK, 2009).

## 6.3 Definição da tematização e trilha sonora

A tematização deve apoiar a imersão na ecologia do mundo, utilizando biomas que possuam uma lógica interna e autonomia própria (ANDRIANO; CARACCIOLO, 2022). Complementarmente, o áudio desempenha um papel fundamental na narrativa ambiental, onde a trilha sonora e os efeitos sonoros dinâmicos não apenas reforçam o tom emocional, mas servem como guias de consciência situacional, informando sobre perigos distantes ou segredos ocultos (ANDRIANO; CARACCIOLO, 2022).

## 6.4 Escolha das tecnologias adequadas

Tecnicamente, o Metroidvania impõe desafios de gestão de memória devido à vastidão de seus mapas interconectados. A escolha da engine e de frameworks específicos deve priorizar sistemas de carregamento dinâmico (dynamic map loading) e carregamento de blocos de código sob demanda, garantindo uma exploração contínua e sem interrupções por telas de carregamento que quebrariam a imersão espacial (RODRÍGUEZ et al., 2020).

## 6.4.1 Gestão de Memória e Carregamento Dinâmico de Cenas.

A gestão de memória é crítica em mapas vastos; o uso de streaming de assets permite que biomas complexos coexistam sem interrupções (RODRÍGUEZ et al., 2020). Motores como Unity ou Godot facilitam essa integração através de sistemas de cenas e prefabs otimizados para 2D (PADUANO, 2025).

## 6.4.2 Algoritmos de Pathfinding para Ambientes de Plataforma 2D.

Pathfinding em ambientes de plataforma 2D possuem algumas nuâncias se comparados aos executados em ambientes de grades ou top down, pois não há garantia na espacialidade 2D, exigindo a modelagem do problema como um espaço de estados que considere restrições físicas e ações do agente (RUSSELL; NORVIG, 2010). A*, possivelmente um dos mais comuns, quando aplicados sobre um sidescroller 2D, requer uma computação prévia dos custos das ações, bem como a especificação delas de forma mais manual, o que adiciona uma carga extra a já complexa estruturação do level design, comumente resultando em entidades que, se tocam ao chão, não perseguem o jogador por muito tempo, se sequer perseguirem (TOGELIUS; YANNIKAKIS, 2016).

Entidades voadoras ou de longo alcance costumam ser a forma de balancear esse custo, quando não há influência da gravidade, o problema de navegação se aproxima de ambientes top-down, permitindo o uso direto de algoritmos clássicos de pathfinding (MILLINGTON; FUNGE, 2009). Alternativamente, comportamentos baseados em steering ou cálculo direto de trajetória podem substituir algoritmos de busca, reduzindo o custo computacional (REYNOLDS, 1999).

## 6.5 Metodologias de Espacialidade

A espacialidade é regida pela relação entre o corpo do avatar e a geometria do mundo (ANDRIANO; CARACCIOLO, 2025). No Level Design, aplica-se o Rational Level Design (RLD) para garantir justiça lúdica através de métricas rigorosas de movimentação (PADUANO, 2025).

Para o cálculo de pulo, utiliza-se a cinemática:

A consistência matemática entre a arte e a colisão evita a frustração do jogador em saltos de precisão (SWINK, 2009). O World Building deve tratar o mapa como um ecossistema vivo, promovendo a "imaginação ambiental" (ANDRIANO; CARACCIOLO, 2025).

### 6.5.1 Level Design

A aplicação do Rational Level Design (RLD) é essencial para garantir a justiça lúdica através de métricas rigorosas de movimentação. É necessário estabelecer padrões matemáticos para alturas de salto e larguras de corredores, permitindo que a dificuldade do jogo evolua através da exigência de maior precisão motora do jogador, e não apenas pelo aumento de atributos estatísticos (LEVEL DESIGN BOOK, 2025; PADUANO, 2025).

### 6.5.2 World Building

O World Building deve ser tratado como a criação de um ecossistema vivo, onde a interconexão entre as áreas faz sentido biológico ou arquitetônico. A construção de biomas autônomos, que resistem a uma visão puramente utilitária do herói, promove o que a literatura chama de "imaginação ambiental", onde o jogador deve atuar como um observador e naturalista para compreender o mundo (ANDRIANO; CARACCIOLO, 2022).

### 6.5.3 Conciliação

A conciliação ocorre quando a mecânica se torna inseparável do ambiente. Neste ponto, o mapa deixa de ser um mero cenário para se tornar um quebra-cabeça em larga escala, onde cada nova habilidade adquirida serve como uma "chave mecânica" que recontextualiza todo o espaço visitado anteriormente através do backtracking significativo (PRADO; LAZARINI; FÁVARO, 2020).

## 6.6 Execução

Segue um modelo iterativo, iniciando pelo blockout (greyboxing) para validar métricas antes da arte final (PADUANO, 2025). Ciclos de playtesting medem o estado de fluxo e garantem que a maestria sobre o sistema seja o principal motor de progresso (PADUANO, 2025; BYCER, 2025).

A análise do Game Feel durante a execução permite ajustar a resposta tátil (sensibilidade e inércia), essencial para que o jogador sinta o empoderamento do avatar (SWINK, 2009). A iteração constante entre o design de níveis e o conjunto de habilidades garante que nenhum obstáculo seja impossível ou trivial demais para o estágio atual do jogador (SYLVESTER, 2013).

### 6.6.1 Prototipagem Rápida e Greyboxing.

Inicia-se as construções iniciais do mapa com o posicionamento de polígonos regulares simples com o objetivo de espacializar a área e testar o fluxo da sua exploração usando as já definidas métricas de movimentação. A resultante é armazenada e posteriormente retornada quando a parte estética estiver efetivamente decidida, avaliando a necessidade de ajustes espaciais para a finalidade do “game feel” ser orgânico para a cena em questão

### 6.6.2 Análise de Feedback e Iteração do Game Feel.

Posteriormente as intrusões iniciais da estética ao protótipo, uma nova etapa de verificação do fluxo entra em vigor, a fim de garantir que todo o contexto da cena, jogador e personagem estejam em harmonia em determinado ponto da progressão espacial, e certificar de que as funcionalidades preteridas seguem seu fluxo esperado, ou se será necessário regressar a prototipagem.

# 7 Conclusão

# 8 Bibliografias:

ANDRIANO, Angelo Maria; CARACCIOLO, Marco. Metroidvania ecologies: exploration and the environmental imagination in Hollow Knight and Rain World. Eludamos: Journal for Computer Game Culture, v. 16, n. 2, p. 9–28, 2025.

BARR, Adam; NOBLE, James; BOYD, Mark. Engineering emergent behavior: a study of AI in games. ACM Queue, New York, v. 3, n. 7, p. 20–29, 2005.

BOLUK, Stephanie; LEMIEUX, Patrick. Metagaming: Playing, Competing, Spectating, Cheating, Trading, Making, and Breaking Videogames. Minneapolis: University of Minnesota Press, 2017.

BOURKE, Paul; TOGELIUS, Julian; YANNIKAKIS, Georgios N. Procedural Content Generation in Games. Cham: Springer, 2016.

BYCER, Joshua. Game Design Deep Dive: Metroidvania. Boca Raton: CRC Press, 2025.

CHAMPANDARD, Alex J. AI Game Development: Synthetic Creatures with Learning and Reactive Behaviors. Indianapolis: New Riders, 2003.

CSIKSZENTMIHALYI, Mihaly. Flow: The Psychology of Optimal Experience. New York: Harper & Row, 1990.

DEVTODEV. Game market overview: the most important reports published in February 2025. Disponível em: https://www.devtodev.com/resources/articles/game-market-overview-the-most-important-reports-published-in-february-2025. Acesso em: 27 mar. 2026.

DORMANS, Joris. Engineering emergence: applied theory for game design. In: PROCEEDINGS OF THE DIACONFERENCE, 2011.

HUNICKE, Robin; LEBLANC, Marc; ZUBEK, Robert. MDA: a formal approach to game design and game research. In: PROCEEDINGS OF THE AAAI WORKSHOP ON CHALLENGES IN GAME AI, 2004.

JENKINS, Henry. Game design as narrative architecture. In: WARDRIP-FRUIN, Noah; HARRIGAN, Pat (ed.). First Person: New Media as Story, Performance, and Game. Cambridge: MIT Press, 2004. p. 118–130.

JUUL, Jesper. The Art of Failure: An Essay on the Pain of Playing Video Games. Cambridge: MIT Press, 2013.

KOSTER, Raph. A Theory of Fun for Game Design. 2. ed. Sebastopol: O’Reilly Media, 2013.

MALEKI, Mehdi. Metroidbrainia: A Genre Analysis of Knowledge-Based Exploration Games. 2025. Disponível em: https://www.researchgate.net/publication/397024365_Metroidbrainia_A_Genre_Analysis_of_Knowledge-Based_Exploration_Games. Acesso em: 27 mar. 2026.

MENDES, Rodrigo. The history and design of metroidvania. In: SIMPÓSIO BRASILEIRO DE JOGOS E ENTRETENIMENTO DIGITAL (SBGAMES), 2019. Anais [...].

METACRITIC. Ori and the Will of the Wisps (Xbox Series X). Disponível em: https://www.metacritic.com/game/ori-and-the-will-of-the-wisps/. Acesso em: 16 abr. 2026.

MILLINGTON, Ian; FUNGE, John. Artificial Intelligence for Games. 2. ed. Boca Raton: CRC Press, 2009.

OBEDKOV, Evgeny. Ender Lilies: Quietus of the Knights hits 1.5 million copies sold: what to know about its developer Adglobe. Game World Observer, 25 jul. 2024. Disponível em: https://gameworldobserver.com/2024/07/25/ender-lilies-quietus-of-the-knights-1-5-million-copies-sold. Acesso em: 10 abr. 2026.

PADUANO, Ivan. Game Design Manual: The Ultimate Scientific Guide to One of the Most Fascinating Worlds of Design. Rome: Sapienza University, 2025.

PRADO, Camila Stefani do; LAZARINI, João Pedro P. R.; FÁVARO, André Luís Orlandi. Análise dos princípios de desenvolvimento de jogos metroidvania. In: SIMPÓSIO BRASILEIRO DE JOGOS E ENTRETENIMENTO DIGITAL (SBGAMES), 2020. Anais [...].

REYNOLDS, Craig W. Steering behaviors for autonomous characters. In: GAME DEVELOPERS CONFERENCE (GDC), 1999. Proceedings [...].

RODRÍGUEZ, R. et al. A framework for metroidvania games. In: SIMPÓSIO BRASILEIRO DE JOGOS E ENTRETENIMENTO DIGITAL (SBGAMES), 2020. Anais [...].

RUSSELL, Stuart; NORVIG, Peter. Artificial Intelligence: A Modern Approach. 3. ed. Upper Saddle River: Pearson, 2010.

SALEN, Katie; ZIMMERMAN, Eric. Rules of Play: Game Design Fundamentals. Cambridge: MIT Press, 2003.

SCHELL, Jesse. The Art of Game Design: A Book of Lenses. Burlington: CRC Press, 2008.

STEAMDB. Game releases by tag (metroidvania). Disponível em: https://steamdb.info/stats/releases/?tagid=1628. Acesso em: 27 mar. 2026.

SUMMERVILLE, Adam et al. Procedural content generation via machine learning (PCGML). IEEE Transactions on Games, v. 10, n. 3, p. 257–270, 2018.

SWINK, Steve. Game Feel: A Game Designer’s Guide to Virtual Sensation. Burlington: Morgan Kaufmann, 2009.

SYLVESTER, Tynan. Designing Games: A Guide to Engineering Experiences. Sebastopol: O’Reilly Media, 2013.

TOTTEN, Christopher W. An Architectural Approach to Level Design: Processes and Experiences. 2. ed. Boca Raton: CRC Press, 2019.

UNITY TECHNOLOGIES. Unity Gaming Report. Disponível em: https://unity.com/resources/gaming-report#top-platforms-expansion. Acesso em: 27 mar. 2026.

VALVE. Steam – best of 2025. Disponível em: https://store.steampowered.com/charts/bestofyear/2025. Acesso em: 27 mar. 2026.

VGCHARTZ. Ori series has sold over a combined 15 million units. Disponível em: https://www.vgchartz.com/article/464159/. Acesso em: 16 abr. 2026.

ZIPDO. Unity gaming industry statistics. Disponível em: https://zipdo.co/unity-gaming-industry-statistics. Acesso em: 27 mar. 2026.

<!-- Coment?rios internos do Word extra?dos do arquivo original. -->
<!-- Coment?rio 1080520684 | Autor: Gabriel Onzi Benachio
Jogos metroidvania costumam herdar aspectos de outros gêneros a fim de complementar a experiência do gênero de ação e exploração, sendo muito comum elementos de RPGs, Soulslike e narrativos. Adicionado a participação destes, é possível ancorar partes da experiência aos elementos chaves dos mesmos, como a compensação narrativa, isto é, dar ao jogador informações importantes para a narrativa, história e mundo do jogo, ou a existência de caminhos sem volta, em que algumas ações do jogador ao longo do jogo alteram o fluxo narrativo, que podem variar de simples detalhes até mesmo uma coalisão narrativa completamente diferente ou ainda a existência de mecânicas, habilidades ou ferramentas que são mutualmente exclusivas, onde adquirir uma pode impedir a aquisição doutra ou a combinação de algumas exclui uma ou mais outras. Silksong possuí vários exemplos destas, para citar um, existe uma ferramenta no jogo que permite o jogador a disparar projéteis, esta ferramenta pode ser montada de três formas diferentes, ocasionando em aparência e funcionamento diferente, onde uma dispararia vários projeteis de uma vez e uma outra permitiria disparar continuamente enquanto houver projeteis a serem consumidos.
ENCONTRAR UM LUGAR PRA BOTAR TALVEZ
-->
<!-- Coment?rio 858458524 | Autor: Gabriel Onzi Benachio
word lixo não tá exibindo a equação no documento
-->
<!-- Coment?rio 1447761906 | Autor: Gabriel Onzi Benachio
n tem nome ainda...
-->
<!-- Coment?rio 1902009946 | Autor: Gabriel Onzi Benachio
não tem imagem ainda
-->
<!-- Coment?rio 789807565 | Autor: Gabriel Onzi Benachio
tentar fazer o word indexar automaticamente as figuras, pelo web não tá dando
-->
