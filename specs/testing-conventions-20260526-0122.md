# Testing Conventions - 20260526-0122

## Contexto

Esta especificacao complementa:

- `specs/code-conventions-20260526-0014.md`
- `specs/damage-system-sdd-20260526-0102.md`

Ela registra convencoes para testes EditMode/PlayMode em Unity.

## Regra: inicializacao explicita

Testes nao devem assumir que `Awake`, `Start`, `OnEnable` ou outro metodo do ciclo de vida Unity rodou no momento em que o componente foi criado.

Quando um teste cria um GameObject/componente que depende de estado inicial valido, ele deve acordar/inicializar explicitamente o sujeito antes da assercao ou da interacao testada.

Preferencia:

1. Usar um metodo publico de inicializacao/reset do proprio componente quando existir.
2. Usar um helper de teste como `CreateInitializedHealth`.
3. Evitar chamar lifecycle methods privados por reflexao ou `SendMessage`, exceto quando nao houver alternativa.

Exemplo:

```csharp
var health = target.AddComponent<SimpleHealth>();
health.Initialize();
```

## Motivo

Em EditMode tests, componentes podem ser criados sem passar pelo mesmo fluxo de cena observado em PlayMode. Assumir lifecycle automatico torna os testes frageis e pode criar objetos "DOA" com valores default, como vida atual zerada antes do primeiro dano.

## Aplicacao imediata

Os testes do `DamageResolver` devem inicializar todos os alvos `SimpleHealth` antes de resolver dano.

