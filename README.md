# Portfólio · Jonas Emanuel Moura Cavalcante

Desenvolvedor .NET (C#, ASP.NET MVC, SQL Server, Oracle). Este repositório reúne o meu currículo/portfólio e um projeto de demonstração que construí para mostrar como trabalho: o **Bancada**, um mini sistema de laboratório clínico (LIS) fictício, mas funcional.

- **`/`**: o portfólio (currículo, perfil, experiência, projetos, stack e contato). Página estática, sem login.
- **`/bancada`**: o sistema de laboratório. Quem entra já começa como **Admin**, vê todos os módulos e pode trocar de papel (Recepção, Médico, Coleta, Analista) pelo seletor verde no topo para ver o menu e as permissões mudarem.

> **Dados 100% fictícios. Simulação educacional: não é dispositivo médico e não deve ser usada em decisão clínica.**

## O que o Bancada tem

Agenda com modal de detalhes · fila e atendimento com resumo · prontuário com evolução cifrada · pedido de exame · coleta com etiqueta Code 128 · consulta de etiquetas · soroteca · equipamentos falando ASTM e HL7 · controle de qualidade (Westgard) · validação e laudo · auditoria encadeada por hash · operação (painéis e gráficos) · painel técnico que roda os próprios testes.

Cada visitante recebe um banco em memória só dele (SQLite), que some ao expirar a sessão. O relógio do laboratório é fictício e só anda com exame processando.

## Como rodar

Precisa do SDK .NET 9 e do Node 24.

```bash
# Linux/macOS/Git Bash
Bancada__CookiesSeguros=false ASPNETCORE_URLS=http://localhost:5099 dotnet run --project src/Bancada.Web -c Release
```

```powershell
# PowerShell
$env:Bancada__CookiesSeguros = 'false'; $env:ASPNETCORE_URLS = 'http://localhost:5099'; dotnet run --project src/Bancada.Web -c Release
```

Abra http://localhost:5099. Pelo Visual Studio, o perfil `Bancada.Web` abre em `https://localhost:50799` (HTTPS, com os cookies seguros ligados). `Bancada__CookiesSeguros=false` serve apenas para rodar sem HTTPS na própria máquina; fora disso o padrão é seguro.

Configuração opcional:

- `Bancada__PapelInicial` (padrão `Admin`) define o papel com que cada visitante começa.
- `Bancada__ProxiesConfiaveis` lista os IPs ou redes (CIDR) do proxy reverso da hospedagem, separados por vírgula. Só deles o `X-Forwarded-For` é aceito; sem a lista, o cabeçalho é ignorado. Atrás de proxy, sem essa configuração, todos os visitantes dividiriam o mesmo limite de taxa.
- `Bancada__Executor__Habilitado` liga o executor de testes do painel técnico. Fora do ambiente de desenvolvimento, ele nasce desligado.

## Como verificar

```powershell
pwsh tools/verificar.ps1
```

Os mesmos passos do CI: restauração travada, formatação, build com avisos como erro, testes de C# e de JavaScript com portão de **100% de linhas e ramos**, guardas de arquitetura e de licenças, e busca por pacotes vulneráveis. Os roteiros de teste de ponta a ponta com Playwright estão em `tools/e2e/`.

## Arquitetura

ASP.NET Core MVC em camadas, com guardas de teste que impedem dependências na direção errada:

`Bancada.Dominio` (regras, sem I/O) · `Bancada.Protocolos` (ASTM, HL7, Code 128, Westgard) · `Bancada.Aplicacao` (casos de uso) · `Bancada.Infraestrutura` (EF Core, SQLite, cifra AES-GCM) · `Bancada.Web` (MVC, JavaScript sem bibliotecas).

## Segurança

Tratada como requisito, não como acabamento: CSP sem `unsafe-inline`, cookie `__Host-` com antifalsificação por sessão, limite de taxa e bloqueio por IP, matriz de autorização com negação por padrão, erros genéricos, dados clínicos cifrados em repouso, auditoria encadeada por hash e testes com entradas hostis (SQL injection, XSS, travessia de caminho). O repositório não guarda senhas, tokens nem chaves: a chave de cifra nasce na memória de cada sessão.

A auditoria de segurança (ataques controlados, achados, correções e o que ainda depende da hospedagem) está em [`RELATORIO-AUDITORIA-CYBERSECURITY.md`](RELATORIO-AUDITORIA-CYBERSECURITY.md). Os ataques ficam como testes de regressão (`AtaquesTestes`, `SEC-xx`) e como roteiro caixa-preta em `tools/seguranca/ataques.mjs`.

## Documentação

O plano completo do projeto está em [`vault/`](vault/00-Indice.md) (notas em Markdown, abrem no Obsidian). O que mudou em relação ao plano está em `vault/07-Plano/Desvios-do-Plano.md`.

## Contato

[LinkedIn](https://www.linkedin.com/in/jonas-emanuel-moura) · [GitHub](https://github.com/jonasmoura247)
