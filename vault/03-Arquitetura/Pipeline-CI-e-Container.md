---
tipo: arquitetura
projeto: Bancada
atualizado: 2026-11-01
---

# Pipeline de CI e container

## Pipeline (GitHub Actions)

`.github/workflows/ci.yml`, em todo push e pull request. Falha em qualquer passo derruba o pipeline.

| # | Passo | Falha quando |
|---|---|---|
| 1 | Checkout, instalar .NET 9 e Node 24 | — |
| 2 | `dotnet restore --locked-mode` | `packages.lock.json` desatualizado |
| 3 | `dotnet format --verify-no-changes` | Formatação fora do `.editorconfig` |
| 4 | `dotnet build -c Release` (avisos como erro) | Qualquer aviso |
| 5 | `dotnet test` com cobertura e limite de 100% (linhas e ramos) | Cobertura < 100% ou teste vermelho |
| 6 | `node --test` com cobertura e limite de 100% | Idem |
| 7 | Teste de auditoria de licenças (parte do passo 5) | Licença fora da lista, ou pacote sem licença |
| 8 | `dotnet list package --vulnerable --include-transitive` | Qualquer pacote vulnerável |
| 9 | Gerar `painel-instantaneo.json` (`tools/`) | Falha ao gerar |
| 10 | Publicar artefatos (instantâneo, cobertura) | — |

As ações usadas (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`) são MIT. O CI do Bestia nunca rodou no GitHub; neste projeto ele roda desde a Fase 0, e o selo de status vai no README.

## Configuração

`appsettings.json` com padrões seguros; segredos e opções por variável de ambiente. Nenhum segredo no repositório (o app quase não tem: a chave de cifra nasce na sessão).

| Chave | Padrão | Função |
|---|---|---|
| `Bancada:Executor:Habilitado` | `true` | Kill switch do executor de testes |
| `Bancada:LabAtaque:Habilitado` | `true` | Kill switch do laboratório de ataque |
| `Bancada:Sessao:OciosoMinutos` | 30 | TTL ocioso |
| `Bancada:Sessao:TotalMinutos` | 120 | TTL total |
| `Bancada:Sessao:Maximo` | 200 | Sessões simultâneas |
| `Bancada:Limites:*` | ver [[Cabecalhos-e-Configuracao-Segura]] | Limites de taxa e cota |
| `Bancada:Relogio:FatorPadrao` | 60 | Aceleração da simulação |

Teste de integração confere que o padrão de cada chave é o seguro.

## Container

Requisitos do `Dockerfile` (multi-estágio):

1. Estágio de build com SDK 9 e Node 24: restaura, compila, testa, publica.
2. Estágio final com **runtime** do ASP.NET e o mínimo para o executor ([[Executor-de-Testes]]).
3. Roda como **usuário sem privilégio**, sistema de arquivos raiz **somente leitura**, diretório temporário em memória (`tmpfs`) para o executor.
4. Imagem **chiseled** (sem shell nem gerenciador de pacotes), sem ferramentas de rede.
5. Health check feito pela hospedagem em `GET /saude` (a imagem chiseled não tem shell nem curl para um `HEALTHCHECK` interno). A Fase 2 troca a imagem final para incluir o executor de testes e reavalia isto.
6. Uma porta exposta; TLS terminado na frente (hospedagem), com HSTS no app.
7. Rótulos da imagem com versão e hash do commit.

Teste do `Dockerfile`: um teste simples lê o arquivo e confere os requisitos objetivos (usuário sem privilégio (`USER $APP_UID`), restauração travada, sem tag `latest` e nenhuma instrução `ADD` de URL).

## Observabilidade

`/saude` (público, sem detalhe), log estruturado no console, métricas de `System.Diagnostics.Metrics` (sessões ativas, requisições bloqueadas, execuções de teste) exibidas na tela "Operação" do [[Painel-Tecnico]]. Sem serviço externo de monitoramento.

## Backup e recuperação

Sem estado persistente: **RPO zero** (nada a perder) e **RTO** igual ao tempo de subir o container. Runbook em [[Runbook-de-Incidente]].

## Relacionadas

[[Estrategia-de-Testes]] · [[Roadmap-Fases]] · [[Executor-de-Testes]]
