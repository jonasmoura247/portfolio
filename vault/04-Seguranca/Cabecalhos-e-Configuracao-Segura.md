---
tipo: seguranca
projeto: Bancada
atualizado: 2026-11-01
---

# Cabeçalhos e configuração segura

Valores de partida, todos em configuração e todos conferidos por teste de integração.

## Cabeçalhos de resposta (todas as rotas)

| Cabeçalho | Valor |
|---|---|
| `Content-Security-Policy` | `default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'; object-src 'none'` |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` |
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `no-referrer` |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=(), usb=()` |
| `Cross-Origin-Opener-Policy` | `same-origin` |
| `Cross-Origin-Resource-Policy` | `same-origin` |
| `Cache-Control` (páginas) | `no-store` |
| `Server` e `X-Powered-By` | **removidos** |

`data:` em `img-src` só para o SVG da etiqueta e ícones. Sem `unsafe-inline`, sem `unsafe-eval`, sem nonce (nada inline, [[Javascript-Modulos]]).

## Limites de taxa (ASP.NET Core, middleware nativo)

| Política | Limite (por IP) | Resposta |
|---|---|---|
| Geral (GET) | 120 por minuto | 429 + `Retry-After` |
| Escrita (POST/PUT/DELETE) | 30 por minuto | 429 |
| Criação de sessão | 10 por minuto | 429 |
| Laboratório de ataque | 60 por minuto | 429 |
| Executor de testes | 1 a cada 5 minutos; **1 simultâneo no total**; 10 por hora no total | 429 / 409 |

Quando o mesmo IP gera **5 eventos de bloqueio em 1 minuto**, é bloqueado por 10 minutos (regra de resposta automática, estilo SOAR). IP vem do cabeçalho de proxy **somente** se o proxy da hospedagem estiver na lista confiável configurada; caso contrário vale o IP da conexão (senão o cliente forja o IP).

## Tamanhos

| Item | Limite |
|---|---|
| Corpo de formulário | 16 KB |
| JSON de entrada | 16 KB, profundidade 8 |
| Texto de busca | 64 caracteres |
| Texto de evolução | 2.000 caracteres |
| Motivo de rejeição/descarte | 200 caracteres |
| Payload do laboratório de ataque | 500 caracteres |
| Cabeçalhos da requisição | 16 KB total |
| Tempo máximo de requisição | 15 s |

## Antifalsificação

Token por sessão, enviado em campo oculto (formulário) ou cabeçalho `X-CSRF` (JavaScript). Cookie próprio `__Host-bancada.af`, `SameSite=Strict`. Validação global (filtro automático para verbos de escrita), nunca por action. Token inválido: 400 e evento de auditoria.

## Cookies e HTTPS

- `__Host-bancada.sid` (sessão) e `__Host-bancada.af` (antifalsificação) exigem HTTPS.
- Chave `Bancada:CookiesSeguros`, **padrão `true`**. Com `false` (só para rodar na máquina local sem certificado) o cookie de antifalsificação perde o prefixo e o `Secure`. Em produção atrás de proxy que termina o TLS, é preciso configurar cabeçalhos encaminhados para a aplicação enxergar HTTPS e o IP real do cliente **só de proxy confiável** (tarefa da Fase 9, junto da hospedagem).
- Sem HTTPS e com o padrão, a aplicação **falha fechado** (500 genérico), nunca grava cookie inseguro. Há teste para os dois modos.
- Health check (`/saude`) e páginas de erro não criam sessão e ficam fora do limite de taxa; rota que não existe (404) também não cria sessão.

## Erros

Página de erro genérica para 4xx/5xx, sem stack trace, sem nome de classe, sem caminho. `ProblemDetails` na API com `traceId` curto para correlacionar com o log.

## Configuração segura por padrão

| Item | Regra |
|---|---|
| Ambiente de produção | `ASPNETCORE_ENVIRONMENT=Production`; página de exceção do desenvolvedor desligada |
| Endpoints | Só os de [[Rotas-e-Controllers]]; sem Swagger em produção |
| Arquivos estáticos | Só `wwwroot`; sem listagem de diretório |
| HTTPS | Redireciona; HSTS ligado |
| Segredos | Nenhum no repositório |
| Dependências | Versões travadas, `--vulnerable` no CI |

## Testes

`CabecalhosTestes`: um teste por cabeçalho, em página, API, erro e arquivo estático. `LimitesTestes`: estourar cada limite devolve 429/413/400 e grava o evento. `ConfiguracaoTestes`: cada chave tem o padrão seguro; produção não expõe detalhe de erro.

## Relacionadas

[[Modelo-de-Ameacas]] · [[Sessao-e-Isolamento]] · [[Controles-e-SecPlus]]
