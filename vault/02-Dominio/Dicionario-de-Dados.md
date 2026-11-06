---
tipo: dominio
projeto: Bancada
atualizado: 2026-11-01
---

# Dicionário de dados

Todas as colunas de todas as tabelas. Complementa [[Modelo-de-Dados]] (visão e restrições) com o que falta para programar: tipo, obrigatoriedade, tamanho e observação. Dialeto SQLite, nomes em pt-BR, singular, snake_case.

**Convenções:** `id` é `INTEGER PRIMARY KEY`. Datas e horas em `TEXT` ISO-8601 **UTC** (`2026-11-04T12:30:00.000Z`); a tela converte para UTC−03:00 (o Brasil não tem horário de verão desde 2019). Booleano é `INTEGER` com `CHECK (x IN (0,1))`. `*_cifrado` guarda `base64(versao‖nonce‖tag‖cifra)` ([[Cifra-e-Chaves]]). `NN` = não nulo. Texto livre sempre com tamanho máximo na validação ([[Cabecalhos-e-Configuracao-Segura]]).

## Pessoas

### `paciente`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `codigo` | TEXT | NN, UNIQUE | `PAC-####`, gerado |
| `nome` | TEXT | NN | até 80, fictício |
| `nascimento` | TEXT | NN | data `AAAA-MM-DD`, entre 1930 e hoje |
| `sexo` | TEXT | NN | `CHECK IN ('M','F')` |
| `criado_em` | TEXT | NN | |

### `alergia`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `paciente_id` | INTEGER | NN, FK | |
| `substancia` | TEXT | NN | até 60 (ex.: látex, dipirona) |
| `reacao_cifrado` | TEXT | | descrição livre, até 120 |
| `ativa` | INTEGER | NN, default 1 | alergia inativada não gera alerta |
| `registrada_em` | TEXT | NN | |

### `profissional` (os "operadores" da demonstração)
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `codigo` | TEXT | NN, UNIQUE | `MED-01`, `ANA-01`, `REC-01`, `COL-01`, `ADM-01` |
| `nome` | TEXT | NN | fictício |
| `papel` | TEXT | NN | `CHECK IN ('Recepcao','Medico','Coleta','Analista','Admin')` |

## Agenda e atendimento

### `agenda_grade`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `profissional_id` | INTEGER | NN, FK | só papel Medico |
| `inicio` | TEXT | NN | |
| `duracao_min` | INTEGER | NN, default 30 | `CHECK > 0` |
| | | | `UNIQUE (profissional_id, inicio)` |

### `agendamento`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `paciente_id` | INTEGER | NN, FK | |
| `grade_id` | INTEGER | NN, **UNIQUE**, FK | impede dupla reserva (RN-001) |
| `estado` | TEXT | NN | `Agendado`, `Confirmado`, `ChegouCheckIn`, `EmAtendimento`, `Concluido`, `Cancelado`, `Faltou` |
| `motivo_cifrado` | TEXT | | motivo da consulta, até 200 |
| `criado_em`, `atualizado_em` | TEXT | NN | |

### `atendimento`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `agendamento_id` | INTEGER | NN, UNIQUE, FK | 1:1, nasce no check-in |
| `profissional_id` | INTEGER | NN, FK | o médico do horário |
| `estado` | TEXT | NN | `Aguardando`, `EmAtendimento`, `Concluido` |
| `chegada_em` | TEXT | NN | hora do check-in |
| `iniciado_em`, `concluido_em` | TEXT | | |

### `evolucao` (imutável)
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `paciente_id` | INTEGER | NN, FK | redundante de propósito (consulta do prontuário) |
| `atendimento_id` | INTEGER | NN, FK | |
| `autor_id` | INTEGER | NN, FK `profissional` | |
| `texto_cifrado` | TEXT | NN | 1 a 2.000 caracteres antes de cifrar |
| `corrige_id` | INTEGER | FK `evolucao` | nulo se for original (RN-011) |
| `criada_em` | TEXT | NN | |
| | | | gatilhos: sem `UPDATE`, sem `DELETE` (RN-010) |

## Catálogo

