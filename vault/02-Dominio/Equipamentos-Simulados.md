---
tipo: dominio
projeto: Bancada
atualizado: 2026-11-01
---

# Equipamentos simulados

Três analisadores fictícios. Cada um fala um protocolo diferente, o que força uma camada de adaptação de verdade.

| Código | Nome | Exames | Protocolo | Capacidade | Falha sorteada |
|---|---|---|---|---|---|
| `EQ-BIO-01` | Bioquímica Alfa | GLI, URE, CRE, COL, TRI, ELE | ASTM (E1381/E1394) | 4 em paralelo | 3% |
| `EQ-HEM-01` | Hematologia Beta | HEM | HL7 v2 | 2 em paralelo | 2% |
| `EQ-IMU-01` | Imunoensaio Gama | TSH, T4L, FER | HL7 v2 | 6 em paralelo | 5% |

A semente do sorteio é fixa por sessão, então o mesmo cenário falha no mesmo ponto (útil para demonstração e para teste).

## Como a simulação funciona

1. O analista (ou o fluxo automático da amostra recebida) **envia** a execução ao equipamento. O sistema monta a mensagem de pedido no protocolo do equipamento ([[Protocolos]]) e a registra no monitor.
2. A execução entra na **fila** do equipamento. Quando há vaga na capacidade, vira `Processando` com horário previsto de término = agora simulado + tempo do exame ([[Catalogo-de-Exames]]).
3. `AvancarSimulacao` ([[Camada-de-Aplicacao]]) conclui as execuções vencidas: gera os valores, monta a mensagem de **resultado** no protocolo, faz o caminho de volta (parser) e grava o resultado como `ResultadoPreliminar`.
4. Falha sorteada leva a `Erro`; reprocessar volta para a fila (até 2 vezes). Na terceira falha a execução fica `Erro` e exige nova amostra.

## Como os valores são gerados

Determinístico a partir da semente: para cada analito, o valor sai de uma distribuição em torno do ponto médio da faixa normal, com desvio pequeno. Os **cenários** ([[Dados-de-Semente]]) forçam valores específicos (ex.: potássio 7,1 mmol/L para o cenário de valor crítico). Nada disso é simulação fisiológica e o painel avisa.

## Monitor do protocolo

Tela que mostra, em ordem, cada quadro/mensagem trocada com o equipamento: `ENQ`, `ACK`, quadros com checksum, `EOT` (ASTM); `MSH`/`OBR`/`OBX` e `ACK` (HL7). Cada linha tem hora simulada, direção (↑ host → equipamento, ↓ equipamento → host) e o texto decodificado. É a peça que mais lembra o trabalho real de integração.

## Relógio de simulação

| Fator | Exame de 20 min leva |
|---|---|
| ×1 | 20 min |
| ×60 | 20 s |
| ×600 | 2 s |

Botões: pausar, ×1, ×60, ×600. O relógio é da **sessão** (`IRelogio` de simulação). O de verdade (para TTL e cookie) nunca é acelerado.

## Controle de qualidade

Antes de liberar resultados de um equipamento no dia, o CQ precisa estar aprovado. O sistema gera medições de controle por equipamento e exame (nível normal e nível alto) e avalia as regras de Westgard ([[Protocolos]]). Cenário "CQ fora" força a violação da regra 2-2s e bloqueia a liberação até um novo controle passar.

## Testes-chave

- Equipamento recusa exame que não declara (`NaoSuportado`).
- Respeito à capacidade: o 5º item do EQ-BIO-01 espera na fila.
- Fila reordena por prioridade (urgente à frente).
- Com semente fixa, a mesma execução falha sempre no mesmo item.
- Resultado gerado → mensagem → parser → mesmo valor (ida e volta).
- Reprocessar 3 vezes termina em `Erro` definitivo.

## Relacionadas

[[Fluxo-do-Laboratorio]] · [[Modulos]] · [[Executor-de-Testes]]
