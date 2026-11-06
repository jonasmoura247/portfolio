---
tipo: arquitetura
projeto: Bancada
atualizado: 2026-11-01
---

# Protocolos (projeto `Bancada.Protocolos`)

Biblioteca de funções puras: recebe texto/bytes, devolve objetos; recebe objetos, devolve texto/bytes. Sem I/O, sem relógio, sem aleatório. É a parte que mais lembra a experiência de integração de equipamentos da C4.

## 1. Camada de transmissão no estilo ASTM E1381 (hoje CLSI LIS01)

Caracteres de controle: `ENQ` (05h), `ACK` (06h), `NAK` (15h), `EOT` (04h), `STX` (02h), `ETX` (03h), `ETB` (17h), `CR` (0Dh), `LF` (0Ah).

**Troca de ponta a ponta:**

```
Remetente: ENQ                         ─►  Receptor
           ◄─ ACK
Remetente: <STX>1 texto <ETX> C1 C2 CR LF ─►
           ◄─ ACK  (ou NAK: reenviar, até 6 vezes)
Remetente: <STX>2 texto <ETX> C1 C2 CR LF ─► ...
Remetente: EOT                         ─►  (fim da transmissão)
```

**Quadro:** `STX` + número do quadro (1 caractere, `1`..`7` e depois `0`, rodando) + texto + `ETX` (ou `ETB` se o registro continua no próximo quadro) + checksum (2 dígitos hex maiúsculos) + `CR` `LF`.

**Checksum:** soma dos bytes do número do quadro até o `ETX`/`ETB` inclusive, módulo 256, em hexadecimal de 2 dígitos.

**Limite de texto por quadro:** 240 caracteres. Registro maior vira vários quadros com `ETB`, e o último termina com `ETX`.

Casos que o parser precisa tratar (cada um vira teste): checksum errado, quadro sem `ETX`, número de quadro fora de ordem, quadro duplicado (reenvio), registro dividido em `ETB`, texto vazio, caractere de controle dentro do texto, `NAK` e esgotamento das 6 tentativas.

## 2. Camada de dados no estilo ASTM E1394 (hoje CLSI LIS02)

Campos separados por `|`, repetições por `\`, componentes por `^`, escape por `&` (delimitadores declarados no registro `H`).

| Registro | Função | Exemplo (fictício) |
|---|---|---|
| `H` | Cabeçalho | `H\|\^&\|\|\|Bancada^1.0\|\|\|\|\|\|\|P\|1\|20261101120000` |
| `P` | Paciente | `P\|1\|\|PAC-0001\|\|\|19800101\|F` |
| `O` | Pedido | `O\|1\|AMO-000123\|\|^^^GLU\|R` |
| `R` | Resultado | `R\|1\|^^^GLU\|92\|mg/dL\|70-99\|N\|\|F` |
| `L` | Fim | `L\|1\|N` |
| `C` | Comentário | `C\|1\|I\|Amostra levemente lipêmica\|G` |

O fluxo **consulta ao host** (equipamento pergunta o que fazer com uma amostra) e **resultado** (equipamento devolve valores) são os dois modelados.

## 3. HL7 v2 (alternativa de mensageria)

Segmentos separados por `CR`, campos por `|`, componentes por `^`.

```
MSH|^~\&|BANCADA|LAB|EQBIO01|LAB|20261101120000||ORM^O01|MSG0001|P|2.5.1
PID|1||PAC-0001||||19800101|F
ORC|NW|PED-000045
OBR|1|PED-000045||GLU^Glicose^BANCADA
```

```
MSH|^~\&|EQBIO01|LAB|BANCADA|LAB|20261101121500||ORU^R01|MSG0002|P|2.5.1
PID|1||PAC-0001
OBR|1|PED-000045||GLU^Glicose^BANCADA
OBX|1|NM|GLU^Glicose||92|mg/dL|70-99|N|||F
```

Mensagens usadas: `ORM^O01` (pedido ao equipamento), `ORU^R01` (resultado), `ACK` (confirmação). Cada equipamento fala **um** dos dois protocolos ([[Equipamentos-Simulados]]), o que mostra a camada de adaptação.

## 4. Code 128 (etiqueta da amostra)

Conjunto B. Cada símbolo tem 11 módulos (barras e espaços); o código de parada tem 13. Sequência: **início B** (valor 104) + caracteres (valor = ASCII − 32) + **dígito verificador** + **parada** (106).

Dígito verificador: `(104 + Σ(posição × valor)) mod 103`, posição começando em 1 para o primeiro caractere de dados.

Saída: lista de larguras de módulo e um SVG (retângulos) gerado por função pura. Teste de ida e volta: gera → decodifica as larguras de volta → igual ao texto original, para todos os caracteres ASCII imprimíveis e para as etiquetas reais (`AMO-000123`).

## 5. Cálculos de laboratório

| Função | Entrada → saída |
|---|---|
| `ClassificarResultado` | valor, faixa normal, faixa crítica → N, L, H, LL/HH crítico |
| `DeltaCheck` | valor atual, valor anterior, percentual aceito → ok / suspeito |
| `RegrasDeWestgard` | série de medições do controle, média, desvio → regras violadas |
| `CalcularIdade` | nascimento, data → idade em anos (para escolher a faixa) |

Todas puras, com testes de borda: valor exatamente no limite, faixa aberta de um lado, série curta demais para a regra 10x.

## Relacionadas

[[Equipamentos-Simulados]] · [[Modulos]] · [[Plano-de-Testes-por-Modulo]]
