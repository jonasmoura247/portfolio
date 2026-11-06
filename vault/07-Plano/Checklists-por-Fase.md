---
tipo: plano
projeto: Bancada
atualizado: 2026-11-01
---

# Checklists por fase

Cada fase termina quando **tudo** da lista está marcado **e** a definição de pronto de [[Estrategia-de-Testes]] vale (100% C# e JS, segurança, licenças, vault atualizado). **Cada módulo**, ao terminar, passa pelo Playwright ([[Verificacao-com-Playwright]]) e o resultado é anotado no registro de andamento no fim desta nota. Visão geral em [[Roadmap-Fases]].

## Fase 0 · Fundação

Passo a passo em [[Fase-0-Tarefas]].

- [x] SDK .NET 9 (9.0.102 já instalado) e Node 24 verificados
- [x] Solução, projetos de código e de teste criados (5 de código, 5 de teste com 100%, mais `Bancada.Testes.Guardas` para arquitetura, licenças, container e CI)
- [x] `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, restauração travada
- [x] Teste de arquitetura (setas de dependência, proibições)
- [x] Teste de auditoria de licenças com a exceção do SQLitePCLRaw (hoje 100% MIT; a exceção só vale quando o Sqlite entrar)
- [x] Teste de exclusões de cobertura (`ExcludeFromCodeCoverage` proibido em `src`)
- [x] Infra de idiomas com testes de paridade e texto solto (entra com a primeira View, Fase 1)
- [x] Estrutura de JS com `node --test`, DOM falso e teste-guarda (100% de linhas, ramos e funções)
- [x] `tools/verificar.ps1` rodando local os 6 passos do CI; `ci.yml` escrito (só roda no GitHub, no final)
- [x] Dockerfile (imagem chiseled, usuário sem privilégio) com teste de requisitos
- [ ] Repositório Git: **adiado para o final** (Fase 9)

## Fase 1 · Núcleo seguro

- [x] Sessão: **feito** repositório com TTL ocioso e total, teto de sessões, id renovado a cada troca, chave de cifra por sessão e operador ([[Sessao-e-Isolamento]]); **falta** o banco em memória por sessão (EF Core + SQLite, cópia do modelo) e as cotas de registros
- [x] Banco-modelo: mapeamentos, migrations, gatilhos da auditoria (próximo passo: entra EF Core 9 + SQLite e o teste de licença passa a ver a exceção do SQLitePCLRaw)
- [x] Papéis, enum `Acao` (49 ações), atributo `[Autorizar]` e matriz testada por extenso, mais guarda que exige `[Autorizar]` em toda action ([[Matriz-de-Autorizacao]]). O teste papel × action pela rede cresce a cada módulo
- [x] Cabeçalhos, CSP, limite de taxa (nova sessão, leitura, escrita), antifalsificação ligada à sessão, páginas de erro genéricas, `/saude` ([[Cabecalhos-e-Configuracao-Segura]])
- [x] Cifra de campo AES-256-GCM ([[Cifra-e-Chaves]])
- [x] Auditoria encadeada: **feito** cadeia por hash, serviço e eventos de sessão/papel/operador/recusa ([[Auditoria-Encadeada]]); **falta** a tela de Auditoria (verificar e simular adulteração) e gravar os eventos de segurança que hoje só o servidor enxerga (antifalsificação inválida, limite de taxa excedido)
- [x] Corpus de ataque rodando em todo campo existente ([[Corpus-de-Ataque]])
- [x] Layout base, tema claro/escuro, seletor de papel e operador, textos pt/en com guardas de paridade, chave usada, texto solto e contraste ([[Design-do-Sistema]])

## Fase 2 · Painel técnico v1

- [x] Executor por `dotnet test --no-build`, sem NUnitLite ([[Desvios-do-Plano]])
- [x] Gerador do instantâneo e esquema ([[Snapshot-do-Painel]])
- [x] Telas: Visão geral, Testes, Cobertura, Licenças
- [x] Executor com as 12 salvaguardas e transmissão ao vivo
- [x] Kill switch testado
- [x] Rastreabilidade RN → teste

## Fase 3 · Pacientes e agenda

- [x] Semente de pacientes, profissionais e grade ([[Dados-de-Semente]])
- [x] Módulos 1, 2 e 3 completos (domínio, caso de uso, controller, View, JS)
- [x] RN-001 a RN-008 com teste
- [x] Grade da agenda acessível por teclado

## Fase 4 · Prontuário e pedido

- [x] Evoluções cifradas e encadeadas; alergias
- [x] Catálogo de exames e faixas ([[Catalogo-de-Exames]])
- [x] Pedido com agrupamento de tubos e alerta de alergia
- [x] RN-010 a RN-013 e RN-020 a RN-021 com teste

## Fase 5 · Coleta e soroteca

- [x] Coleta, jejum, volume, rejeição e recoleta
- [x] Code 128 e etiqueta imprimível ([[Protocolos]])
- [x] Soroteca com mapa, validade, descarte e cadeia de custódia
- [x] RN-022 a RN-025 e RN-030 a RN-035 com teste

## Fase 6 · Equipamento

- [x] Biblioteca ASTM e HL7 com testes de borda
- [x] Três equipamentos, fila, capacidade, reprocesso ([[Equipamentos-Simulados]])
- [x] Relógio de simulação
- [x] Monitor do protocolo
- [x] RN-040 a RN-043 com teste

## Fase 7 · Resultado e laudo

- [x] Flags, faixas por sexo e idade, delta check
- [x] Validação em duas etapas, separação de deveres, valor crítico
- [x] Controle de qualidade com Westgard e gráfico
- [x] Laudo imprimível e retificação
- [x] RN-044, RN-050 a RN-056 com teste
- [x] Jornada completa do agendamento ao laudo funcionando

## Fase 8 · Acabamento

- [x] Tour guiado e 6 cenários ([[Personas-e-Jornada]])
- [x] Laboratório de ataque
- [x] Painel completo: arquitetura, banco, segurança, catálogo de testes, código, ADRs
- [x] Inglês completo e paridade
- [x] Acessibilidade revisada; layout em 400 px
- [x] Revisão final do corpus e do modelo de ameaças
- [x] Ideias extras: relógio, cenários, tour, painel operacional e etiqueta entraram; FHIR e TISS ficaram de fora ([[Desvios-do-Plano]])

## Fase 9 · Publicação

- [ ] **Migrar de .NET 9 para .NET 10 LTS** (o 9 sai de suporte em 10/11/2026): instalar o SDK 10, trocar `TargetFramework`, `global.json`, imagem do container e pacotes 9.x para 10.x; rodar `tools/verificar` com 100%
- [ ] Hospedagem escolhida pelo Jonas (**decisão adiada para o fim**)
- [ ] README com como rodar, arquitetura, o que é simulado
- [ ] Verificação ponta a ponta no ambiente publicado
- [ ] Executor ligado ou desligado conforme [[Decisoes-em-Aberto]]

## Vídeos por módulo (tarefa do Jonas, depois do sistema pronto)

- [ ] Escolher a skill e o formato ([[Videos-por-Modulo]])
- [ ] Um vídeo de 60 a 90 s por módulo, a partir do roteiro `tools/e2e/<modulo>.md`
- [ ] Anexar ao portfólio e ao Bancada

## Portfólio (tarefa à parte)

- [ ] Refazer o portfólio com os tokens e a tipografia de [[Design-do-Sistema]] (menos vibrante)
- [ ] Incluir o Bancada como projeto principal, com link e selos
- [ ] Manter só projetos com Git público (ver conversa de 31/10: Konta e rpg-mesa hoje)

## Relacionadas

[[Roadmap-Fases]] · [[Fase-0-Tarefas]]

## Registro de andamento

| Data | Fase / módulo | O que foi feito | Playwright |
|---|---|---|---|
| 01/11/2026 | Planejamento | Vault de 46 notas, esboço interativo aprovado, execução autorizada de ponta a ponta | n/a |
| 01/11/2026 | **Fase 0 · Fundação** | Solução .NET 9, portões de 100% (provados: reprovam de verdade em C# e JS), guardas de arquitetura e licenças (tudo MIT), `tools/verificar` verde nos 6 passos, Dockerfile e `ci.yml` | `/saude` em Kestrel real: 200, cabeçalhos completos, sem `Server`. Roteiro em `tools/e2e/fase0-saude.md` |
| 01/11/2026 | **Fase 1 · Núcleo seguro (parcial)** | Matriz de autorização, cadeia de auditoria, cifra AES-GCM, sessões (TTL, teto, id renovado), cookie `__Host-`, antifalsificação por sessão, limite de taxa, erros genéricos, layout com tema, textos pt/en. 232 testes de C# e 17 de JS a 100%, `tools/verificar` verde | 11 passos em `tools/e2e/fase1-nucleo.md`: passou. Bug achado no navegador (cookie `__Host-` sem Secure no modo local) virou teste |
| 01/11/2026 | **Fases 1 a 8 · Sistema completo** | Banco por sessão (EF Core, migrations, gatilhos, semente), 15 módulos, cenários, idiomas, painel técnico, executor, laboratório de ataque. `tools/verificar` verde a 100% | Jornada do agendamento ao laudo, 5 papéis, cenários hemolisada, erro e CQ, tema, inglês, executor JS ao vivo, lab de ataque, 400 px sem rolagem. Roteiros em `tools/e2e/`. Bugs achados em [[Desvios-do-Plano]] |
| 06/11/2026 | **Portfólio na raiz + acento verde** | Currículo/portfólio virou a tela inicial (`/`, estático, sem sessão); a Bancada passou para `/bancada`, com botão "Portfólio" de volta. Acento `#20d676` (texto `#0a7a3c` no claro). Testes C# e JS a 100% | `/` carrega sem erro (só favicon 404), botão "Abrir o Bancada" leva a `/bancada`, verde em botão, foco e filetes no tema escuro. Tema claro e 400 px não conferidos |
| 06/11/2026 | **Ajustes de uso** | Modal (`<dialog>`) na agenda (célula livre ou ocupada) e no resumo do atendimento (evolução só para Médico/Admin); linhas inteiras clicáveis em Atendimento, Pedidos e Coleta (botão "Ver etiqueta"); Operação com números e gráficos maiores; telas em 100% da largura e topo compacto no celular; Laboratório de ataque removido (o corpus hostil continua nos testes). `tools/verificar` verde a 100% | Agenda (modal livre e ocupado), fila (resumo como Recepção), Operação em 1920 px e agenda/modal/início em 390 px. Admin e tema claro não conferidos no navegador |
| 06/11/2026 | **Abas "Consultar etiqueta" e "Consultar soroteca" + ponta a ponta** | Nova aba lateral Consultar etiqueta (`/etiquetas`: busca por etiqueta, código ou nome do paciente; Coleta, Analista e Admin); aba da soroteca renomeada para Consultar soroteca. `tools/verificar` verde a 100% | Roteiros em `tools/e2e/ponta-a-ponta-{1,2,3}.js` (browser_run_code_unsafe): portfólio, agenda com modal, fila, atendimento, pedido, coleta, etiqueta, soroteca, equipamento, validação e laudo (28 passos) e Admin em 15 telas x 5 larguras (1920 a 360 px) sem rolagem lateral, tema, volta ao portfólio e console sem erros: tudo passou |
