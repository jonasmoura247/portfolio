---
tipo: seguranca
projeto: Bancada
atualizado: 2026-11-01
fonte: material-security-plus-completo.html (CompTIA SY0-701)
---

# Controles de segurança e mapa Security+

Cada controle diz **onde mora no Bancada**, **qual conceito do Security+ ele ilustra** e **qual teste prova**. A tela "Segurança" do [[Painel-Tecnico]] mostra esta tabela a partir de um JSON, e cada linha liga ao teste real.

Domínios: **D1** Conceitos gerais · **D2** Ameaças e mitigações · **D3** Arquitetura · **D4** Operações · **D5** Gestão do programa.

## Tríade CIA e AAA (D1)

| Conceito | No Bancada | Prova |
|---|---|---|
| Confidencialidade | Banco isolado por sessão; texto do prontuário cifrado em repouso; cookie HttpOnly | Id de outra sessão dá 404; coluna no banco não contém texto legível |
| Integridade | Laudo imutável; auditoria encadeada por hash; token antifalsificação | Alterar laudo falha; verificação da cadeia detecta adulteração |
| Disponibilidade | Limite de taxa, teto de sessões, TTL, cotas, health check | Excesso recebe 429; teto respeitado |
| Não repúdio | Auditoria com papel, ação, alvo, hora e hash | Toda transição gera evento |
| Autenticação | **Não existe por design** (sem credencial). Papel escolhido pelo visitante | — |
| Autorização | Política por action; matriz papel × action | Teste percorre todas as combinações |
| Accounting | Tabela `auditoria` e tela de eventos | Evento gravado por ação |
| Menor privilégio | Cada papel só enxerga o que precisa ([[Personas-e-Jornada]]) | Matriz de permissões |
| Separação de deveres | Quem executa não valida | Regra de domínio testada |
| Tipos de controle | Técnico preventivo (parâmetros, CSP), detectivo (auditoria, log), diretivo (aviso "dados fictícios"), corretivo (kill switch, TTL) | Tabela no painel |

## Ameaças e mitigações (D2)

| Ameaça | Controle | Prova |
|---|---|---|
| Injeção SQL | LINQ e `FromSql` interpolado (viram parâmetros); teste de arquitetura proíbe SQL cru montado por string | Corpus de payloads em toda entrada de busca |
| XSS | Escape do Razor, CSP sem inline, entrada sem HTML | Payloads viram texto; cabeçalho conferido |
| CSRF | `ValidateAntiForgeryToken` global + SameSite | POST sem token recebe 400 |
| Força bruta / DoS | Limite de taxa nativo do ASP.NET Core | Teste de excesso |
| Cadeia de suprimentos | Lista de licenças, `packages.lock.json`, restauração travada, `dotnet list package --vulnerable` | Teste de auditoria |
| Engenharia social | Aviso permanente de dados fictícios; nunca pedir dado real | Texto presente em toda página |
| Indicadores de comprometimento | Eventos de segurança: payload bloqueado, 429, antifalsificação inválido | Tela "Eventos" mostra cada um |

## Arquitetura (D3)

| Conceito | No Bancada |
|---|---|
| **Defesa em profundidade** | Perímetro: limite de taxa e cabeçalhos · Rede: HTTPS e HSTS · Host: container sem root, sistema de arquivos somente leitura · Aplicação: validação, autorização, CSP · Dado: cifra de campo |
| Dado em repouso | AES-256-GCM nos campos de texto livre; chave por sessão, descartada com ela |
| Dado em trânsito | TLS, HSTS |
| Dado em uso | Chave só em memória; sem log de conteúdo |
| Segmentação | Executor de testes em processo separado, ambiente limpo |
| Resiliência / RTO / RPO | Sem estado persistente; recuperar é reiniciar o container (RPO zero, RTO em segundos). Documentado no runbook |
| Soberania de dados | Nenhum dado pessoal; hospedagem escolhida em [[Decisoes-em-Aberto]] |

## Operações (D4)

| Conceito | No Bancada | Prova |
|---|---|---|
| **Hardening** | Sem cabeçalho `Server`, sem página de depuração, container não-root, só os endpoints necessários, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy` | Teste de cabeçalhos |
| IAM (ciclo de vida) | Sessão nasce, troca de papel, expira (TTL), é descartada | Teste de expiração |
| SIEM (simplificado) | Log estruturado + tela de eventos correlacionando por sessão | Evento visível na tela |
| **Resposta a incidentes** | Runbook no vault seguindo preparação → identificação → contenção → erradicação → recuperação → lições. Kill switch desliga o executor de testes e o laboratório de ataque por configuração | Teste do kill switch |
| Forense / cadeia de custódia | Cadeia de custódia **da amostra** (quem, quando, onde) e da própria auditoria | Linha do tempo da amostra |
| Gestão de vulnerabilidades | `dotnet list package --vulnerable` + corpus de ataque a cada fase → priorização (CVSS) → correção → validação, registrada no vault | Corpus verde; relatório de pacotes arquivado |
| SOAR (conceito) | Regra: 5 bloqueios do mesmo IP em 1 minuto bloqueia o IP por 10 minutos | Teste da regra |

## Gestão do programa (D5)

| Conceito | No Bancada |
|---|---|
| Hierarquia de governança | Política = regras de ouro do [[00-Indice]] · Norma = "100% de cobertura, só licença aceita" · Procedimento = runbooks · Diretriz = [[Arquitetura-MVC]] |
| Gestão de risco | Registro abaixo, com as 4 formas de tratamento |
| Risco de terceiros | Cada dependência passa pelo teste de licença e pelo `--vulnerable` |
| Compliance | Princípios da LGPD (finalidade, minimização) em [[Dados-Ficticios-e-LGPD]]; OWASP ASVS nível 1 como checklist |
| Conscientização | Aviso na entrada, "Laboratório de ataque" educativo |

### Registro de riscos

| Risco | Tratamento | Como |
|---|---|---|
| Upload malicioso | **Evitar** | Não há upload |
| Roubo de credencial | **Evitar** | Não há credencial |
| XSS / SQLi | **Mitigar** | Controles acima e testes |
| Abuso do executor de testes | **Mitigar** | Comando fixo, limite, kill switch |
| Queda da hospedagem | **Transferir** | Provedor com SLA; reimplantar é rápido |
| DDoS volumétrico | **Aceitar** | Fora do alcance da aplicação; documentado |
| Dado real digitado por visitante | **Mitigar** | Aviso, TTL, nada persistido, conteúdo nunca vai para log |

## Checklist por fase (todo item entra na definição de pronto)

- [ ] Parâmetros em todo SQL novo.
- [ ] Action nova entra na matriz de autorização.
- [ ] Entrada nova tem limite de tamanho e teste de payload.
- [ ] Evento novo entra na auditoria.
- [ ] Texto novo em pt-BR e en.
- [ ] Dependência nova passa no teste de licença.

## Relacionadas

[[Modelo-de-Ameacas]] · [[Estrategia-de-Testes]] · [[Painel-Tecnico]]
