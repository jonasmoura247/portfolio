---
tipo: plano
projeto: Bancada
atualizado: 2026-11-01
---

# Decisões

## Decididas

| # | Decisão | Resposta do Jonas | Data |
|---|---|---|---|
| 1 | Banco e regra MIT | **SQLite + exceção documentada** do `SQLitePCLRaw` (Apache-2.0). Alternativas descartadas: PostgreSQL (servidor e custo), DuckDB (foco analítico) | 01/11 |
| 2 | Ferramentas fora do projeto | **Estrito**: nada fora de MIT, nem ferramenta de apoio. Saem OWASP ZAP e Stryker.NET; a validação de segurança fica no [[Corpus-de-Ataque]] | 01/11 |
| 3 | Versão do .NET | Primeiro .NET 10 LTS; depois, ao ver que o SDK 10 não estava instalado, **.NET 9** com o SDK que já existe. Risco: o 9 sai de suporte em 10/11/2026; migração para o 10 antes da publicação ([[Decisoes-ADR]] ADR-001) | 01/11 |
| 4 | Testes abertos ao público | **Sim, com limites** ([[Executor-de-Testes]]) | 01/11 |
| 5 | Acesso a dados | **EF Core code-first com migrations**, sem DbUp e sem repositório genérico ([[Decisoes-ADR]] ADR-003) | 01/11 |
| 6 | Hospedagem | **Só no final**, quando tudo estiver certo | 01/11 |
| 7 | Visual | **Menos vibrante**, no Bancada e no portfólio. Primeiro cinza, depois "um pouco mais de cor, dev moderno": **paleta azul-ardósia com três acentos aprovada** ([[Design-do-Sistema]]); portfólio já convertido | 01/11 |
| 9 | Git | **Criado só no final**, junto com a hospedagem. Durante o desenvolvimento tudo roda local, com o script `tools/verificar` rodando os mesmos passos do CI. Antes de publicar, confirmar que o CI passa de verdade no GitHub e varrer o histórico atrás de segredo | 01/11 |
| 8 | Gráficos e mapa | Quer gráficos, mapa e detalhes gráficos como no site dos Resumos Diários. Feito no portfólio (barras, linha do tempo, rosca, mapa do Brasil, diagramas) | 01/11 |

Nota sobre a #2: Docker e a hospedagem não são MIT em sentido estrito (são infraestrutura de entrega, não dependência do código). Tratados como fora da regra, a menos que o Jonas diga o contrário.

## Ainda em aberto

| # | Pergunta | Recomendação | Trava |
|---|---|---|---|
| D | **Idiomas**: pt-BR e en no plano. Incluir es? | Ficar com os dois | Fase 8 |
| E | **Nome**: "Bancada" é provisório | Manter; trocar é barato até a Fase 1 | Fase 1 |
| F | **Quais ideias extras** entram ([[Ideias-Extras]]) | Decidir na Fase 8, com o produto já de pé | Fase 8 |
| G | **Docker e hospedagem sob a regra MIT?** | Tratar como infraestrutura, fora da regra | Fase 9 |

## Relacionadas

[[Roadmap-Fases]] · [[Decisoes-ADR]] · [[Checklists-por-Fase]]
