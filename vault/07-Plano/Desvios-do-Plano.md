---
tipo: plano
projeto: Bancada
atualizado: 2026-11-01
---

# Desvios do plano

O que ficou diferente do que o vault previa ao construir, e por quê. O vault descreve a intenção; esta nota descreve o que existe.

## Mudanças de desenho

| Plano | O que foi feito | Motivo |
|---|---|---|
| Datas relativas ao relógio real | Relógio do laboratório com data fixa (quarta, 04/11/2026, 10:15), que só anda com exame processando | A demonstração sai igual em qualquer dia e hora; a auditoria continua com o relógio real |
| Tabela `atendimento` | O agendamento acumula o atendimento depois do check-in | Um registro só, sem sincronizar dois estados |
| Tabelas de catálogo, equipamento e profissional | Estáticos em `Bancada.Dominio` | Não mudam em tempo de execução |
| Eventos do servidor (SSE) | Polling simples | Sem conexão presa; menos superfície |
| API JSON por módulo | Só `/painel/testes/estado` | As telas são renderizadas no servidor |
| Modais | Painel de detalhe na própria página (Post/Redirect/Get) | Funciona sem JS e cabe em 400 px |
| ViewModel separado por tela | As Views recebem os records dos casos de uso | Menos conversão sem ganho real numa demonstração |
| NUnitLite no executor | `dotnet test --no-build` | Sem pacote novo. Exige o SDK: na imagem chiseled o executor fica desligado e o painel mostra o instantâneo |
| Visualizador lê do disco | Arquivos embutidos no assembly | Sem caminho de disco, não há o que atravessar |
| 11 operadores, 4 semanas, 6 profissionais | 9 operadores, 2 semanas, 2 médicos | Suficiente para todas as regras |
| 6 cenários | 5 cenários; a alergia (PAC-0047) está em toda sessão | Não precisa de carga própria |
| RN-050 por sexo e idade | Por sexo | O catálogo não tem faixa por idade |
| Cadeia verificada na lista em memória | Verificação lê a tabela `Auditoria` | É a tabela que os gatilhos protegem |

## Não feito (pendente)

- Exportação FHIR e guia TISS ([[Ideias-Extras]]): propostas opcionais.
- Aba "Decisões" e cobertura por arquivo no painel.
- Atalhos de teclado no mapa da soroteca (M, D): o mapa navega por Tab e Enter.
- Catálogo de eventos: `EtiquetaGerada`, `AmostraRecebida`, `AmostraVencida`, `ExecucaoIniciada`, `ResultadoRecebido`, `RegraDeWestgardViolada`, `LiberacaoBloqueadaPorCQ`, `SessaoReiniciada`, `SessaoExpirada`, `TestesConcluidos` e `ExecutorDesligado` não são gravados.
- Corpus: roda nos campos de texto listados em `TelasTestes`, sem descoberta por reflexão nem arquivo JSON.
- `ForwardedHeaders` para o limite por IP atrás de proxy: entra com a hospedagem (Fase 9).

## Bugs que só o navegador e a integração pegaram

| Bug | Correção | Teste que protege |
|---|---|---|
| `<form action=...>` sem token antifalsificação (o tag helper só gera com `asp-*`) | `asp-antiforgery="true"` em todo POST | Guarda `Views_TodoFormularioQueEscreveLevaOTokenAntifalsificacao` |
| Byte nulo no formulário virava 500: a página de erro reexecutada lia o form de novo | `ErroController` sem parâmetro de action | `Corpus_EntradaHostilNuncaDa500` |
| Em servidor pt-BR, `0.1` virava `1` no formulário | Cultura invariante no processo | `Jornada_DoAgendamentoAoLaudo` |
| Tela do executor presa em "Rodando" quando os testes acabavam antes da primeira consulta | Compara com o estado inicial | `telas.test.js` |
| Tema não sobrevivia à troca de página | `localStorage` com tratamento de bloqueio | `tema.test.js` |

## Relacionadas

[[Checklists-por-Fase]] · [[Decisoes-em-Aberto]] · [[Roadmap-Fases]]
