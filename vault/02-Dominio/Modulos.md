---
tipo: dominio
projeto: Bancada
atualizado: 2026-11-01
---

# Módulos

Cada módulo é uma fatia vertical: controller, view, JavaScript da tela, caso de uso, repositório, SQL e testes. Entrega por fase em [[Roadmap-Fases]]. Campos, validações e mensagens de cada tela: [[Especificacao-das-Telas]]. Colunas do banco: [[Dicionario-de-Dados]].

## 1. Pacientes

- **Telas:** lista com busca; ficha do paciente (dados, alergias, linha do tempo).
- **Regras:** busca parametrizada e paginada; sem CPF.
- **JS:** busca com atraso (debounce) e cancelamento de requisição antiga.
- **Testes-chave:** busca com payload de SQL injection volta vazia sem erro; paginação nos limites.

## 2. Agenda e agendamento

- **Telas:** agenda semanal/diária por profissional; modal de agendamento.
- **Regras:** grade fixa, sem dupla reserva, sem passado, confirmação, falta automática.
- **JS:** grade da agenda desenhada por função pura `montarGrade(grade, agendamentos)`; navegação por teclado.
- **Testes-chave:** duas reservas simultâneas no mesmo horário, só uma vence (UNIQUE + domínio).

## 3. Atendimento

- **Telas:** fila de chegada, tela de atendimento.
- **Regras:** check-in só no dia; só o profissional do horário atende.
- **Testes-chave:** matriz estado × evento do agendamento.

## 4. Prontuário

- **Telas:** resumo, evoluções em ordem cronológica, alergias, exames anteriores.
- **Regras:** evolução imutável, correção encadeada; texto cifrado em repouso; tamanho máximo.
- **JS:** editor simples de texto com contador; sem HTML.
- **Testes-chave:** `<script>` digitado volta escapado; evolução corrigida mostra as duas versões.

## 5. Pedido de exame

- **Telas:** catálogo com busca, carrinho de exames, resumo do pedido.
- **Regras:** alerta de alergia, jejum, volume e material agregados por tubo.
- **Testes-chave:** dois exames do mesmo material viram um tubo só.

## 6. Coleta e etiqueta

- **Telas:** lista de pedidos a coletar; etiqueta imprimível.
- **Regras:** etiqueta única; confirmação de jejum; rejeição com motivo.
- **JS:** gerador de **Code 128** em SVG, função pura.
- **Testes-chave:** o código gerado decodifica de volta ao texto original (ida e volta).

## 7. Soroteca

- **Telas:** mapa visual dos racks e caixas (grade 9×9), busca por amostra, ocupação, vencimentos.
- **Regras:** uma amostra por posição, validade, descarte registrado, cadeia de custódia.
- **JS:** mapa de calor de ocupação; clique move amostra (arrastar com teclado também).
- **Testes-chave:** guardar duas amostras na mesma posição falha no domínio e no banco.

## 8. Equipamento e processamento

- **Telas:** painel do analisador (fila, status, luzes), monitor do protocolo (quadros trocados), controle do relógio de simulação.
- **Regras:** só processa exames suportados; falha sorteada com semente; reprocessa até 2 vezes.
- **Protocolo:** quadros no estilo ASTM E1381/E1394 (ENQ/ACK, STX, número do quadro, texto, ETX, checksum, CR LF) e mensagens HL7 v2 (ORM^O01 pedido, ORU^R01 resultado). Biblioteca própria, pura, em `Bancada.Protocolos`.
- **Relógio:** serviço `IRelogio` injetável. Em produção acelera (por exemplo ×60) para o exame de 20 minutos terminar em 20 segundos. Nos testes é manual e determinístico.
- **Testes-chave:** quadro com checksum errado é rejeitado; quadro cortado ao meio; ida e volta de cada tipo de registro (H, P, O, R, L).

## 9. Resultado, validação e laudo

- **Telas:** resultados pendentes, validação, laudo (imprimível), histórico de versões.
- **Regras:** flags H/L/C por faixa, delta check contra o resultado anterior, validação em duas etapas, separação de deveres, laudo imutável.
- **Testes-chave:** valor crítico exige confirmação; mesmo usuário não executa e valida.

## 10. Controle de qualidade

- **Telas:** gráfico de Levey-Jennings por equipamento e exame.
- **Regras:** regras de Westgard (1-2s aviso, 1-3s, 2-2s, R-4s, 4-1s, 10x) bloqueiam liberação.
- **JS:** gráfico SVG feito à mão, sem biblioteca.
- **Testes-chave:** cada regra com séries que a violam e séries que não violam.

## 11. Painel técnico

Ver [[Painel-Tecnico]].

## 12. Laboratório de ataque

Tela educativa: biblioteca de payloads (SQLi, XSS, travessia de caminho, CRLF, payload gigante). O visitante dispara contra **a proteção real** (não contra um endpoint vulnerável de brinquedo), vê a resposta e o evento aparecer no log de segurança. Ver [[Controles-e-SecPlus]].

## Relacionadas

[[Fluxo-do-Laboratorio]] · [[Modelo-de-Dados]] · [[Arquitetura-MVC]]
