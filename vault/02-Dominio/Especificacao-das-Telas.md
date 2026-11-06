---
tipo: dominio
projeto: Bancada
atualizado: 2026-11-01
---

# Especificação das telas

O que cada tela mostra, quais campos tem, o que valida e qual mensagem devolve. É a ponte entre [[Modulos]] (visão) e o código (controller, ViewModel, View, JS). As chaves de texto seguem `Modulo.Elemento.Acao` ([[Convencoes-de-Codigo]]).

## Padrões que valem para todas

| Item | Padrão |
|---|---|
| **Barra superior** | marca · seletor de **Papel** · seletor de **Operador** (profissionais fictícios daquele papel) · tema · idioma · "Reiniciar sessão" |
| **Faixa fixa** | "Dados fictícios. Simulação educacional..." ([[Dados-Ficticios-e-LGPD]]) |
| **Menu** | só os módulos que o papel pode usar ([[Matriz-de-Autorizacao]]) |
| **Erro de campo** | texto abaixo do campo, `aria-describedby`, ícone, foco no primeiro erro |
| **Erro de regra** | faixa no topo do formulário com a mensagem da regra (409/422), formulário mantém o que foi digitado |
| **Sucesso** | mensagem curta com `aria-live` e o novo estado do item |
| **Estado vazio** | frase que diz o que aparece ali e o botão para criar o primeiro |
| **Carregando** | esqueleto da tabela; nunca tela em branco |
| **Confirmar ação destrutiva** | modal próprio da página (sem `confirm()` do navegador) |
| **Tabela longa** | paginação de 20, ordenação por teclado |

**Operador:** o visitante escolhe papel e depois o profissional fictício que está "logado" (`ANA-01`, `ANA-02`, `MED-01`...). É isso que dá sentido à separação de deveres (RN-054): `ANA-01` envia ao equipamento, `ANA-02` valida. O operador fica na sessão e vai para a auditoria ([[Sessao-e-Isolamento]]).

## 1. Pacientes

**Lista** (`/pacientes`): busca (até 64 caracteres, sem exigir tamanho mínimo), tabela com código, nome, idade, sexo, próximo agendamento. Vazia: "Nenhum paciente encontrado para esta busca."

**Ficha** (`/pacientes/{id}`): dados cadastrais, alergias, próximos agendamentos, linha do tempo ([[Fluxo-do-Laboratorio]]). Recepção vê só o cadastral e os agendamentos; médico vê tudo; coleta vê alergias e jejum.

## 2. Agenda

**Tela** (`/agenda`): seletor de médico, navegação de semana (anterior, hoje, próxima), grade de segunda a sexta com horários de 30 min. Cada célula: livre ou agendamento (código do paciente + selo de estado).

**Novo agendamento** (modal):
| Campo | Tipo | Obrigatório | Validação | Mensagem |
|---|---|---|---|---|
| Paciente | busca com lista | sim | existe na sessão | `Agenda.Erro.PacienteInvalido` |
| Horário | célula clicada | sim | está na grade, é futuro, está livre | `Agenda.Erro.HorarioIndisponivel` (RN-001, 002, 003) |
| Motivo | texto | não | até 200, sem HTML | `Comum.Erro.TextoLongo` |

**Ações no agendamento:** confirmar, reagendar (escolhe outro horário livre), cancelar (modal de confirmação com motivo opcional). Teclado: setas movem entre células, `Enter` abre, `Esc` fecha.

## 3. Atendimento

**Fila** (`/atendimentos`, recepção e médico): lista do dia ordenada por horário. Colunas: hora, paciente, médico, estado, chegada, espera. Ações por linha conforme o papel:

| Estado do agendamento | Recepção | Médico |
|---|---|---|
| Agendado / Confirmado | **Fazer check-in** | — |
| ChegouCheckIn | — | **Iniciar atendimento** |
| EmAtendimento | — | **Abrir atendimento** |
| Concluído / Faltou / Cancelado | só leitura | só leitura |

