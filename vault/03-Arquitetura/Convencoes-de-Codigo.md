---
tipo: arquitetura
projeto: Bancada
atualizado: 2026-11-01
---

# Convenções de código

Regras curtas que valem para o projeto inteiro. Quem faz cumprir é o compilador, os analisadores e os testes, não a boa vontade.

## Nomes

| Item | Regra | Exemplo |
|---|---|---|
| Domínio e negócio | pt-BR, sem abreviação | `Agendamento`, `ColetarAmostra`, `PosicaoDaSoroteca` |
| Termos de framework | inglês como vêm | `DbContext`, `Controller`, `Task` |
| Tabelas e colunas | pt-BR, singular, snake_case | `soroteca_posicao`, `criado_em` |
| Testes | `Metodo_Cenario_Resultado` | `Agendar_HorarioOcupado_LancaConflitoDeAgenda` |
| JavaScript | pt-BR para domínio, camelCase; arquivos em kebab-case | `montarGrade`, `grade-agenda.js` |
| Textos de tela | só em recursos, chave `Modulo.Elemento.Acao` | `Agenda.Botao.Agendar` |

## C#

- `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, analisadores da própria SDK (MIT) em nível máximo.
- `Directory.Build.props` com essas regras, mais `Directory.Packages.props` (versões centralizadas) e `packages.lock.json` (restauração travada).
- Classes `sealed` por padrão; `record` para objetos de valor; construtores que garantem invariantes (objeto nunca nasce inválido).
- Namespaces de arquivo (`namespace X;`). Sem `region`. Sem comentário que repete o código; comentário só explica o porquê.
- Métodos pequenos, um nível de abstração por método, no máximo 3 parâmetros (acima disso, objeto de comando).
- Tempo sempre `DateTimeOffset` em UTC, obtido de `IRelogio`. Proibido `DateTime.Now`, `DateTime.UtcNow`, `Random` e `Guid.NewGuid()` fora da Infraestrutura (teste de arquitetura).
- Sem `static` com estado. Sem service locator. Injeção por construtor.

## Erros

| Tipo | Quando | Resposta HTTP |
|---|---|---|
| `ViolacaoDeRegra` (base) | Regra de negócio violada: transição inválida, conflito de horário | 409 (ou 422 para dado inválido) com mensagem de recurso |
| `NaoEncontrado` | Entidade não existe **na sessão** | 404 |
| `AcessoNegado` | Papel sem permissão | 403 |
| Outras | Defeito | 500 com página genérica; detalhe só no log estruturado |

Um filtro único traduz essas exceções; controller não tem `try/catch`. Mensagem nunca revela estrutura interna.

## Controller

```csharp
[HttpPost, ValidateAntiForgeryToken, Autorizar(Acao.Agendar)]
public IActionResult Agendar(AgendarModelo modelo)
{
    var resultado = _agendar.Executar(modelo.ParaComando());
    return RedirectToAction(nameof(Semana), new { resultado.Semana });
}
```

Valida modelo (atributos/`IValidatableObject`), chama **um** caso de uso, devolve. Nada mais.

## Validação

Duas camadas: o **modelo de entrada** valida formato e tamanho (rápido, perto da borda); o **domínio** valida regra (autoridade final). Toda string de entrada tem tamanho máximo declarado.

## Idiomas

Recursos `.resx` ou JSON mestre com classe tipada (decidir na Fase 0, o Bestia usou JSON mestre + classe `Textos` e deu certo). Testes: **paridade** pt-BR/en, **texto solto** nas Views, **chave não usada**.

## Logs

Log estruturado (`ILogger` com templates). Regras: nunca registrar texto livre do usuário nem chave; sempre registrar `sessaoId` (truncado), `papel`, `acao`, `resultado`. Entrada do usuário nunca vai dentro da mensagem do template (evita injeção de linha).

## Git

Commits pequenos, mensagem no imperativo em português, um assunto por commit. Nenhum commit sem o Jonas pedir.

## Relacionadas

[[Arquitetura-MVC]] · [[Estrategia-de-Testes]] · [[Design-do-Sistema]]
