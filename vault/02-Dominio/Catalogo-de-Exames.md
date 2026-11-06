---
tipo: dominio
projeto: Bancada
atualizado: 2026-11-01
---

# Catálogo de exames (fictício)

> **Valores simplificados e inventados para demonstração. Não usar clinicamente.** Servem para exercitar as regras (faixa por sexo e idade, flags, crítico, delta check).

Modelo: **exame** (o que se pede) contém **analitos** (o que se mede). O resultado e a faixa são por analito.

## Exames

| Código | Exame | Material | Volume mínimo | Jejum | Equipamento | Tempo simulado |
|---|---|---|---|---|---|---|
| `GLI` | Glicose | Soro | 0,5 mL | 8 h | EQ-BIO-01 | 8 min |
| `URE` | Ureia | Soro | 0,5 mL | não | EQ-BIO-01 | 8 min |
| `CRE` | Creatinina | Soro | 0,5 mL | não | EQ-BIO-01 | 8 min |
| `COL` | Colesterol total | Soro | 0,5 mL | 12 h | EQ-BIO-01 | 10 min |
| `TRI` | Triglicerídeos | Soro | 0,5 mL | 12 h | EQ-BIO-01 | 10 min |
| `ELE` | Eletrólitos (sódio e potássio) | Soro | 0,5 mL | não | EQ-BIO-01 | 6 min |
| `HEM` | Hemograma simplificado | Sangue total EDTA | 2 mL | não | EQ-HEM-01 | 12 min |
| `TSH` | TSH | Soro | 0,5 mL | não | EQ-IMU-01 | 20 min |
| `T4L` | T4 livre | Soro | 0,5 mL | não | EQ-IMU-01 | 20 min |
| `FER` | Ferritina | Soro | 0,5 mL | não | EQ-IMU-01 | 20 min |

## Analitos e faixas

Sexo: M, F ou A (ambos). Idade: adultos (18 anos ou mais). Faixa crítica: fora dela exige confirmação.

| Exame | Analito | Unidade | Sexo | Normal | Crítico (abaixo / acima) |
|---|---|---|---|---|---|
| GLI | Glicose em jejum | mg/dL | A | 70 – 99 | < 40 / > 500 |
| URE | Ureia | mg/dL | A | 15 – 45 | — / > 180 |
| CRE | Creatinina | mg/dL | M | 0,7 – 1,3 | — / > 7,0 |
| CRE | Creatinina | mg/dL | F | 0,6 – 1,1 | — / > 7,0 |
| COL | Colesterol total | mg/dL | A | < 190 | — / — |
| TRI | Triglicerídeos | mg/dL | A | < 150 | — / > 1000 |
| ELE | Sódio | mmol/L | A | 135 – 145 | < 120 / > 160 |
| ELE | Potássio | mmol/L | A | 3,5 – 5,1 | < 2,5 / > 6,5 |
| HEM | Hemoglobina | g/dL | M | 13,5 – 17,5 | < 7,0 / > 20,0 |
| HEM | Hemoglobina | g/dL | F | 12,0 – 15,5 | < 7,0 / > 20,0 |
| HEM | Leucócitos | /µL | A | 4.000 – 11.000 | < 1.000 / > 50.000 |
| HEM | Plaquetas | /µL | A | 150.000 – 450.000 | < 20.000 / > 1.000.000 |
| TSH | TSH | mUI/L | A | 0,4 – 4,0 | — / — |
| T4L | T4 livre | ng/dL | A | 0,8 – 1,8 | — / — |
| FER | Ferritina | ng/mL | M | 30 – 400 | — / — |
| FER | Ferritina | ng/mL | F | 15 – 150 | — / — |

Faixa com um lado só (`< 190`) é **faixa aberta**: o código precisa tratar e há teste para cada tipo (aberta embaixo, aberta em cima, fechada, só crítica).

## Regras do catálogo

- Cada exame tem **um** equipamento principal; o equipamento só aceita os exames que ele declara ([[Equipamentos-Simulados]]).
- Exames do mesmo material no mesmo pedido vão no **mesmo tubo** (ex.: GLI + URE + CRE = um tubo de soro). Hemograma vai em tubo à parte.
- O tubo de **soro** vai para a soroteca após a análise. O de **sangue total** é descartado após a validação (não é arquivado).
- Jejum: se algum exame do tubo exige jejum, a coleta pede confirmação com o maior período.
- Delta check: variação acima do percentual do analito vira "suspeito". Valores de partida: glicose 50%, creatinina 40%, potássio 25%, hemoglobina 20%, demais 50%.

## Impacto no modelo de dados

O catálogo exige a tabela `analito` (e `faixa_referencia` ligada a ela), já refletida em [[Modelo-de-Dados]].

## Relacionadas

[[Dados-de-Semente]] · [[Regras-de-Negocio]] · [[Protocolos]]