**Check-in** (botão, sem formulário): confirma a hora, cria o atendimento em `Aguardando`. Erros: fora do dia → `Atendimento.Erro.CheckInForaDoDia` (RN-006); já feito → `Atendimento.Erro.JaFeito`.

**Tela do atendimento** (`/atendimentos/{id}`, médico do horário):
- Cabeçalho: paciente (código, nome, idade, sexo), horário, tempo decorrido, **faixa de alergias ativas** se houver.
- Colunas: prontuário recente (últimas 3 evoluções), formulário de nova evolução, pedidos deste atendimento, botão **Pedir exame**.
- Botão **Concluir atendimento**: exige pelo menos uma evolução registrada neste atendimento (`Atendimento.Erro.SemEvolucao`).
- Outro médico abrindo: `Atendimento.Erro.OutroProfissional` (RN-007).

## 4. Prontuário

**Tela** (`/prontuario/{pacienteId}`): três blocos.

1. **Alergias:** lista (substância, reação, ativa/inativa); adicionar/inativar. Sem exclusão.
2. **Evoluções:** da mais nova para a mais antiga. Cada uma: data/hora, autor, texto, selo "correção de..." quando houver, botão **Corrigir**. Correção aparece como evolução nova ligada à original; a original continua visível com selo "corrigida em dd/mm".
3. **Exames anteriores:** pedidos com data, exames e estado; abre o laudo se liberado.

**Registrar evolução:**
| Campo | Tipo | Obrigatório | Validação | Mensagem |
|---|---|---|---|---|
| Texto | área de texto | sim | 1 a 2.000 caracteres; nenhum HTML interpretado | `Prontuario.Erro.TextoVazio` / `Comum.Erro.TextoLongo` |

Contador de caracteres ao vivo (JS). **Corrigir evolução:** mesmo formulário, com o texto original mostrado ao lado, só leitura. Sem edição nem exclusão (RN-010).

**Alergia nova:**
| Campo | Obrigatório | Validação |
|---|---|---|
| Substância | sim | 1 a 60, sem HTML |
| Reação | não | até 120 |

Texto das evoluções e reações vai cifrado ao banco e é decifrado só para exibir ([[Cifra-e-Chaves]]).

## 5. Pedido de exame

**Tela** (`/pedidos/novo?atendimento=`): catálogo à esquerda (busca por nome ou código, filtro por material), carrinho à direita.

- Alergia ativa: faixa de aviso no topo do pedido (RN-013), sem bloquear.
- Carrinho: exames escolhidos, **tubos previstos** (agrupados por material, RN-021), jejum exigido (o maior), volume total.
- Regras: 1 a 20 exames, sem repetição (`Pedido.Erro.Quantidade`, `Pedido.Erro.Repetido`).
- Campo **Urgente** (caixa de seleção): urgente vai à frente na fila do equipamento.
- Botão **Enviar pedido**: cria `PED-######`, mostra resumo e etiquetas previstas.

## 6. Coleta

**Lista** (`/coletas`): pedidos `Solicitado` com paciente, exames, tubos, jejum. **Coletar** abre o formulário:

| Campo | Tipo | Obrigatório | Validação | Mensagem |
|---|---|---|---|---|
| Jejum cumprido (horas) | número | se o tubo exige | inteiro 0–72; ≥ exigido | `Coleta.Erro.JejumInsuficiente` (RN-022) |
| Volume coletado (mL) | número | sim | > 0; ≥ mínimo do tubo | `Coleta.Erro.VolumeInsuficiente` (RN-023) |
| Hemolisada | caixa | não | — | — |

Ao confirmar: gera etiqueta `AMO-######`, abre a **etiqueta imprimível** (Code 128, CSS de impressão). **Rejeitar amostra:** motivo da lista fechada + observação até 200; gera pedido de recoleta (RN-025).

## 7. Soroteca

