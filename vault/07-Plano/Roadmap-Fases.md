---
tipo: plano
projeto: Bancada
atualizado: 2026-11-01
---

# Roadmap em fases

Ordem pensada para que **cada fase termine com algo que roda e aparece no painel técnico**. Tamanho: P (pequeno), M (médio), G (grande). Toda fase cumpre a definição de pronto de [[Estrategia-de-Testes]].

| Fase | Entrega | Tam. | Termina quando |
|---|---|---|---|
| **0 · Fundação** | Solução com projetos, `.editorconfig`, avisos como erro, limites de 100%, auditoria de licenças, teste de arquitetura, paridade de idiomas, script local `tools/verificar`, `ci.yml` escrito, Dockerfile | M | `dotnet test` e `node --test` verdes com cobertura travada, em projeto quase vazio |
| **1 · Núcleo seguro** | Sessão com banco em memória, banco-modelo + semente, papéis e política de autorização, cabeçalhos e CSP, limite de taxa, antifalsificação, auditoria encadeada, página de erro, health check | G | Matriz de autorização e corpus de segurança verdes |
| **2 · Painel técnico v1** | Seções Visão geral, Testes, Cobertura, Licenças e executor com todos os limites. Instantâneo do build | G | Visitante roda a suíte e vê 100% |
| **3 · Pacientes e agenda** | Módulos 1, 2 e 3 | M | Agendar e fazer check-in pela tela |
| **4 · Prontuário e pedido** | Módulos 4 e 5, alergias, texto cifrado | M | Médico atende e pede exame |
| **5 · Coleta e soroteca** | Módulos 6 e 7, Code 128, mapa de racks, validade, cadeia de custódia | G | Amostra etiquetada e guardada na posição certa |
| **6 · Equipamento** | Módulo 8, protocolo ASTM e HL7, relógio de simulação, falhas e reprocesso | G | Exame vai e volta pelo protocolo e aparece o resultado |
| **7 · Resultado e laudo** | Módulos 9 e 10, flags, delta check, Westgard, validação em duas etapas, laudo imprimível | G | Jornada completa do agendamento ao laudo |
| **8 · Acabamento** | Tour guiado, cenários, Laboratório de ataque, painel completo (arquitetura, banco, segurança, catálogo, código), inglês, acessibilidade, celular, revisão final do corpus de segurança, roteiros e resumos de cada módulo para os vídeos de [[Videos-por-Modulo]] | G | Critérios de sucesso de [[Visao-e-Escopo]] |
| **9 · Publicação** | Repositório Git público, CI rodando no GitHub, hospedagem, domínio, README com GIFs, link no portfólio | P | Link público funcionando |

## Por que o painel vem na fase 2

Cada fase seguinte já nasce visível no painel, e o executor (a parte arriscada) é testado cedo, com pouca coisa em volta para atrapalhar.

## Riscos do plano

| Risco | Resposta |
|---|---|
| Escopo grande | Fatias verticais; cada fase é entregável sozinha; fases 5 a 7 podem encolher sem quebrar o resto |
| 100% de cobertura atrasar | Código novo nasce com teste; lista de exclusões fechada |
| Hospedagem não aceitar processo filho | Plano B do [[Painel-Tecnico]] |
| Licença surpresa em dependência transitiva | Auditoria como teste desde a fase 0 |

## Relacionadas

[[Ideias-Extras]] · [[Decisoes-em-Aberto]]
