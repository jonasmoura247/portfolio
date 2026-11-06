---
tipo: seguranca
projeto: Bancada
atualizado: 2026-11-01
---

# Matriz de autorização

Fonte única de verdade: o enum `Acao` no código. Cada linha desta tabela vira uma linha no teste da matriz, que percorre **toda ação × todo papel** e confere resposta esperada (200/302 ou 403). Action sem `[Autorizar]` faz o teste de arquitetura falhar.

Papéis: **Rec** recepção · **Med** médico(a) · **Col** coleta · **Ana** analista · **Adm** admin. "sim" permite, "-" nega.

> Os papéis são de **demonstração**: qualquer visitante pode trocar para qualquer um. A matriz prova que a autorização funciona; ela **não** protege recursos sensíveis do servidor. Estes dependem de limite de taxa, comando fixo e kill switch, nunca do papel ([[Executor-de-Testes]]).

| Ação | Rec | Med | Col | Ana | Adm |
|---|---|---|---|---|---|
| **Sessão** | | | | | |
| TrocarPapel, TrocarOperador, CarregarCenario, ReiniciarSessao | sim | sim | sim | sim | sim |
| **Pacientes** | | | | | |
| ListarPacientes, VerPaciente | sim | sim | sim | sim | sim |
| **Agenda** | | | | | |
| VerAgenda | sim | sim | - | - | sim |
| Agendar, Reagendar, CancelarAgendamento, ConfirmarAgendamento | sim | - | - | - | sim |
| **Atendimento** | | | | | |
| VerFila | sim | sim | - | - | sim |
| FazerCheckIn | sim | - | - | - | sim |
| VerAtendimento, IniciarAtendimento, ConcluirAtendimento | - | sim | - | - | sim |
| **Prontuário** | | | | | |
| VerEvolucoes | - | sim | - | - | sim |
| RegistrarEvolucao, CorrigirEvolucao | - | sim | - | - | - |
| EditarAlergias | - | sim | sim | - | sim |
| **Pedido** | | | | | |
| CriarPedido | - | sim | - | - | sim |
| VerPedido | sim | sim | sim | sim | sim |
| CancelarPedido | - | sim | - | - | sim |
| **Coleta** | | | | | |
| ListarColetas, ColetarAmostra, ImprimirEtiqueta | - | - | sim | - | sim |
| RejeitarAmostra | - | - | sim | sim | sim |
| **Soroteca** | | | | | |
| VerSoroteca | - | - | sim | sim | sim |
| ArmazenarAmostra, MoverAmostra | - | - | sim | sim | sim |
| DescartarAmostra | - | - | - | sim | sim |
| **Equipamento e qualidade** | | | | | |
| VerEquipamento, VerQualidade | - | - | - | sim | sim |
| EnviarAoEquipamento, ControlarRelogio, ReprocessarExecucao, RegistrarControle | - | - | - | sim | sim |
| **Resultado e laudo** | | | | | |
| VerPendentes | - | - | - | sim | sim |
| ValidarResultado, ConfirmarCritico, LiberarLaudo, RetificarLaudo | - | - | - | sim | sim |
| VerLaudo | - | sim | - | sim | sim |
| **Auditoria** | | | | | |
| VerAuditoria | - | - | - | sim | sim |
| VerificarCadeia | - | - | - | sim | sim |
| **Painel e demonstração** | | | | | |
| VerPainel, ExecutarTestes, UsarLabAtaque | sim | sim | sim | sim | sim |

## Regras que a matriz não cobre (e têm teste próprio)

| Regra | Teste |
|---|---|
| Admin pode validar e liberar, **exceto** o que ele mesmo executou (RN-054) | `Admin_NaoValidaOQueExecutou` |
| Evolução só é escrita por médico, nem o admin (a matriz mostra "-") | `Admin_NaoEscreveEvolucao` |
| Recepção vê dados cadastrais do paciente, não evoluções | `Recepcao_NaoVeProntuario` |
| Menu mostra só o que o papel pode usar | `Menu_OcultaAcoesSemPermissao` |
| Ação recusada gera `AutorizacaoRecusada` na auditoria | `Recusa_GeraEvento` |

## Como o teste é montado

1. O enum `Acao` lista todas as ações.
2. Um atributo `[Autorizar(Acao.X)]` marca cada action.
3. O teste descobre por reflexão todas as actions e confere que cada uma tem o atributo (sem exceção para actions públicas além de `Erro` e `Saude`, em lista fechada).
4. Para cada ação e cada papel, faz a requisição real com `WebApplicationFactory` e compara com a tabela embutida no teste.

## Relacionadas

[[Personas-e-Jornada]] · [[Rotas-e-Controllers]] · [[Controles-e-SecPlus]]
