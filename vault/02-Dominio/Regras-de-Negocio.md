---
tipo: dominio
projeto: Bancada
atualizado: 2026-11-01
---

# Regras de negócio

Catálogo com identificador estável. Cada regra aponta o lugar onde vive e o **nome do teste que a prova** (rastreabilidade). Mudou a regra, muda o teste com o mesmo identificador.

Convenção do teste: `RN_###_Descricao` na classe do agregado.

## Agenda e atendimento

| ID | Regra | Onde | Teste |
|---|---|---|---|
| RN-001 | Um horário da grade admite no máximo um agendamento | `Agendamento`, UNIQUE em `grade_id` | `RN_001_HorarioOcupadoRecusaSegundaReserva` |
| RN-002 | Não agendar no passado | `Agendamento` | `RN_002_PassadoRecusado` |
| RN-003 | Só agendar em horário existente na grade do profissional | `Agenda` | `RN_003_ForaDaGradeRecusado` |
| RN-004 | Cancelar libera o horário | `Agendamento` | `RN_004_CancelarLiberaHorario` |
| RN-005 | Agendado/Confirmado cujo horário passou vira Faltou | `MarcarFaltas` | `RN_005_HorarioPassadoViraFaltou` |
| RN-006 | Check-in só no dia do agendamento | `Agendamento` | `RN_006_CheckInForaDoDiaRecusado` |
| RN-007 | Só o profissional do horário inicia o atendimento | `Atendimento` | `RN_007_OutroProfissionalRecusado` |
| RN-008 | Transições de estado só pelas arestas permitidas | todas as máquinas | `RN_008_TodasAsTransicoes` (exaustivo) |

## Prontuário

| ID | Regra | Onde | Teste |
|---|---|---|---|
| RN-010 | Evolução não pode ser editada nem apagada | `Evolucao`, gatilho | `RN_010_EvolucaoImutavel` |
| RN-011 | Correção cria nova evolução que referencia a anterior; ambas aparecem | `Prontuario` | `RN_011_CorrecaoEncadeada` |
| RN-012 | Texto da evolução: 1 a 2.000 caracteres, sem HTML | `Evolucao` | `RN_012_TamanhoELimites` |
| RN-013 | Alergia ativa gera alerta em qualquer pedido do paciente | `Pedido` | `RN_013_AlergiaGeraAlerta` |

## Pedido e coleta

| ID | Regra | Onde | Teste |
|---|---|---|---|
| RN-020 | Pedido tem de 1 a 20 exames, sem repetição | `Pedido` | `RN_020_LimitesEUnicidade` |
| RN-021 | Exames do mesmo material formam um tubo só | `Pedido` | `RN_021_AgrupaPorMaterial` |
| RN-022 | Coleta com jejum exige confirmação do maior período exigido | `Coleta` | `RN_022_JejumExigeConfirmacao` |
| RN-023 | Volume coletado ≥ volume mínimo do tubo | `Amostra` | `RN_023_VolumeInsuficiente` |
| RN-024 | Etiqueta é única (`AMO-######`) | `Amostra`, UNIQUE | `RN_024_EtiquetaUnica` |
| RN-025 | Rejeição exige motivo (hemólise, volume, material errado, etiqueta ilegível) e gera pedido de recoleta | `Amostra` | `RN_025_RejeicaoGeraRecoleta` |

## Soroteca

| ID | Regra | Onde | Teste |
|---|---|---|---|
| RN-030 | Uma posição guarda no máximo uma amostra | domínio e UNIQUE | `RN_030_PosicaoUnica` |
| RN-031 | Só soro vai para a soroteca; sangue total é descartado após validação | `Soroteca` | `RN_031_SoSoroArmazena` |
| RN-032 | Validade na soroteca: 7 dias a partir da coleta (valor fictício) | `Soroteca` | `RN_032_Validade` |
| RN-033 | Amostra vencida não vai para análise e é marcada para descarte | `Soroteca` | `RN_033_VencidaNaoAnalisa` |
| RN-034 | Descarte registra quem, quando e motivo e libera a posição | `Soroteca` | `RN_034_DescarteRegistrado` |
| RN-035 | Toda movimentação entra na cadeia de custódia | `Amostra` | `RN_035_CustodiaCompleta` |

## Equipamento e qualidade

| ID | Regra | Onde | Teste |
|---|---|---|---|
| RN-040 | Equipamento só aceita exames que declara | `Equipamento` | `RN_040_ExameNaoSuportado` |
| RN-041 | Respeita a capacidade; excedente espera na fila | `Equipamento` | `RN_041_Capacidade` |
| RN-042 | Reprocessa até 2 vezes; depois erro definitivo | `Execucao` | `RN_042_LimiteDeReprocesso` |
| RN-043 | CQ reprovado bloqueia liberação dos resultados daquele equipamento | `Qualidade` | `RN_043_CQReprovadoBloqueia` |
| RN-044 | Avaliar as seis regras de Westgard | `Protocolos` | `RN_044_Westgard_*` (uma por regra) |

## Resultado e laudo

| ID | Regra | Onde | Teste |
|---|---|---|---|
| RN-050 | Faixa escolhida por sexo do paciente | `Catalogo` | `RN_050_FaixaPorSexo` |
| RN-051 | Flag H/L/C conforme faixa normal e crítica | `Protocolos` | `RN_051_Flags` |
| RN-052 | Valor crítico exige confirmação explícita antes de validar | `Resultado` | `RN_052_CriticoExigeConfirmacao` |
| RN-053 | Delta check acima do percentual marca "suspeito" e exige justificativa | `Resultado` | `RN_053_DeltaCheck` |
| RN-054 | O operador que enviou o exame ao equipamento não valida o mesmo resultado (compara `execucao.enviada_por_id` com o operador atual) | `Resultado` | `RN_054_SeparacaoDeDeveres` |
| RN-055 | Laudo só libera com todos os resultados do pedido validados | `Laudo` | `RN_055_LiberacaoCompleta` |
| RN-056 | Laudo liberado é imutável; retificação cria versão nova e a anterior permanece consultável | `Laudo`, gatilho | `RN_056_LaudoImutavel` |

## Sessão e segurança

| ID | Regra | Onde | Teste |
|---|---|---|---|
| RN-060 | Cota de 500 registros por sessão | `Sessao` | `RN_060_CotaDeRegistros` |
| RN-061 | Toda transição e todo bloqueio geram evento de auditoria | `IAuditoria` | `RN_061_TudoAuditado` |
| RN-062 | A cadeia de auditoria é verificável e detecta adulteração | `Auditoria` | `RN_062_CadeiaDetectaAdulteracao` |

## Relacionadas

[[Fluxo-do-Laboratorio]] · [[Plano-de-Testes-por-Modulo]] · [[Catalogo-de-Exames]]
