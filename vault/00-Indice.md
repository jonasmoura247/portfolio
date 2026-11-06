---
tipo: indice
projeto: Bancada
atualizado: 2026-11-01
status: planejamento (fase 0 pronta para começar)
---

# Bancada — laboratório de demonstração

Mini LIS (sistema de informação laboratorial) **fictício e funcional** para o portfólio do Jonas. O visitante escolhe um paciente, agenda, atende, abre o prontuário, pede exame, guarda a amostra na soroteca, vê o equipamento processar e lê o resultado. Tudo com dados inventados, isolado por visitante, com um painel técnico que mostra o código, a segurança e **roda os testes de verdade**.

## Regras de ouro (pedidas pelo Jonas)

1. Arquitetura **MVC**.
2. **C#, JavaScript e SQL**.
3. Tudo que for usado é **MIT**, política estrita (única exceção: `SQLitePCLRaw`): [[Licencas-MIT]].
4. **Segurança** como requisito de primeira classe, com o material Security+ como referência: [[Controles-e-SecPlus]].
5. **Testes unitários** em tudo, **100% de cobertura** (linhas e ramos), o build falha abaixo disso: [[Estrategia-de-Testes]].
6. Uma **tela técnica** onde o visitante vê a parte técnica e roda os testes: [[Painel-Tecnico]].
7. Dados **100% fictícios**: [[Dados-Ficticios-e-LGPD]].
8. Visual **calmo, menos vibrante**: [[Design-do-Sistema]].

## Mapa

### 01 · Visão
[[Visao-e-Escopo]] · [[Personas-e-Jornada]] · [[Glossario]]

### 02 · Domínio
[[Fluxo-do-Laboratorio]] · [[Regras-de-Negocio]] · [[Modulos]] · [[Especificacao-das-Telas]] · [[Modelo-de-Dados]] · [[Dicionario-de-Dados]] · [[Catalogo-de-Exames]] · [[Equipamentos-Simulados]] · [[Dados-de-Semente]] · [[Catalogo-de-Eventos]]

### 03 · Arquitetura
[[Arquitetura-MVC]] · [[Decisoes-ADR]] · [[Convencoes-de-Codigo]] · [[Camada-de-Aplicacao]] · [[Rotas-e-Controllers]] · [[Sessao-e-Isolamento]] · [[Protocolos]] · [[Javascript-Modulos]] · [[Design-do-Sistema]] · [[Pipeline-CI-e-Container]]

### 04 · Segurança
[[Modelo-de-Ameacas]] · [[Controles-e-SecPlus]] · [[Matriz-de-Autorizacao]] · [[Corpus-de-Ataque]] · [[Cabecalhos-e-Configuracao-Segura]] · [[Cifra-e-Chaves]] · [[Auditoria-Encadeada]] · [[Dados-Ficticios-e-LGPD]] · [[Runbook-de-Incidente]]

### 05 · Qualidade
[[Estrategia-de-Testes]] · [[Plano-de-Testes-por-Modulo]] · [[Verificacao-com-Playwright]] · [[Licencas-MIT]]

### 06 · Painel técnico
[[Painel-Tecnico]] · [[Executor-de-Testes]] · [[Snapshot-do-Painel]]

### 07 · Plano
[[Roadmap-Fases]] · [[Checklists-por-Fase]] · [[Fase-0-Tarefas]] · [[Videos-por-Modulo]] · [[Ideias-Extras]] · [[Decisoes-em-Aberto]]

## Por onde começar a ler

1. [[Visao-e-Escopo]] e [[Personas-e-Jornada]]: o que é e para quem.
2. [[Arquitetura-MVC]] e [[Decisoes-ADR]]: como é feito e por quê.
3. [[Controles-e-SecPlus]] e [[Modelo-de-Ameacas]]: como se defende.
4. [[Roadmap-Fases]] e [[Fase-0-Tarefas]]: o que vem a seguir.

## Próximo passo

Começar a [[Fase-0-Tarefas]] com o SDK .NET 9 que já está instalado. A paleta está aprovada e o Git fica para o final ([[Decisoes-em-Aberto]]).

**Estado atual:** sistema construído e verificado localmente. O que mudou em relação ao plano está em [[Desvios-do-Plano]].
