---
tipo: arquitetura
projeto: Bancada
atualizado: 2026-11-01
---

# Rotas e controllers

Páginas renderizadas no servidor (Razor). O JavaScript só **melhora** a tela, buscando dados em rotas `/api/...` (controllers separados, JSON). Toda rota que altera estado é `POST`/`PUT`/`DELETE` com token antifalsificação. Toda action declara `[Autorizar(Acao.X)]`, e a lista de ações é a mesma da [[Matriz-de-Autorizacao]].

## Páginas (MVC)

| Controller | Rota | Verbo | Ação (policy) |
|---|---|---|---|
| `InicioController` | `/` | GET | tour, escolha de papel e cenário |
| `SessaoController` | `/sessao/papel` | POST | `TrocarPapel` |
| | `/sessao/operador` | POST | `TrocarOperador` |
| | `/sessao/cenario` | POST | `CarregarCenario` |
| | `/sessao/reiniciar` | POST | `ReiniciarSessao` |
| `PacientesController` | `/pacientes` | GET | `ListarPacientes` |
| | `/pacientes/{id}` | GET | `VerPaciente` |
| `AgendaController` | `/agenda?profissional=&semana=` | GET | `VerAgenda` |
| | `/agenda/agendar` | POST | `Agendar` |
| | `/agenda/{id}/reagendar` | POST | `Reagendar` |
| | `/agenda/{id}/cancelar` | POST | `CancelarAgendamento` |
| | `/agenda/{id}/confirmar` | POST | `ConfirmarAgendamento` |
| `AtendimentosController` | `/atendimentos` | GET | `VerFila` |
| | `/atendimentos/{agendamentoId}/checkin` | POST | `FazerCheckIn` |
| | `/atendimentos/{id}` | GET | `VerAtendimento` |
| | `/atendimentos/{id}/iniciar` | POST | `IniciarAtendimento` |
| | `/atendimentos/{id}/concluir` | POST | `ConcluirAtendimento` |
| `ProntuarioController` | `/prontuario/{pacienteId}` | GET | `VerEvolucoes` |
| | `/prontuario/{pacienteId}/evolucao` | POST | `RegistrarEvolucao` |
| | `/prontuario/evolucao/{id}/corrigir` | POST | `CorrigirEvolucao` |
| | `/prontuario/{pacienteId}/alergia` | POST | `EditarAlergias` |
| `PedidosController` | `/pedidos/novo?atendimento=` | GET/POST | `CriarPedido` |
| | `/pedidos/{id}` | GET | `VerPedido` |
| | `/pedidos/{id}/cancelar` | POST | `CancelarPedido` |
| `ColetasController` | `/coletas` | GET | `ListarColetas` |
| | `/coletas/{pedidoId}/coletar` | POST | `ColetarAmostra` |
| | `/amostras/{id}/rejeitar` | POST | `RejeitarAmostra` |
| | `/amostras/{id}/etiqueta` | GET | `ImprimirEtiqueta` |
| `SorotecaController` | `/soroteca` | GET | `VerSoroteca` |
| | `/soroteca/armazenar` | POST | `ArmazenarAmostra` |
| | `/soroteca/mover` | POST | `MoverAmostra` |
| | `/soroteca/descartar` | POST | `DescartarAmostra` |
| `EquipamentosController` | `/equipamentos` | GET | `VerEquipamento` |
| | `/equipamentos/{id}/enviar` | POST | `EnviarAoEquipamento` |
| | `/equipamentos/relogio` | POST | `ControlarRelogio` |
| | `/execucoes/{id}/reprocessar` | POST | `ReprocessarExecucao` |
| `QualidadeController` | `/qualidade` | GET | `VerQualidade` |
| | `/qualidade/medicao` | POST | `RegistrarControle` |
| `ResultadosController` | `/resultados` | GET | `VerPendentes` |
| | `/resultados/{id}/validar` | POST | `ValidarResultado` |
| | `/resultados/{id}/confirmar-critico` | POST | `ConfirmarCritico` |
| `LaudosController` | `/laudos/{id}` | GET | `VerLaudo` |
| | `/laudos/{pedidoId}/liberar` | POST | `LiberarLaudo` |
| | `/laudos/{id}/retificar` | POST | `RetificarLaudo` |
| `AuditoriaController` | `/auditoria` | GET | `VerAuditoria` |
| | `/auditoria/verificar` | POST | `VerificarCadeia` |
| `PainelController` | `/painel/...` | GET | `VerPainel` |
| | `/painel/testes/executar` | POST | `ExecutarTestes` |
| `LabAtaqueController` | `/lab-ataque` | GET/POST | `UsarLabAtaque` |
| `ErroController` | `/erro/{codigo}` | GET | público |
| `SaudeController` | `/saude` | GET | público (health check, sem detalhe) |

## API JSON (para o JavaScript)

| Rota | Retorna |
|---|---|
| `GET /api/pacientes?busca=&pagina=` | página de pacientes |
| `GET /api/agenda/grade?profissional=&semana=` | grade e agendamentos da semana |
| `GET /api/soroteca/mapa?caixa=` | posições da caixa |
| `GET /api/equipamentos/{id}/estado` | fila, status, quadros do protocolo |
| `GET /api/execucoes/eventos` | fluxo de eventos do servidor (SSE) com o andamento |
| `GET /api/qualidade/serie?equipamento=&exame=` | medições do controle |
| `GET /api/painel/testes/saida` | fluxo (SSE) da execução de testes |
| `GET /api/painel/instantaneo` | último resultado gravado no build |

Respostas de erro seguem `ProblemDetails` (tipo, título, status), sem detalhe interno.

## ViewModels

Um modelo de **entrada** e um de **saída** por tela (`AgendarModelo`, `SemanaViewModel`). A View nunca recebe entidade do domínio. Conversão em métodos estáticos pequenos e testados (`ParaComando`, `DeDominio`).

## Regras de pipeline (ordem)

1. Cabeçalhos de segurança · 2. limite de taxa · 3. sessão · 4. antifalsificação (verbos de escrita) · 5. autorização por ação · 6. controller · 7. filtro de exceções · 8. auditoria da ação.

## Relacionadas

[[Camada-de-Aplicacao]] · [[Matriz-de-Autorizacao]] · [[Cabecalhos-e-Configuracao-Segura]]
