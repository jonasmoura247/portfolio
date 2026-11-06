---
tipo: arquitetura
projeto: Bancada
atualizado: 2026-11-01
---

# Módulos JavaScript

JavaScript puro (ES modules), sem framework, sem bundler, sem dependência. Cada arquivo é pequeno e tem um papel só.

## Estrutura

```
src/Bancada.Web/wwwroot/js/
  comum/
    http.js            fetch com token antifalsificação, tempo máximo e tratamento de erro
    dom.js             helpers que só usam textContent / setAttribute (nunca innerHTML)
    formato.js         datas, números e unidades em pt-BR/en via Intl
    relogio.js         relógio de simulação do lado do cliente (exibição)
  agenda/
    grade-agenda.js    lógica pura: montarGrade(grade, agendamentos) → matriz de células
    tela-agenda.js     liga a grade ao DOM, teclado e modal
    index.js           ponto de entrada da página
  soroteca/
    mapa-soroteca.js   lógica pura: ocupação, vencimento, busca por posição
    tela-soroteca.js
    index.js
  etiqueta/
    code128.js         lógica pura: texto → módulos → SVG
  equipamentos/ ...    estado da fila, monitor de quadros, SSE
  qualidade/
    levey-jennings.js  lógica pura + geração do SVG
  painel/ ...          executor de testes (SSE), tabelas, cobertura
  pacientes/ ...       busca com atraso e cancelamento
tests/js/
  _apoio/dom-falso.js  DOM de mentira escrito à mão
  agenda/*.test.js ...
```

Cada página Razor inclui **um** `<script type="module" src="...">`. Nenhum script inline, nenhum `style=""` ([[Decisoes-ADR]] ADR-007).

## Lógica pura × ligação com o DOM

| Arquivo | O que tem | Como testa |
|---|---|---|
| `grade-agenda.js` | Funções sem DOM: entrada de dados, saída de dados | Teste direto, tabela de casos |
| `tela-agenda.js` | Recebe `documento` e `http` por parâmetro e conecta eventos | DOM de mentira + `http` falso |

O DOM de mentira implementa só o que o código usa (`createElement`, `createElementNS`, `appendChild`, `textContent`, `setAttribute`, `addEventListener`, `dispatchEvent`, `querySelector` simples). Cresce com o projeto, e tudo nele também é coberto por teste.

## Regras

1. **Proibido `innerHTML`, `outerHTML`, `insertAdjacentHTML`, `document.write`, `eval`, `new Function`.** Teste varre `wwwroot/js` atrás desses nomes e falha se achar.
2. Dado vindo do servidor entra no DOM por `textContent` ou `setAttribute`, nunca como HTML.
3. Estilo dinâmico por propriedade CSS personalizada (`elemento.style.setProperty('--largura', ...)`), permitido pela CSP.
4. Toda chamada ao servidor passa por `comum/http.js` (token, tempo máximo de 10 s, `AbortController`).
5. Sem estado global; estado vive em objetos criados pela função de entrada.
6. Textos de interface vêm do HTML (atributos `data-texto-*` preenchidos pelo servidor a partir dos recursos), nunca fixos no JS.
7. Funções de até ~25 linhas; arquivos de até ~200.

## Cobertura de 100% sem pontos cegos

O `node --test` só mede arquivos que foram **importados** por algum teste. Para não existir arquivo esquecido, há um teste-guarda que percorre `wwwroot/js` e importa todos os `.js`; arquivo sem teste aparece com 0% e o limite de 100% derruba o build.

Comando completo:

```
node --test --experimental-test-coverage \
  --test-coverage-lines=100 --test-coverage-branches=100 --test-coverage-functions=100 \
  --test-coverage-include="src/Bancada.Web/wwwroot/js/**" tests/js
```

Flags conferidos no Node 24 instalado; reconferir `--test-coverage-include` na Fase 0.

## Acessibilidade em JS

Grade da agenda e mapa da soroteca navegáveis por setas, `Enter` ativa, `Esc` fecha. Foco devolvido ao elemento de origem ao fechar modal. `aria-live="polite"` para mensagens. Cada comportamento tem teste com eventos de teclado simulados.

## Relacionadas

[[Design-do-Sistema]] · [[Estrategia-de-Testes]] · [[Modulos]]
