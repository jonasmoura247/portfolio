---
tipo: seguranca
projeto: Bancada
atualizado: 2026-11-01
---

# Corpus de ataque

Lista de entradas hostis usadas de duas formas: nos **testes automáticos** (toda entrada de texto passa por elas) e no **Laboratório de ataque** (o visitante dispara contra a defesa real). Como a política é estrita ([[Licencas-MIT]]), este corpus substitui um scanner externo.

São textos genéricos e públicos, usados apenas contra o próprio sistema.

## Categorias e o que se espera

| Categoria | Exemplos | Resultado esperado |
|---|---|---|
| **SQLi** | `' OR '1'='1` · `'; DROP TABLE paciente;--` · `1 UNION SELECT NULL--` · `" OR ""="` · `admin'--` | Tratado como texto comum; busca volta vazia, sem erro 500; esquema intacto (teste confere `sqlite_master` antes e depois) |
| **XSS** | `<script>alert(1)</script>` · `"><img src=x onerror=alert(1)>` · `<svg/onload=alert(1)>` · `javascript:alert(1)` · `'><iframe src=//x>` | Exibido como texto escapado; nenhum elemento novo no DOM; CSP bloqueia mesmo se algo escapasse |
| **Injeção de template** | `{{7*7}}` · `${7*7}` · `@(7*7)` · `<%= 7*7 %>` | Exibido literal, nunca `49` |
| **Travessia de caminho** | `../../etc/passwd` · `..\..\appsettings.json` · `%2e%2e%2f` · `....//` · `/painel/codigo?arquivo=C:\Windows\win.ini` | 400/404; nenhum arquivo fora da lista permitida é lido |
| **Injeção de linha (CRLF)** | `x%0d%0aSet-Cookie:a=b` · `x\r\nHeader: y` | Sem cabeçalho extra na resposta; sem linha extra no log |
| **Caractere especial** | byte nulo `%00` · Unicode de controle · texto com 100.000 caracteres · emoji de comprimento variável | Rejeitado ou truncado com mensagem; nunca 500 |
| **Corpo grande** | 1 MB em formulário · JSON com 50 níveis de profundidade | 413 / 400 antes de processar |
| **Verbo e método** | `PUT`/`DELETE` em rota de página · `GET` em rota que escreve | 405 |
| **Antifalsificação** | POST sem token · token de outra sessão · token reaproveitado após trocar papel | 400 e evento `AntifalsificacaoInvalido` |
| **Sessão** | cookie inventado · cookie de outra sessão · cookie truncado · sem cookie | Sessão nova (sem herdar nada) |
| **IDOR** | `/pacientes/{id}` com id de outra sessão ou inexistente | 404 idêntico nos dois casos (sem distinguir) |
| **Abuso de taxa** | 200 requisições em 10 s · 20 POSTs em 5 s | 429 a partir do limite; evento `LimiteDeTaxaExcedido` |
| **Parâmetro extra** | `?comando=rm -rf` em `/painel/testes/executar` · campo `args` no corpo | Ignorado; o comando é constante |

## Como os testes usam

1. Arquivo `tests/dados/corpus-de-ataque.json` com `{categoria, entrada, esperado}`.
2. Um teste parametrizado lista **toda entrada de texto** do sistema (campos de formulário e parâmetros de consulta, descobertos por reflexão nos modelos de entrada) e dispara cada payload aplicável.
3. Para cada disparo confere: status esperado, corpo sem payload cru, cabeçalhos sem injeção, esquema do banco intacto, evento de auditoria presente quando devia.
4. Entrada de texto **nova** que não esteja na lista do teste faz o teste falhar (ninguém esquece de cobrir um campo).

## Laboratório de ataque (tela)

- O visitante escolhe uma categoria e um payload da lista (ou digita o seu, com limite de 500 caracteres).
- A tela escolhe o alvo real (campo de busca de pacientes, campo de evolução, rota de código) e mostra: o que foi enviado, **a resposta**, qual defesa agiu e **o evento gravado** na auditoria.
- Não existe endpoint vulnerável de brinquedo. O que se vê é a defesa de verdade.
- Disponível por padrão e desligável em `Bancada:LabAtaque:Habilitado`; sujeito ao limite de taxa.

## Relacionadas

[[Modelo-de-Ameacas]] · [[Cabecalhos-e-Configuracao-Segura]] · [[Estrategia-de-Testes]]
