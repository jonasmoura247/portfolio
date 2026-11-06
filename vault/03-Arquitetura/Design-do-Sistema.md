---
tipo: arquitetura
projeto: Bancada
atualizado: 2026-11-01
status: paleta aprovada pelo Jonas em 01/11/2026
---

# Design do sistema

Direção do Jonas: **dev moderno, com mais cor que um cinza neutro, mas sem vibrar**. Vale para o Bancada e para o portfólio, que compartilham os mesmos tokens. A prévia aprovada está em `Bancada/previa-visual.html` e o portfólio já convertido em `Projetos/portfolio-jonas.html`.

## Princípios

1. **Base azul-ardósia**, três acentos suaves e cor forte só para estado.
2. **Cor é informação.** Estado nunca só por cor: sempre texto, ícone ou letra junto (acessibilidade).
3. **Sem neon**: nada de gradiente de fundo, brilho colorido ou sombra colorida. Único efeito de luz: o anel suave do indicador "ok".
4. **Dois temas** (claro e escuro) com os mesmos papéis de cor.
5. **Detalhes de dev**: rótulos em fonte monoespaçada com `//`, destaque de uma palavra no título, fundo de pontos sutis, teclas `kbd`, código colorido.
6. **Movimento mínimo**: barras que crescem ao abrir e um pulso no marcador do mapa; tudo desligado com `prefers-reduced-motion`.

## Tokens

| Papel | Token | Claro | Escuro |
|---|---|---|---|
| Fundo da página | `--fundo` | `#f5f6fa` | `#0e1219` |
| Superfície | `--superficie` | `#ffffff` | `#141922` |
| Superfície 2 (campos, código, zebra) | `--superficie-2` | `#eef0f6` | `#1a2030` |
| Linha | `--linha` | `#dde1ea` | `#262e3d` |
| Texto | `--texto` | `#1a2030` | `#e4e8f0` |
| Texto secundário | `--texto-2` | `#596378` | `#98a3b8` |
| **Acento · verde** (botão, foco, aba ativa, filetes; decisão do Jonas) | `--acento` | `#20d676` | `#20d676` |
| **Acento · verde, como texto** (link, destaque em texto; `#20d676` não passa o contraste sobre claro) | `--acento-texto` | `#0a7a3c` | `#20d676` |
| **Acento 2 · petróleo** (código, dados, progresso) | `--acento-2` | `#1a7a70` | `#5cc1b2` |
| **Acento 3 · violeta** (front-end, segurança) | `--acento-3` | `#6b4fb3` | `#b4a0e6` |
| Ok | `--ok` | `#277a4c` | `#72c492` |
| Atenção | `--atencao` | `#8a5a0a` | `#e0b062` |
| Perigo | `--perigo` | `#b03a48` | `#ee8d98` |
| Texto sobre o acento | `--sobre-acento` | `#ffffff` | `#0e1219` |

**Contraste** (WCAG, texto sobre `--fundo`; sobre as superfícies o mínimo é 4,5:1 em todos): texto 15,0 / 15,3 · texto secundário 5,6 / 7,4 · índigo 5,7 / 7,5 · petróleo 4,8 / 8,7 · violeta 5,7 / 8,1 · ok 4,9 / 9,0 · atenção 5,5 / 9,4 · perigo 5,5 / 8,0 (claro / escuro). Botão primário: 6,1 / 7,5. Um teste automático recalcula tudo a partir do CSS e falha se alguém trocar um token e quebrar o contraste.

**Uso dos acentos por tipo de informação**
- Índigo: backend (C#, ASP.NET), ação principal, aba ativa, "confirmado".
- Petróleo: dados e bancos (SQL), progresso, código, rótulos `//`.
- Violeta: front-end, JavaScript, segurança e IA, textos cifrados, correções do prontuário.

## Tipografia

Fontes **do sistema** (`system-ui`; `ui-monospace` para códigos e rótulos). Nenhuma licença extra ([[Licencas-MIT]]), nenhuma requisição de terceiros (CSP `font-src 'self'`).

| Uso | Tamanho | Peso |
|---|---|---|
| Título de página | `clamp(26px, 4.5vw, 36px)` | 700, `letter-spacing -.01em` |
| Título de seção | 18 px | 600 |
| Texto | 15 px (17 px no portfólio) | 400 |
| Rótulo (maiúsculas, +0.06em) | 12 px | 600 |
| Rótulo dev (`// assunto`) | monoespaçada 13 px, petróleo | 500 |
| Códigos, etiquetas, ids | monoespaçada 13 px | 500 |

## Componentes

| Componente | Regra |
|---|---|
| Cartão | raio 10, borda 1 px, sem sombra; destaque com filete de 2 px no topo em índigo |
| Botão | primário (índigo cheio), secundário (contorno), perigo (contorno vermelho); altura mínima 36 px (40 px em toque) |
| Selo de estado | pílula com ponto + texto, cor e fundo a 9% da mesma cor |
| Chip de tecnologia | mono 12,5 px, colorido por categoria (índigo/petróleo/violeta) ou neutro |
| Tabela | cabeçalho em maiúsculas, linhas de ~36 px, zebra sutil, números alinhados |
| Resultado fora da faixa | bandeira `H ▲`, `L ▼`, `CH ▲▲`, `CL ▼▼`; crítico com fundo a 12% |
| Alerta | faixa com marca `!` / `!!` em mono e borda da cor do nível |
| Mapa da soroteca | grade 9×9: livre (superfície 2), ocupada (índigo 28%), vence em 1 dia (atenção 32%), vencida (perigo 30%) |
| Gráficos | SVG feito à mão: barras horizontais, linha do tempo, rosca, Levey-Jennings, diagramas; cores pelos tokens; título `// assunto` e nota à direita |
| Mapa geográfico | contornos do Natural Earth (via `geo.json`) convertidos em SVG; país em destaque a 24% de índigo; marcador petróleo com anel pulsante |
| Aviso de dados fictícios | faixa fina e fixa no topo, atenção a 12% |

## Layout

- Barra superior: marca (monograma + nome), seletor de papel e operador, tema, idioma, "Reiniciar sessão"; abas ou menu abaixo.
- Menu lateral do sistema com os módulos do papel atual; em celular (400 px) vira faixa rolável no topo.
- Conteúdo em coluna de até 1120 px (sistema) ou 880 px (portfólio).
- Nada rola na horizontal na página inteira; tabelas rolam dentro do próprio contêiner.

## Acessibilidade

Teclado completo, foco visível (anel de 2 px no índigo), HTML semântico, rótulo em todo controle, `aria-live` para avisos, `role="img"` com `aria-label` descritivo em todo gráfico, grade da agenda e mapa da soroteca com alternativa em lista. Testes: contraste por cálculo, `alt` e rótulos nas Views.

## Segurança do front-end

Zero `<script>` e `style=""` inline no Bancada (CSP sem `unsafe-inline`, [[Decisoes-ADR]] ADR-007): valores dinâmicos de CSS (largura de barra, progresso) entram por propriedade personalizada via JavaScript (`style.setProperty`). A prévia e o portfólio, por serem artefatos, usam estilo inline; o sistema real não.

## Relacionadas

[[Arquitetura-MVC]] · [[Convencoes-de-Codigo]] · [[Javascript-Modulos]] · [[Especificacao-das-Telas]]
