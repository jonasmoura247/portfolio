---
tipo: qualidade
projeto: Bancada
atualizado: 2026-11-01
---

# Verificação com Playwright

**Regra do Jonas (01/11/2026):** ao terminar **cada módulo**, ele é testado no navegador com o Playwright antes de ser dado como pronto. Testes unitários e de integração provam a lógica; o Playwright prova que a tela realmente funciona para uma pessoa.

## Quando

- Fim de cada módulo de [[Modulos]] (pacientes, agenda, atendimento, prontuário, pedido, coleta, soroteca, equipamento, qualidade, resultado e laudo, auditoria, painel técnico, laboratório de ataque).
- Fim de cada fase de [[Roadmap-Fases]], uma passada geral.
- Depois de qualquer correção de defeito visual relatado.

## Procedimento

1. Subir o app localmente (`dotnet run` do projeto Web) numa porta livre; anotar a URL.
2. Abrir **uma aba nova** no navegador do Playwright (nunca fechar o navegador nem mexer nas abas do Jonas).
3. Percorrer o fluxo feliz do módulo e **cada regra de negócio** dele (RN-###): provocar o erro e conferir a mensagem.
4. Conferir por papel: o menu mostra só o permitido; a ação negada devolve 403 com a mensagem certa ([[Matriz-de-Autorizacao]]).
5. Conferir segurança básica da tela: payload do [[Corpus-de-Ataque]] no campo do módulo aparece como texto, sem script.
6. **Console sem erro** (ignorando o `favicon`) e nenhuma requisição 4xx/5xx inesperada.
7. Capturas em **claro e escuro**, desktop (1360 px) e **celular (400 px)**; conferir que a página não rola na horizontal.
8. Registrar o resultado em `Checklists-por-Fase` (módulo, data, o que foi testado, o que falhou e foi corrigido).

## Critério de "módulo pronto"

Testes unitários e de integração verdes com 100% de cobertura **e** passada do Playwright sem pendência. Defeito achado no navegador vira **teste automatizado** antes da correção, para não voltar.

## Como fica no repositório

- Roteiro de cada módulo em `tools/e2e/<modulo>.md` (passos e resultado esperado), para qualquer sessão repetir.
- Capturas em `tools/e2e/capturas/`, fora da cobertura e do build.
- O Playwright é **ferramenta de verificação local**, não dependência do projeto: não entra nos `.csproj`, no `package.json` do app nem na imagem (política MIT estrita, [[Licencas-MIT]]).

## Relacionadas

[[Estrategia-de-Testes]] · [[Checklists-por-Fase]] · [[Videos-por-Modulo]]