### `exame_catalogo`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `codigo` | TEXT | NN, UNIQUE | `GLI`, `ELE`, `HEM`... |
| `nome` | TEXT | NN | |
| `material` | TEXT | NN | `CHECK IN ('Soro','SangueTotalEDTA')` |
| `volume_min_ml` | REAL | NN | `CHECK > 0` |
| `jejum_horas` | INTEGER | NN, default 0 | |
| `tempo_min` | INTEGER | NN | tempo simulado de processamento |

### `analito`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `exame_id` | INTEGER | NN, FK | |
| `codigo` | TEXT | NN, UNIQUE | `POT`, `SOD`... |
| `nome`, `unidade` | TEXT | NN | |
| `casas_decimais` | INTEGER | NN, default 1 | |

### `faixa_referencia`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `analito_id` | INTEGER | NN, FK | |
| `sexo` | TEXT | NN | `CHECK IN ('M','F','A')` |
| `idade_min_anos` | INTEGER | NN, default 18 | |
| `idade_max_anos` | INTEGER | | nulo = sem teto |
| `normal_min`, `normal_max` | REAL | | nulo = faixa aberta daquele lado |
| `critico_min`, `critico_max` | REAL | | nulo = sem limite crítico |
| `delta_pct` | REAL | | variação aceita contra o resultado anterior |
| | | | `CHECK (normal_min IS NULL OR normal_max IS NULL OR normal_min <= normal_max)` |

## Pedido, amostra e soroteca

### `pedido`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `codigo` | TEXT | NN, UNIQUE | `PED-######` |
| `atendimento_id` | INTEGER | NN, FK | |
| `solicitante_id` | INTEGER | NN, FK `profissional` | médico |
| `estado` | TEXT | NN | `Solicitado`, `Coletado`, `Recebido`, `EmAnalise`, `ResultadoPreliminar`, `Validado`, `Liberado`, `Rejeitado`, `Retificado`, `Cancelado` |
| `urgente` | INTEGER | NN, default 0 | |
| `criado_em` | TEXT | NN | |

### `pedido_item`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `pedido_id`, `exame_id` | INTEGER | NN, FK | `UNIQUE (pedido_id, exame_id)` |
| `amostra_id` | INTEGER | FK | nulo até a coleta (RN-021) |
| `estado` | TEXT | NN | mesmos estados do pedido, por item |

### `amostra` (tubo)
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `etiqueta` | TEXT | NN, UNIQUE | `AMO-######` (RN-024) |
| `pedido_id` | INTEGER | NN, FK | |
| `material` | TEXT | NN | |
| `estado` | TEXT | NN | `Etiquetada`, `Coletada`, `Recebida`, `Armazenada`, `EmAnalise`, `Descartada`, `Rejeitada` |
| `volume_ml` | REAL | | preenchido na coleta (RN-023) |
| `jejum_confirmado_horas` | INTEGER | | |
| `hemolisada` | INTEGER | NN, default 0 | |
| `coletada_em`, `recebida_em` | TEXT | | |
| `validade_ate` | TEXT | | coleta + 7 dias (RN-032) |
| `motivo_rejeicao` | TEXT | | `CHECK IN ('Hemolise','VolumeInsuficiente','MaterialErrado','EtiquetaIlegivel')` |
| `observacao_cifrado` | TEXT | | até 200 |

### `amostra_custodia` (somente inclusão)
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `amostra_id` | INTEGER | NN, FK | |
| `evento` | TEXT | NN | `Coletada`, `Recebida`, `Armazenada`, `Movida`, `EnviadaAoEquipamento`, `Descartada` |
| `operador_id` | INTEGER | NN, FK | |
| `local` | TEXT | NN | `Coleta`, `A-2-4`, `EQ-BIO-01` |
| `quando` | TEXT | NN | |

### `soroteca_rack`, `soroteca_caixa`, `soroteca_posicao`
| Tabela | Colunas |
|---|---|
| `soroteca_rack` | `id`, `codigo` TEXT NN UNIQUE (`R1`) |
| `soroteca_caixa` | `id`, `rack_id` FK NN, `codigo` TEXT NN (`A`), `linhas` INTEGER NN default 9, `colunas` INTEGER NN default 9, `UNIQUE (rack_id, codigo)` |
| `soroteca_posicao` | `id`, `caixa_id` FK NN, `linha` INTEGER NN `BETWEEN 1 AND 9`, `coluna` INTEGER NN `BETWEEN 1 AND 9`, `amostra_id` INTEGER **UNIQUE** FK nulo se livre; `UNIQUE (caixa_id, linha, coluna)` (RN-030) |

