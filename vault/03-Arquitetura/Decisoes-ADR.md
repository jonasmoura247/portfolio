---
tipo: adr
projeto: Bancada
atualizado: 2026-11-01
---

# Decisões de arquitetura

Formato: contexto, decisão, alternativas, consequência. O painel técnico exibe esta nota ([[Painel-Tecnico]]).

## ADR-001 · .NET 9

- **Contexto:** a máquina tem SDK 8.0.405 e 9.0.102; o 10 não está instalado (a instalação foi cancelada pelo Windows).
- **Decisão:** `net9.0`, com o SDK 9 que já existe (decidido pelo Jonas em 01/11/2026). Pacotes Microsoft na linha 9.x.
- **Risco aceito:** o .NET 9 é de suporte padrão e, pelo calendário da Microsoft, sai de suporte em 10/11/2026, dez dias depois da decisão. Não há problema para desenvolver e demonstrar, mas o projeto publicado passaria a rodar em versão sem correções de segurança.
- **Plano de saída:** migrar para o .NET 10 LTS antes da Fase 9 (publicação). A troca é pequena: `TargetFramework`, `global.json`, imagem do container e subir os pacotes 9.x para 10.x; os 100% de testes provam que nada quebrou. Tarefa registrada no [[Checklists-por-Fase]].
- **Alternativa descartada:** começar no .NET 10 (exigia instalar o SDK agora).

## ADR-002 · SQLite, um banco em memória por visitante

- **Contexto:** a demo precisa de SQL relacional, isolamento entre visitantes e custo zero de operação.
- **Decisão:** SQLite com `Microsoft.Data.Sqlite`. Banco-modelo copiado para memória em cada sessão.
- **Alternativas:** PostgreSQL (licença própria, parecida com MIT; exige servidor e esquema por sessão); SQL Server (não é MIT, já tratado como exceção no Bestia).
- **Consequência:** o SQLite é domínio público, mas o conector depende do `SQLitePCLRaw`, que é Apache-2.0. Exceção registrada em [[Licencas-MIT]] e **aceita pelo Jonas em 01/11/2026**.

## ADR-003 · EF Core (code-first, migrations), SQL cru só onde agrega

- **Contexto:** o padrão de mercado em .NET e o que as vagas pedem é EF Core. O banco aqui é efêmero (nasce de um modelo a cada sessão), então não há dado de produção para evoluir; migrations seguem valendo como sinal de portfólio.
- **Decisão:**
  - EF Core (MIT) com SQLite. Entidades do domínio **sem atributo de EF**; mapeamento por `IEntityTypeConfiguration` na Infraestrutura.
  - **Sem repositório genérico** (`IRepository<T>`): o `DbContext` já é repositório e unidade de trabalho. Cada caso de uso depende de uma **porta pequena e específica** (`IAgendaDaSemana`, `IPedidosPendentes`...) implementada na Infraestrutura com EF.
  - Migrations geradas pelo EF. Onde o EF não expressa, o SQL aparece na própria migration: `CHECK` (`HasCheckConstraint`), gatilhos da auditoria e views via `migrationBuilder.Sql`.
  - Relatórios e painel operacional com SQL cru **parametrizado** (`FromSql` interpolado, que vira parâmetro). `FromSqlRaw`/`ExecuteSqlRaw` com concatenação é proibido por teste de arquitetura.
- **Alternativas:** Dapper (Apache-2.0, e o SQL ficaria espalhado em strings); ADO.NET e DbUp (mais código sem ganho de leitura); `EnsureCreated` sem migrations (funciona, porque o banco é efêmero, mas perde o sinal).
- **Consequência:** menos código, SQL injection evitada por construção, mapeamento testável contra SQLite real (sem mock de banco). Pasta `Migrations` e `ModelSnapshot` ficam fora dos 100% (gerados), compensados por um teste que aplica tudo em banco vazio e confere que o modelo não tem mudança pendente.

## ADR-004 · NUnit e fakes escritos à mão

- **Contexto:** xUnit é Apache-2.0; Moq é BSD-3; FluentAssertions a partir da v8 é comercial.
- **Decisão:** NUnit (MIT) com as asserções nativas. Dublês são classes pequenas escritas no projeto de teste.
- **Cobertura:** `coverlet.msbuild` (MIT) com limite de 100%.

## ADR-005 · JavaScript sem dependência

- **Decisão:** módulos ES nativos; testes com `node --test` e cobertura nativa do V8 com limite de 100%; DOM de mentira escrito à mão.
- **Alternativas rejeitadas:** jsdom (árvore grande de dependências com licenças mistas), React/Vue (peso e licenças a auditar sem ganho para o objetivo).

## ADR-006 · Sem contas, sem upload, sem saída de rede

- **Decisão:** papéis simulados na sessão; nenhum campo de senha, arquivo ou e-mail; o servidor não faz requisições externas.
- **Consequência:** some a superfície de credenciais, de arquivos maliciosos e de SSRF.

## ADR-007 · CSP estrita sem inline

- **Decisão:** `default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; font-src 'self'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'`. Zero script e zero estilo inline. Teste de integração confere o cabeçalho e outro varre as views atrás de `style=` e `<script>` sem `src`.

## ADR-008 · Execução de testes pelo painel

- **Decisão:** processo filho com comando fixo, sem parâmetro vindo do cliente, uma execução por vez, limite de taxa, tempo máximo, saída limitada, desligável por configuração. Detalhes e riscos em [[Painel-Tecnico]].

## ADR-009 · Sem CPF nem dado pessoal

- **Decisão:** paciente identificado por código fictício. Ver [[Dados-Ficticios-e-LGPD]].

## ADR-010 · pt-BR e en

- **Decisão:** dois idiomas, com teste de paridade. Espanhol fica fora (decisão em aberto).

## ADR-011 · Auditoria encadeada por hash

- **Decisão:** cada evento guarda `SHA-256(hash_anterior ‖ conteúdo canônico)`. Tabela só aceita inclusão (gatilhos). Tela permite verificar a cadeia e simular uma adulteração para ver a detecção.

## Relacionadas

[[Arquitetura-MVC]] · [[Decisoes-em-Aberto]]
