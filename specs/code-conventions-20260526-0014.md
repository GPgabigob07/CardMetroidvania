# Code Conventions - 20260526-0014

## Contexto

Esta especificacao define convencoes de codigo para o prototipo Unity. Ela complementa:

- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/unity-script-asset-file-layout-20260526-0000.md`
- `specs/event-architecture-layout-20260526-0005.md`

## Principios

### SOLID sempre que possivel

Aplicar SOLID como criterio de decisao, sem transformar codigo simples em arquitetura cerimonial.

- Single Responsibility: componentes devem ter uma responsabilidade clara.
- Open/Closed: dados e variacoes devem preferir definicoes/configuracoes antes de `if` espalhado.
- Liskov Substitution: contratos devem permitir troca de implementacoes sem comportamento surpresa.
- Interface Segregation: preferir interfaces pequenas como `IDamageable`, `IInteractable` e `ICapabilityProvider`.
- Dependency Inversion: sistemas de alto nivel devem depender de contratos/eventos/dados, nao de componentes concretos sempre que possivel.

## Documentacao onsite

Todo metodo nao concreto deve conter documentacao XML onsite no estilo C#:

```csharp
/// <summary>
/// Descreve o que o metodo deve fazer e qual contrato ele estabelece.
/// </summary>
void Execute();
```

Esta regra se aplica a:

- metodos de interfaces;
- propriedades de interfaces quando definem contrato relevante;
- metodos abstratos;
- metodos virtuais usados como ponto de extensao por subclasses.

Nao e necessario documentar todo metodo privado simples. Prefira nomes claros e comentarios apenas quando eles reduzirem ambiguidade real.

## Simplicidade declarativa

Priorizar C# idiomatico e simples:

- usar `var` quando o tipo estiver claro pelo lado direito;
- usar `foreach` para iteracao de colecoes quando indice nao for necessario;
- usar propriedades somente-leitura para expor estado;
- usar inicializadores de colecao quando melhorarem legibilidade;
- preferir funcoes nativas/idiomaticas do C# e Unity antes de utilitarios customizados;
- evitar abstracoes antes de haver duplicacao ou complexidade real.

## Campos expostos ao Unity

Variaveis expostas no Inspector devem ser agrupadas e explicadas quando aplicavel:

- usar `[Header("...")]` para separar grupos logicos;
- usar `[Tooltip("...")]` em campos cujo significado nao seja obvio;
- usar atributos como `[Min]`, `[Range]`, `[TextArea]` e similares quando ajudarem autoria;
- manter campos `private` com `[SerializeField]`, expondo propriedades publicas somente-leitura quando necessario;
- nao expor campos publicos apenas para aparecer no Inspector.

Exemplo:

```csharp
[Header("Identity")]
[SerializeField]
[Tooltip("Stable id used by saves, gates and debug tooling. Falls back to asset name when empty.")]
private string id;
```

## Unity

Continuar respeitando a regra de script-assets:

- cada `MonoBehaviour` concreto em arquivo homonimo;
- cada `ScriptableObject` concreto em arquivo homonimo;
- nao agrupar multiplos script-assets concretos no mesmo arquivo.

## Testes e revisao

Ao adicionar codigo novo:

1. Checar se contratos/metodos nao concretos possuem XML docs.
2. Checar se campos do Inspector possuem agrupamento/editor annotations quando aplicavel.
3. Checar se a solucao nao adiciona acoplamento concreto desnecessario.
4. Compilar ou executar validacao equivalente quando possivel.

