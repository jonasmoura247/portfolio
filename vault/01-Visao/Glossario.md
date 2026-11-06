---
tipo: visao
projeto: Bancada
atualizado: 2026-11-01
---

# Glossário

## Laboratório

| Termo | Significado |
|---|---|
| **LIS** | Sistema de informação laboratorial: o software que controla pedido, amostra, equipamento e laudo |
| **Analito** | A substância medida (glicose, potássio). Um exame pode ter vários |
| **Exame** | O que o médico pede; pode medir um ou mais analitos (hemograma) |
| **Amostra / tubo** | O material coletado do paciente, identificado por etiqueta |
| **Material** | Tipo de amostra: soro, sangue total com EDTA |
| **Soro** | Parte líquida do sangue depois de coagular e centrifugar |
| **Soroteca** | Arquivo refrigerado de amostras guardadas por um tempo para repetir exame ou tirar dúvida |
| **Pré-analítico** | Tudo antes do equipamento: pedido, jejum, coleta, transporte, triagem |
| **Analítico** | A medição no equipamento |
| **Pós-analítico** | Validação, liberação e laudo |
| **Triagem / recebimento** | Conferir se a amostra chegou certa (etiqueta, volume, hemólise) |
| **Hemólise** | Ruptura das hemácias na amostra; pode falsear resultados (ex.: potássio) e leva à rejeição |
| **Jejum** | Período sem comer antes de certos exames |
| **Faixa de referência** | Intervalo esperado para um analito, por sexo e idade |
| **Valor crítico** | Resultado tão fora que exige aviso imediato ao médico |
| **Flag H / L / C** | Alto, baixo, crítico |
| **Delta check** | Comparar com o resultado anterior do mesmo paciente para achar troca de amostra ou erro |
| **Validação técnica** | Conferência do analista antes de liberar |
| **Liberação** | Publicar o resultado e emitir o laudo |
| **Retificação** | Corrigir laudo já liberado, gerando nova versão |
| **TAT** | *Turnaround time*: tempo entre pedido (ou coleta) e liberação |
| **CQ** | Controle de qualidade: amostras de valor conhecido medidas para checar o equipamento |
| **Levey-Jennings** | Gráfico dos controles ao longo do tempo, com faixas de ±1, ±2 e ±3 desvios |
| **Regras de Westgard** | Critérios para dizer se uma série de controles indica erro (1-2s, 1-3s, 2-2s, R-4s, 4-1s, 10x) |
| **Cadeia de custódia** | Registro de quem manuseou a amostra, quando e onde |

## Integração e faturamento

| Termo | Significado |
|---|---|
| **ASTM E1381 / E1394** | Padrões antigos (hoje CLSI LIS01/LIS02) de comunicação entre equipamento e LIS |
| **HL7 v2** | Padrão de mensagens de saúde (`ORM` pedido, `ORU` resultado) |
| **FHIR** | Padrão moderno de recursos de saúde em JSON (proposta, [[Ideias-Extras]]) |
| **TISS** | Padrão de troca de informações entre operadoras e prestadores de saúde |
| **SADT** | Guia TISS de serviços auxiliares de diagnóstico e terapia (inclui exames) |

## Segurança (Security+ SY0-701)

| Termo | Significado |
|---|---|
| **CIA** | Confidencialidade, integridade, disponibilidade |
| **AAA** | Autenticação, autorização, *accounting* (registro) |
| **Defesa em profundidade** | Camadas independentes de proteção |
| **STRIDE** | Spoofing, Tampering, Repudiation, Information disclosure, DoS, Elevation of privilege |
| **IoC** | Indicador de comprometimento |
| **RTO / RPO** | Tempo máximo parado / quantidade máxima de dado perdido |
| **CSP** | Política que diz ao navegador de onde pode carregar script, estilo e imagem |
| **CSRF** | Fazer o navegador da vítima enviar uma requisição sem ela querer |
| **XSS** | Injetar script que roda no navegador de outra pessoa |
| **IDOR** | Acessar dado de outro usuário trocando um identificador |
| **SIEM / SOAR** | Correlação de logs / automação de resposta |
| **LGPD** | Lei Geral de Proteção de Dados |

## Relacionadas

[[Fluxo-do-Laboratorio]] · [[Controles-e-SecPlus]]
