---
tipo: qualidade
projeto: Bancada
atualizado: 2026-11-01
---

# Estratégia de testes

## Regra

**100% de linhas e 100% de ramos** em C# e em JavaScript. O build falha abaixo disso. Nenhuma fase termina sem cumprir.

Cobertura de 100% garante que cada linha **executou**, não que o teste **verifica** algo. Por isso o plano soma testes exaustivos de máquina de estados, de propriedade (ida e volta) e de segurança, descritos abaixo.

## Ferramentas

| Camada | Ferramenta | Licença |
|---|---|---|
| Testes C# | NUnit + NUnit3TestAdapter + Microsoft.NET.Test.Sdk | MIT |
| Cobertura C# | coverlet.msbuild, limite 100 linhas e ramos por projeto | MIT |
| Integração web | Microsoft.AspNetCore.Mvc.Testing (`WebApplicationFactory`) | MIT |
| Testes JS | `node --test` (Node 24) | MIT |
| Cobertura JS | nativa: `--experimental-test-coverage --test-coverage-lines=100 --test-coverage-branches=100 --test-coverage-functions=100` | MIT |
| Dublês | classes escritas à mão | — |

Os flags do Node foram conferidos na máquina (Node 24.14.1).

## Tipos de teste

| Tipo | O que cobre |
|---|---|
| **Unidade pura** | Domínio, protocolos, cálculos. Sem I/O |
| **Máquina de estados** | Todo par (estado, evento): permitido ou proibido. Gerado por laço |
| **Contrato de repositório** | SQL contra SQLite em memória de verdade, sem mock. Cada `CHECK`, `UNIQUE` e `FK` com violação deliberada |
| **Integração web** | Pipeline completo: roteamento, filtros, antifalsificação, cabeçalhos, autorização, Views renderizando |
| **Matriz de autorização** | Toda action × todo papel, esperado versus obtido |
| **Segurança** | Corpus de payloads (SQLi, XSS, travessia, CRLF, corpo gigante) em toda entrada |
| **Arquitetura** | Reflexão: setas de dependência, controller sem acesso a dados, ausência de `FromSqlRaw`/`ExecuteSqlRaw` com string montada |
| **Texto** | Paridade pt-BR/en e ausência de texto solto nas Views |
| **Licença** | Auditoria de todas as dependências ([[Licencas-MIT]]) |
| **Ida e volta** | Code 128, quadros ASTM e mensagens HL7: gerar, ler, comparar |
| **JavaScript** | Lógica pura direta; ligação com DOM contra DOM de mentira |

## O que fica fora da conta dos 100%

Lista fechada, cada item com justificativa, conferida por teste que falha se a lista crescer sem querer:

| Exclusão | Por quê | Compensação |
|---|---|---|
| Código gerado (`*.Designer.cs`, `RazorViews`) | Não é escrito por nós | Views exercitadas pelos testes de integração |
| Arquivos `.cshtml` | Coverlet não instrumenta Razor | Teste de integração renderiza cada View e confere o conteúdo |
| `Migrations/` e `ModelSnapshot` | Geradas pelo EF | Teste aplica todas em banco vazio, confere o esquema e que não há mudança de modelo pendente |
| `Program.cs` (apenas a raiz de composição) | Montagem | `WebApplicationFactory` sobe o app real |

Nada de `[ExcludeFromCodeCoverage]` espalhado. Se aparecer, o teste de exclusões falha.

## Execução

```
dotnet test /p:CollectCoverage=true /p:Threshold=100 /p:ThresholdType="line,branch" /p:CoverletOutputFormat=cobertura
node --test --experimental-test-coverage --test-coverage-lines=100 --test-coverage-branches=100 --test-coverage-functions=100 tests/js
```

Os dois geram resultado em formato que o [[Painel-Tecnico]] lê (Cobertura XML e saída do runner), sem biblioteca de relatório.

## Definição de pronto (toda fase)

1. Testes novos escritos antes ou junto, nunca depois.
2. 100% de linhas e ramos, C# e JS.
3. Checklist de segurança de [[Controles-e-SecPlus]] fechado.
3a. **Passada do Playwright no módulo** sem pendência ([[Verificacao-com-Playwright]]). Regra do Jonas: cada módulo é testado no navegador ao terminar.
4. Auditoria de licenças verde.
5. Vault atualizado (ADR, módulo, risco).

## Relacionadas

[[Plano-de-Testes-por-Modulo]] · [[Arquitetura-MVC]] · [[Licencas-MIT]] · [[Roadmap-Fases]]
