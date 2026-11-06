---
tipo: seguranca
projeto: Bancada
atualizado: 2026-11-01
---

# Modelo de ameaças

Referência: material Security+ SY0-701 (domínio 2, ameaças e vulnerabilidades) e OWASP Top 10.

## O que protegemos

| Ativo | Por que importa |
|---|---|
| Servidor de hospedagem | Se comprometido, vira hospedeiro de ataque com o nome do Jonas |
| Reputação | Um XSS armazenado num portfólio de segurança seria desastroso |
| Disponibilidade da demo | Um recrutador abre o link e precisa funcionar |
| Isolamento entre visitantes | Um visitante nunca vê ou altera dado de outro |
| Executor de testes do painel | É o único ponto que roda processo no servidor |

Não há dado real, então confidencialidade de dado pessoal não é o risco principal. O risco é **a aplicação ser usada contra terceiros ou ficar fora do ar**.

## Atores

Scanner automático, script kiddie, bot de spam, hacktivista, recrutador ou dev que resolve "testar o pentest do cara". Todos anônimos, todos com acesso total ao cliente.

## Fronteiras de confiança

1. Navegador ⇄ Kestrel (tudo que vem do navegador é hostil).
2. Aplicação ⇄ banco da sessão (SQL só parametrizado).
3. Aplicação ⇄ processo de testes (comando fixo, sem entrada do cliente).
4. Aplicação ⇄ sistema de arquivos (somente lista permitida, somente leitura).

## STRIDE

| Ameaça | Cenário | Mitigação | Teste |
|---|---|---|---|
| **S**poofing | Forjar cookie de sessão | Id aleatório de 256 bits, cookie `__Host-`, HttpOnly, Secure, SameSite=Lax; id novo a cada troca de papel | Cookie tem os atributos; id previsível não é aceito |
| **T**ampering | Alterar laudo liberado, adulterar log | Laudo imutável; auditoria encadeada por hash e sem UPDATE/DELETE | Tentativa de alterar falha; verificação detecta adulteração |
| **R**epudiation | "Eu não liberei esse laudo" | Auditoria com papel, ação, alvo e hora | Toda transição gera evento |
| **I**nformation disclosure | IDOR entre sessões; erro com stack trace | Banco por sessão; página de erro genérica; sem cabeçalho de servidor | Id de outra sessão dá 404; erro 500 não vaza detalhe |
| **D**enial of service | Criar milhares de sessões; spam no executor de testes | Limite de taxa por IP; teto de sessões simultâneas com descarte das mais antigas; TTL; cota por sessão; execução única | Excedeu a taxa, recebe 429; teto respeitado |
| **E**levation of privilege | Chamar action de outro papel direto | Política de autorização em **toda** action; matriz papel × action testada inteira | Teste gera todas as combinações |

## Casos de abuso (cada um vira teste de segurança)

| # | Ataque | Defesa |
|---|---|---|
| AC-1 | SQL injection em busca e filtros | Parâmetros; teste com corpus de payloads |
| AC-2 | XSS armazenado na evolução do prontuário | Escape do Razor + CSP sem inline + sem HTML aceito |
| AC-3 | XSS refletido em parâmetro de busca | Mesmo |
| AC-4 | CSRF em POST | Token antifalsificação + SameSite |
| AC-5 | Clickjacking | `frame-ancestors 'none'` |
| AC-6 | Fixação de sessão | Id novo ao trocar de papel |
| AC-7 | Corpo gigante / JSON profundo | Limites de tamanho e profundidade |
| AC-8 | Travessia de caminho no visualizador de código | Lista permitida, caminho canônico conferido |
| AC-9 | Injeção de linha no log (CRLF) | Log estruturado; entrada nunca vai crua na mensagem |
| AC-10 | Inundação do botão "rodar testes" | Uma por vez, limite por IP e global, kill switch |
| AC-11 | Parâmetro extra tentando mudar o comando do executor | O servidor ignora: comando é constante |
| AC-12 | Enumeração de pacientes de outra sessão | Banco isolado |

## Risco residual (aceito)

DDoS volumétrico contra a hospedagem não é tratável na aplicação. Aceito, documentado no registro de riscos ([[Controles-e-SecPlus]]).

## Relacionadas

[[Controles-e-SecPlus]] · [[Painel-Tecnico]] · [[Estrategia-de-Testes]]
