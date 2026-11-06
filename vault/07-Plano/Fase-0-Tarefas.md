---
tipo: plano
projeto: Bancada
atualizado: 2026-11-01
---

# Fase 0 · tarefas em ordem

O primeiro dia de código. Cada passo é pequeno e verificável. **Nada de commit ou push sem o Jonas pedir.**

## 0. Ambiente

1. Nada a instalar: o SDK **9.0.102** e o Node **24.14.1** já estão na máquina (conferido em 01/11). O SDK 8.0.405 também está, mas o projeto usa o 9.
2. `global.json` na raiz fixando `9.0.102` com `rollForward: latestFeature`, para o `dotnet` não cair no 8 por engano.
3. Conferir que `dotnet --version` dentro da pasta do projeto responde `9.0.x`.

## 1. Esqueleto

4. `Bancada.sln` e os projetos de [[Arquitetura-MVC]], apontando para `net9.0`.
5. `Directory.Build.props`: `Nullable`, `TreatWarningsAsErrors`, `AnalysisLevel` máximo, `ImplicitUsings` desligado nos projetos de domínio.
6. `Directory.Packages.props` com versões centralizadas e `RestorePackagesWithLockFile` ligado; gerar `packages.lock.json`.
7. `.editorconfig` ([[Convencoes-de-Codigo]]).
8. `.gitignore` (bin, obj, TestResults, cobertura, `.vs`).

## 2. Guardas (antes de escrever qualquer regra de negócio)

9. Teste de **arquitetura**: setas de dependência; ausência de `DateTime.Now/UtcNow`, `Random`, `Guid.NewGuid` fora da Infraestrutura; ausência de `FromSqlRaw/ExecuteSqlRaw` com string montada; ausência de `[ExcludeFromCodeCoverage]`; toda action com `[Autorizar]`.
10. Teste de **licenças**: lê `project.assets.json`, resolve licença de cada pacote, compara com a lista; exceção única do `SQLitePCLRaw`.
11. Teste de **exclusões** de cobertura (lista fechada).
12. Teste de **idiomas**: paridade pt-BR/en, texto solto, chave não usada.
13. Limite de **100%** configurado em cada projeto de teste (`coverlet.msbuild`), com um teste mínimo passando para provar que o gate funciona (e um commit local de prova de que ele **falha** quando a cobertura cai).

## 3. JavaScript

14. `tests/js/_apoio/dom-falso.js` e primeiro teste.
15. Teste-guarda que importa todo `.js` de `wwwroot/js`.
16. Script de execução com os limites de 100% (`--test-coverage-include` conferido na versão instalada).
17. Teste que proíbe `innerHTML`, `outerHTML`, `insertAdjacentHTML`, `document.write`, `eval`, `new Function`.

## 4. Web mínimo

18. `Program.cs` mínimo (raiz de composição) e uma View vazia.
19. `WebApplicationFactory` funcionando e o primeiro teste de integração (`GET /saude` devolve 200).
20. Cabeçalhos de segurança básicos já nesta fase, com teste (o resto vem na Fase 1).

## 5. CI e container

21. `tools/verificar` (script local) que roda, nesta ordem, os passos 2 a 8 de [[Pipeline-CI-e-Container]]: restauração travada, formatação, build com avisos como erro, testes C# com 100%, testes JS com 100%, pacotes vulneráveis. É o "CI de mentira" até o Git existir. Escrever também o `.github/workflows/ci.yml` com os mesmos passos, **sem rodar** (só vale no GitHub, no final).
22. `Dockerfile` + teste de requisitos.
23. README com instruções de rodar localmente e selo do CI.

## 6. Fechamento

24. Rodar tudo local: `dotnet test` (100%), `node --test` (100%), `dotnet list package --vulnerable`.
25. Atualizar o vault: status no [[00-Indice]], ADRs que mudaram, checklist da fase marcado.
26. (Git fica para o final, decisão de 01/11.) Ao criar o repositório público: rodar `tools/verificar` uma última vez, varrer o histórico atrás de segredo, fazer o primeiro push e confirmar que o CI roda de verdade no GitHub.

## Armadilhas já conhecidas

- O coverlet só mede assemblies carregados pelo teste; confirmar que um arquivo sem teste aparece com 0% (e derruba o gate).
- Gate de 100% em projeto quase vazio passa por vazio: o teste de exclusões e o teste-guarda de JS existem para isso não esconder arquivo.
- `Directory.Packages.props` + restauração travada exige regenerar o lock a cada pacote novo.
- Conferir o nome exato da opção `--test-coverage-include` e do limite de ramos no Node instalado antes de escrever o script.

## Relacionadas

[[Checklists-por-Fase]] · [[Roadmap-Fases]] · [[Decisoes-ADR]]