**Tela** (`/soroteca`): seletor de rack e caixa, mapa 9×9, painel lateral da posição selecionada, lista de **vencendo em 24 h** e **vencidas**, busca por etiqueta.

| Ação | Quem | Regra | Mensagem de erro |
|---|---|---|---|
| Armazenar | coleta, analista | posição livre; amostra de soro e `Recebida` | `Soroteca.Erro.PosicaoOcupada`, `.SoSoro` |
| Mover | coleta, analista | destino livre | `Soroteca.Erro.PosicaoOcupada` |
| Descartar | analista | motivo; libera a posição | `Soroteca.Erro.MotivoObrigatorio` |

Cada ação grava na cadeia de custódia. Teclado: setas no mapa, `Enter` seleciona, `M` move, `D` descarta (com confirmação).

## 8. Equipamento

**Tela** (`/equipamentos`): um cartão por equipamento (estado, capacidade usada, fila, progresso de cada item), monitor do protocolo, controle do relógio (pausar, ×1, ×60, ×600).

- **Enviar ao equipamento:** a partir de uma amostra `Recebida`; lista os exames do tubo que aquele equipamento suporta (`Equipamento.Erro.NaoSuportado`, RN-040).
- **Reprocessar:** só execução em `Erro` e com menos de 3 tentativas (RN-042).
- Monitor: linhas com hora simulada, direção (↑ host, ↓ equipamento), texto decodificado ([[Protocolos]]).

## 9. Resultado, validação e laudo

**Pendentes** (`/resultados`): resultados `Preliminar`, agrupados por pedido. Linha: analito, valor, unidade, faixa, flag (H, L, CH, CL com seta e letra), valor anterior, delta.

**Validar:**
- Valor **crítico**: botão **Confirmar valor crítico** antes de validar (RN-052).
- Delta check suspeito: exige justificativa de até 200 caracteres (RN-053).
- Operador que executou: botão **Validar** desabilitado com a explicação `Resultado.Erro.MesmoOperador` (RN-054).
- Controle de qualidade reprovado do equipamento: validação bloqueada com `Qualidade.Erro.CQReprovado` (RN-043).

**Liberar laudo:** só com todos os resultados do pedido validados (`Laudo.Erro.Incompleto`, RN-055). **Laudo** (`/laudos/{id}`): cabeçalho com paciente, pedido, datas; tabela de resultados com faixas e flags; rodapé com o **aviso médico**; versões anteriores listadas. **Retificar:** exige motivo; cria versão nova.

## 10. Controle de qualidade

**Tela** (`/qualidade`): seletor de equipamento e analito, gráfico de Levey-Jennings (médias e faixas de ±1, ±2, ±3 desvios, pontos coloridos pelo estado), lista de violações, botão **Registrar controle** (gera medição simulada pela semente).

## 11. Auditoria

**Tela** (`/auditoria`): tabela paginada dos eventos com filtro por grupo ([[Catalogo-de-Eventos]]), botão **Verificar cadeia** (resultado "íntegra" ou "quebrou no evento N") e **Simular adulteração** (sobre a cópia exibida, avisado na tela).

## 12. Painel técnico e laboratório de ataque

Ver [[Painel-Tecnico]], [[Executor-de-Testes]] e [[Corpus-de-Ataque]].

## ViewModels (nomes)

Para cada tela: `<Tela>ViewModel` (saída) e `<Acao>Modelo` (entrada), em `Bancada.Web/Modelos/`. Exemplos: `SemanaViewModel`, `AgendarModelo`, `FilaDoDiaViewModel`, `RegistrarEvolucaoModelo`, `ProntuarioViewModel`, `NovoPedidoModelo`, `ColetaModelo`, `MapaDaCaixaViewModel`, `ResultadosPendentesViewModel`, `LaudoViewModel`. Conversões em métodos estáticos testados ([[Rotas-e-Controllers]]).

## Relacionadas

[[Dicionario-de-Dados]] · [[Matriz-de-Autorizacao]] · [[Regras-de-Negocio]] · [[Design-do-Sistema]]
