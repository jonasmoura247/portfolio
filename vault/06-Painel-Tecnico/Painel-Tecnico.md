---
tipo: painel
projeto: Bancada
atualizado: 2026-11-01
---

# Painel técnico

A tela em que o tech lead confere o trabalho. Princípio: **tudo que o painel mostra é lido do sistema real**, nada desenhado à mão que possa mentir.

## Seções

| Seção | O que mostra | De onde vem |
|---|---|---|
| Visão geral | Selos: testes, cobertura, licenças, última execução, versão | Resultado das execuções |
| Arquitetura | Camadas e setas de dependência | Reflexão nos assemblies (referências reais) |
| Banco | Esquema, índices, restrições, scripts SQL, diagrama de relacionamento | `sqlite_master` da sessão e o SQL gerado pelas migrations |
| Segurança | Matriz Security+, cabeçalhos efetivos desta página, eventos de segurança da sessão, registro de riscos | JSON gerado de [[Controles-e-SecPlus]] + log da sessão |
| Licenças | Pacote, versão, licença, quem puxou | Mesma auditoria do teste |
| **Testes** | Botões "Rodar testes C#" e "Rodar testes JS", saída ao vivo, resumo por projeto | Executor (abaixo) |
| Cobertura | Por projeto, arquivo e linha; ramos; selo de 100% | Cobertura XML e relatório do V8 |
| Catálogo de testes | Lista de todos os testes agrupados por funcionalidade, com o nome em português | Reflexão nos atributos do NUnit e nomes do `node --test` |
| Decisões | ADRs | [[Decisoes-ADR]] |
| Código | Visualizador somente leitura de arquivos permitidos | Lista de permissão |
| Laboratório de ataque | Payloads contra a proteção real | [[Modulos]] #12 |

## Executor de testes (a parte perigosa)

Rodar processo no servidor a pedido de um anônimo é o ponto de maior risco do projeto. Por isso:

1. **Comando constante.** O cliente só diz "C#" ou "JS". Nenhum argumento, caminho ou variável vem do cliente (AC-11).
2. **Processo filho separado**, usuário sem privilégio, ambiente limpo, diretório de trabalho fixo, binários já compilados (`--no-build`).
3. **Uma execução por vez**, global. Quem chega durante outra vê o resultado em andamento (transmitido por eventos do servidor).
4. **Limite de taxa** por IP e global. **Tempo máximo** (por exemplo 120 s), depois mata a árvore de processos.
5. **Saída limitada** em tamanho e escapada ao exibir (a saída de teste é texto não confiável).
6. **Kill switch** por configuração: com ele desligado, o painel mostra a **última execução registrada no build** (instantâneo), então a página continua útil.
7. Banco e relógio dos testes são os do processo de teste, nunca os da sessão do visitante.
8. Cada execução gera evento de auditoria.

Alternativa de menor risco se a hospedagem não permitir processo filho: só o instantâneo do CI, mais os testes de JS rodando no próprio navegador. Fica como plano B.

## Dados que o build gera

Um passo do build (em `tools/`) grava `painel-instantaneo.json` com resultados, cobertura, licenças e matriz de segurança. É o que aparece com o executor desligado e o que o CI publica.

## Detalhamento

- Especificação completa do executor, salvaguardas e plano B: [[Executor-de-Testes]].
- Formato do arquivo gerado pelo CI e regras de uso: [[Snapshot-do-Painel]].
- Aparência e componentes: [[Design-do-Sistema]].

## Relacionadas

[[Estrategia-de-Testes]] · [[Modelo-de-Ameacas]] · [[Roadmap-Fases]]
