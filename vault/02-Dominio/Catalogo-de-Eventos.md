---
tipo: dominio
projeto: Bancada
atualizado: 2026-11-01
---

# Catálogo de eventos de auditoria

Toda ação relevante grava um evento imutável na cadeia ([[Auditoria-Encadeada]]). A linha do tempo do paciente, a cadeia de custódia da amostra e a tela de eventos de segurança são **consultas sobre esta tabela**.

## Campos

| Campo | Conteúdo |
|---|---|
| `seq` | Sequência na sessão, a partir de 1 |
| `quando` | Hora (UTC) do relógio real |
| `quando_simulado` | Hora do relógio de simulação, se aplicável |
| `papel` | Papel de quem agiu |
| `acao` | Código do evento (tabela abaixo) |
| `alvo_tipo` / `alvo_id` | Entidade afetada (`Amostra`, `AMO-000123`) |
| `detalhe` | JSON pequeno e canônico, **sem texto livre do usuário** |
| `resultado` | `Ok`, `Recusado`, `Bloqueado` |
| `hash_anterior` / `hash` | Cadeia |

## Eventos

| Grupo | Códigos |
|---|---|
| Sessão | `SessaoCriada`, `PapelTrocado`, `OperadorTrocado`, `CenarioCarregado`, `SessaoReiniciada`, `SessaoExpirada` |
| Agenda | `AgendamentoCriado`, `AgendamentoReagendado`, `AgendamentoCancelado`, `AgendamentoConfirmado`, `FaltaRegistrada` |
| Atendimento | `CheckInFeito`, `AtendimentoIniciado`, `AtendimentoConcluido` |
| Prontuário | `EvolucaoRegistrada`, `EvolucaoCorrigida`, `AlergiaAlterada` |
| Pedido | `PedidoCriado`, `PedidoCancelado` |
| Amostra | `EtiquetaGerada`, `AmostraColetada`, `AmostraRecebida`, `AmostraRejeitada`, `AmostraArmazenada`, `AmostraMovida`, `AmostraDescartada`, `AmostraVencida` |
| Equipamento | `ExecucaoEnviada`, `ExecucaoIniciada`, `ExecucaoConcluida`, `ExecucaoComErro`, `ExecucaoReprocessada` |
| Qualidade | `ControleRegistrado`, `RegraDeWestgardViolada`, `LiberacaoBloqueadaPorCQ` |
| Resultado | `ResultadoRecebido`, `CriticoConfirmado`, `ResultadoValidado`, `LaudoLiberado`, `LaudoRetificado` |
| Auditoria | `CadeiaVerificada`, `AdulteracaoSimulada` |
| Segurança | `AutorizacaoRecusada`, `AntifalsificacaoInvalido`, `PayloadBloqueado`, `LimiteDeTaxaExcedido`, `CotaExcedida`, `IpBloqueado`, `CorpoGrandeDemais` |
| Painel | `TestesIniciados`, `TestesConcluidos`, `TestesRecusados`, `LabAtaqueUsado`, `ExecutorDesligado` |

## Regras

1. O `detalhe` guarda identificadores e valores técnicos (códigos, estados, tamanhos), **nunca** o texto que o visitante digitou.
2. Evento de segurança também sai no log estruturado, com o mesmo código.
3. O catálogo é uma enumeração; teste confere que **todo** código tem pelo menos um ponto do sistema que o grava (nenhum evento órfão) e que nenhuma ação sensível deixa de gravar.

## Relacionadas

[[Auditoria-Encadeada]] · [[Controles-e-SecPlus]] · [[Regras-de-Negocio]]
