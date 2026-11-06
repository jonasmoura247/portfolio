---
tipo: plano
projeto: Bancada
atualizado: 2026-11-01
---

# O que mais faz sentido colocar

Ordenado por valor para quem está avaliando o Jonas. Os itens 1 a 6 já estão no plano; os demais são propostas.

## Já incluídos no plano

1. **Protocolo de equipamento de verdade (ASTM E1381/E1394 e HL7 v2).** Bate direto com a experiência da C4 em integração por rede e porta serial. É o diferencial técnico do projeto.
2. **Controle de qualidade com Levey-Jennings e regras de Westgard.** Coisa que só quem conhece laboratório faz.
3. **Valores de referência por sexo e idade, flags H/L/crítico e delta check.**
4. **Cadeia de custódia da amostra.** É o conceito forense do Security+ (domínio 4) aplicado ao domínio real.
5. **Auditoria encadeada por hash**, com botão "verificar" e botão "simular adulteração".
6. **Laboratório de ataque.** O visitante tenta quebrar e vê a defesa e o log funcionando.

## Propostas novas

| # | Ideia | Valor | Custo | Observação |
|---|---|---|---|---|
| 7 | **Relógio acelerado** (×1, ×60, ×600) e botão pausar | Alto | P | Sem isso o exame demora e o visitante vai embora |
| 8 | **Cenários prontos** (valor crítico, hemólise, equipamento com erro, CQ fora) | Alto | M | Mostra as regras sem o visitante precisar descobri-las |
| 9 | **Tour guiado de 5 minutos** | Alto | M | Recrutador não vai explorar sozinho |
| 10 | **Exportação FHIR R4** (Patient, ServiceRequest, Specimen, Observation, DiagnosticReport) gerada à mão em JSON | Médio-alto | M | Interoperabilidade; sem biblioteca (a oficial é BSD-3) |
| 11 | **Guia SADT simulada (TISS)** a partir do pedido | Médio-alto | M | Conecta com o que o Jonas faz na Moderna; versão simplificada, sem XML oficial completo |
| 12 | **Painel operacional**: tempo de resposta (TAT) por exame, fila por equipamento, ocupação da soroteca | Médio | M | Gráficos em SVG feitos à mão |
| 13 | **Etiqueta imprimível** com Code 128 e CSS de impressão | Médio | P | Já usa o gerador do módulo 6 |
| 14 | **Linha do tempo do paciente** unificando agenda, atendimento, amostra, resultado | Médio | P | Vem quase de graça da auditoria |
| 15 | **Observabilidade** (`System.Diagnostics.Metrics` + página de métricas) | Médio | P | Mostra maturidade operacional |
| 16 | **Modo claro/escuro e acessibilidade** (teclado, contraste, leitor de tela) | Médio | M | Teste de contraste por cálculo, sem biblioteca |
| 17 | **Runbook de incidente no próprio painel** | Médio | P | Fecha o ciclo do Security+ domínio 4 |
| 18 | ~~Teste de mutação (Stryker)~~ | — | — | Fora: Stryker é Apache-2.0 e a política é estrita. Alternativa: testes de propriedade e de máquina de estados exaustivos |
| 19 | **SBOM** visível | Baixo-médio | P | Sai da auditoria de licenças |

## Tentador, mas não

| Ideia | Por que não |
|---|---|
| Login real, OAuth | Cria superfície que o projeto escolheu eliminar |
| Upload de PDF de exame | Idem |
| IA interpretando resultado | Soaria como apoio clínico; aviso médico não resolve |
| Banco persistente compartilhado | Perde o isolamento por visitante |
| Notificação por e-mail/SMS | Saída de rede, custo, abuso |

## Relacionadas

[[Roadmap-Fases]] · [[Modulos]]