## Equipamento, resultado e laudo

### `equipamento`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `codigo` | TEXT | NN, UNIQUE | `EQ-BIO-01` |
| `nome` | TEXT | NN | |
| `protocolo` | TEXT | NN | `CHECK IN ('ASTM','HL7')` |
| `capacidade` | INTEGER | NN | `CHECK > 0` |
| `taxa_falha` | REAL | NN | `CHECK BETWEEN 0 AND 1` |

`equipamento_exame`: `equipamento_id`, `exame_id`, PK composta.

### `execucao`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `pedido_item_id` | INTEGER | NN, FK | |
| `equipamento_id` | INTEGER | NN, FK | |
| `estado` | TEXT | NN | `NaFila`, `Enviado`, `Processando`, `Concluido`, `Erro` |
| `prioridade` | INTEGER | NN, default 0 | urgente = 1 |
| `tentativas` | INTEGER | NN, default 0 | `CHECK <= 3` (RN-042) |
| `enviada_por_id` | INTEGER | NN, FK `profissional` | **quem executou** (RN-054) |
| `enviada_em`, `prevista_para`, `concluida_em` | TEXT | | horas de simulação |

### `resultado`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `execucao_id`, `analito_id` | INTEGER | NN, FK | |
| `valor` | REAL | NN | |
| `flag` | TEXT | NN | `CHECK IN ('N','L','H','CL','CH')` |
| `delta_suspeito` | INTEGER | NN, default 0 | |
| `justificativa_delta` | TEXT | | obrigatória se suspeito |
| `estado` | TEXT | NN | `Preliminar`, `CriticoConfirmado`, `Validado` |
| `validado_por_id` | INTEGER | FK `profissional` | tem de ser diferente de `execucao.enviada_por_id` |
| `validado_em` | TEXT | | |

### `laudo` (imutável)
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `pedido_id` | INTEGER | NN, FK | |
| `versao` | INTEGER | NN | começa em 1; `UNIQUE (pedido_id, versao)` |
| `conteudo_json` | TEXT | NN | instantâneo dos resultados, faixas e flags |
| `liberado_por_id` | INTEGER | NN, FK | |
| `liberado_em` | TEXT | NN | |
| `motivo_retificacao_cifrado` | TEXT | | exigido a partir da versão 2 |
| | | | gatilhos: sem `UPDATE`, sem `DELETE` (RN-056) |

### `controle_qualidade`
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `id` | INTEGER | PK | |
| `equipamento_id`, `analito_id` | INTEGER | NN, FK | |
| `nivel` | TEXT | NN | `CHECK IN ('Normal','Alto')` |
| `valor`, `media_alvo`, `desvio` | REAL | NN | `desvio > 0` |
| `regra_violada` | TEXT | | `1-2s`, `1-3s`, `2-2s`, `R-4s`, `4-1s`, `10x` |
| `aprovado` | INTEGER | NN | |
| `medido_em` | TEXT | NN | |

## Auditoria

### `auditoria` (somente inclusão, [[Auditoria-Encadeada]])
| Coluna | Tipo | | Observação |
|---|---|---|---|
| `seq` | INTEGER | PK | |
| `quando` | TEXT | NN | relógio real |
| `quando_simulado` | TEXT | | relógio da simulação |
| `papel` | TEXT | NN | |
| `operador_id` | INTEGER | FK | |
| `acao` | TEXT | NN | do [[Catalogo-de-Eventos]] |
| `alvo_tipo`, `alvo_id` | TEXT | NN | |
| `detalhe_json` | TEXT | NN | sem texto livre |
| `resultado` | TEXT | NN | `CHECK IN ('Ok','Recusado','Bloqueado')` |
| `hash_anterior`, `hash` | TEXT | NN | SHA-256 hex |

## Fora do banco (só na sessão)

Papel atual, operador atual, chave de cifra, relógio de simulação, contadores de cota, estado do tour. Ver [[Sessao-e-Isolamento]].

## Relacionadas

[[Modelo-de-Dados]] · [[Regras-de-Negocio]] · [[Especificacao-das-Telas]]
