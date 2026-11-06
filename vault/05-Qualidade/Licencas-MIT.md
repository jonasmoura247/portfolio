---
tipo: qualidade
projeto: Bancada
atualizado: 2026-11-01
---

# Licenças: só MIT

## Política proposta

1. Todo pacote em `*.csproj`, `package.json` (se um dia existir) e todo arquivo copiado para `wwwroot` tem licença **MIT**.
2. Exceções são **explícitas, uma por linha, com motivo**. Nenhuma entra sem o Jonas aceitar.
3. **Estrito** (decidido em 01/11/2026): vale também para ferramentas de apoio. Nada de OWASP ZAP nem Stryker.NET. Docker e hospedagem são infraestrutura de entrega e ficam fora da regra, salvo decisão em contrário ([[Decisoes-em-Aberto]]).

## Auditoria como teste

Teste lê `obj/project.assets.json` de cada projeto, resolve o `.nuspec` de cada pacote (incluindo transitivos), extrai a expressão de licença e compara com a lista aceita. Falha se aparecer licença nova ou pacote sem licença declarada. O Bestia já faz isso; a ideia é reaproveitar.

O painel técnico mostra o resultado: pacote, versão, licença, quem o puxou.

## Já conferido (precisa reconfirmar no momento da inclusão)

| Pacote / componente | Licença | Veredito |
|---|---|---|
| .NET 9 runtime, ASP.NET Core, Microsoft.Extensions.* | MIT | ok |
| NUnit, NUnit3TestAdapter, NUnit.Analyzers | MIT | ok |
| Microsoft.NET.Test.Sdk | MIT | ok |
| coverlet.msbuild | MIT | ok |
| Microsoft.AspNetCore.Mvc.Testing | MIT | ok |
| Microsoft.EntityFrameworkCore, .Sqlite, .Design | MIT | ok |
| Microsoft.Data.Sqlite (puxado pelo EF Core Sqlite) | MIT | ok |
| SQLitePCLRaw (puxado pelo Microsoft.Data.Sqlite) | Apache-2.0 | **exceção aceita pelo Jonas em 01/11/2026**; é a única, listada no teste de auditoria |
| SQLite (a biblioteca) | domínio público | ok, sem obrigações |
| Node.js (só para testes de JS e executor) | MIT | ok |

## Evitar de propósito

| Item | Motivo |
|---|---|
| xUnit | Apache-2.0 |
| Moq, NSubstitute | BSD-3 |
| FluentAssertions 8+ | licença comercial |
| ReportGenerator | Apache-2.0 (substituído por leitor próprio do XML) |
| Bogus | usaríamos só nomes; gerador próprio basta |
| jsdom, bibliotecas de gráfico, frameworks JS/CSS | árvore de licenças mistas ou desnecessário |
| Fontes do Google | licença OFL, não MIT; fontes do sistema resolvem |
| Markdig (renderizar o vault) | BSD-2; o painel mostra o vault pré-renderizado ou como texto |

## Ferramentas de apoio

Política estrita: ficam de fora OWASP ZAP e Stryker.NET (Apache-2.0). A validação de segurança vem do corpus de testes próprio ([[Modelo-de-Ameacas]], casos AC-1 a AC-12).

## Relacionadas

[[Estrategia-de-Testes]] · [[Decisoes-ADR]]
