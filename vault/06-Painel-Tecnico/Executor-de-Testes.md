---
tipo: painel
projeto: Bancada
atualizado: 2026-11-01
---

# Executor de testes

Especificação do ponto mais sensível do projeto: rodar processo no servidor a pedido de qualquer visitante. Complementa [[Painel-Tecnico]]. Ameaças: AC-10 e AC-11 em [[Modelo-de-Ameacas]].

## O que roda ao vivo e o que vem do build

| Suíte | Ao vivo | Cobertura |
|---|---|---|
| **JavaScript** (`node --test`) | sim | **ao vivo**, nativa do Node |
| **C#** | sim: aprovados, reprovados, tempo, nomes | **do build**, rotulada "medida no commit X" (medir cobertura de C# exige `coverlet`, que roda no CI; ver spike abaixo) |

O painel diz isso na tela, com estas palavras. Prometer "cobertura ao vivo" para o C# seria falso.

## Como rodar testes de C# sem o SDK inteiro

O projeto de testes C# é publicado como **aplicativo de console com NUnitLite** (pacote MIT, executa NUnit sem `dotnet test`). O executor chama o binário já compilado. Vantagens: imagem menor, sem `dotnet` CLI exposto, sem compilar em produção.

**Spike da Fase 2 (confirmar antes de assumir):** NUnitLite na versão 4 executando os mesmos testes, emitindo XML de resultado que o painel lê, mantendo o `dotnet test` normal funcionando no CI. Se falhar, plano B: imagem com SDK.

## Contrato

Entrada do cliente: **apenas** `{ "suite": "csharp" | "js" }` e o token antifalsificação. Qualquer outro campo é ignorado (AC-11).

Comandos (constantes no código, nunca montados com entrada):

| Suíte | Comando | Diretório |
|---|---|---|
| `csharp` | `./testes/Bancada.Testes.Executavel --result=saida.xml` | `/app/testes` |
| `js` | `node --test --experimental-test-coverage ... tests/js` | `/app` |

## Salvaguardas (todas obrigatórias, todas testadas)

| # | Salvaguarda | Como |
|---|---|---|
| 1 | Comando constante | Sem concatenação; parâmetros de lista fixa |
| 2 | Usuário sem privilégio, ambiente limpo | Variáveis de ambiente do processo filho montadas do zero (só `PATH` mínimo e `HOME` vazio) |
| 3 | Diretório de trabalho fixo e somente leitura | Escrita só em `tmpfs` de saída |
| 4 | Uma execução por vez, global | `SemaphoreSlim(1)`; quem chega durante outra assiste a execução em curso |
| 5 | Limites de taxa | 1 por IP a cada 5 min; 10 por hora no total ([[Cabecalhos-e-Configuracao-Segura]]) |
| 6 | Tempo máximo | C#: 120 s; JS: 60 s; depois mata a **árvore** de processos |
| 7 | Saída limitada | 256 KB; passou disso, corta e avisa |
| 8 | Saída é texto não confiável | Escapada ao exibir, via `textContent` |
| 9 | Kill switch | `Bancada:Executor:Habilitado=false` → recusa e mostra o instantâneo |
| 10 | Sem rede para o processo filho | Sem credenciais, sem variáveis de proxy |
| 11 | Auditoria | `TestesIniciados`, `TestesConcluidos`, `TestesRecusados`, `ExecutorDesligado` |
| 12 | Isolamento do estado | Testes usam seus próprios bancos em memória; nada da sessão do visitante |

## Transmissão ao vivo

`GET /api/painel/testes/saida` devolve **eventos do servidor (SSE)**: linhas de saída conforme chegam e, no fim, um resumo (totais, falhas, tempo, mais lentos). Se o visitante abre durante uma execução de outro, assiste à mesma. Reconexão simples; sem WebSocket.

## Estados da tela

`Pronto` → `Rodando` (barra, saída, botão desabilitado) → `Concluído` (resumo) ou `Falhou` (mostra qual teste e a mensagem) ou `Recusado` (limite/kill switch, com o motivo e quando tenta de novo).

## Plano B (se a hospedagem não deixar subir processo)

Painel mostra só o instantâneo do build. Os testes de JavaScript podem rodar **no navegador do visitante** (carregando os módulos e um corredor de testes mínimo próprio), o que não toca o servidor. Decidir só se necessário.

## Testes do executor

- Parâmetro extra no corpo/consulta é ignorado (o processo roda o comando de sempre).
- Duas chamadas simultâneas: a segunda recebe a mesma execução, não cria outra.
- Estourar taxa devolve 429 e `TestesRecusados`.
- Processo que passa do tempo é morto (com um executável de teste que dorme).
- Saída de 10 MB é cortada em 256 KB.
- Saída com `<script>` aparece como texto.
- Kill switch desligado nunca inicia processo (provado com `IProcessos` falso que falha se chamado).
- O executor usa a porta `IProcessos`; o código que realmente cria processo é fino, e o teste de integração roda um executável de verdade.

## Relacionadas

[[Painel-Tecnico]] · [[Snapshot-do-Painel]] · [[Pipeline-CI-e-Container]]
