# Bancada

Mini LIS de demonstração para o portfólio do Jonas. O plano completo está em `vault/` (Obsidian); comece por `vault/00-Indice.md` e `vault/07-Plano/Checklists-por-Fase.md`.

## Regras fixas

- ASP.NET Core MVC em camadas (Dominio, Protocolos, Aplicacao, Infraestrutura, Web), C#, JavaScript e SQL. **.NET 9** (SDK 9.0.102; migrar para o 10 LTS antes de publicar).
- **Tudo MIT, política estrita.** Única exceção: `SQLitePCLRaw` (Apache-2.0). Testes com NUnit e dublês escritos à mão. Sem ZAP, Stryker, xUnit, Moq, FluentAssertions.
- **100% de cobertura** de linhas e ramos (C# e JS); o build falha abaixo disso. Código novo nasce com teste.
- Segurança como requisito: ver `vault/04-Seguranca/`. Textos em pt-BR e en, nunca fixos nas Views.
- Dados 100% fictícios. Sem CPF.
- **Ao terminar cada módulo, testar com o Playwright** (`vault/05-Qualidade/Verificacao-com-Playwright.md`) e anotar em `Checklists-por-Fase.md`.
- **Git só no final.** Nenhum commit, push ou repositório sem o Jonas pedir.
- Verificação local: `tools/verificar` (mesmos passos do CI).
- Visual: `vault/03-Arquitetura/Design-do-Sistema.md` (azul-ardósia, três acentos suaves). O esboço aprovado está em `esboco/`.
