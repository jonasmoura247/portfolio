# Roteiro E2E · Fase 1 (núcleo seguro)

Subir (modo local, sem HTTPS): `Bancada__CookiesSeguros=false ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Bancada.Web -c Release`.
Em produção o padrão é `true` (cookie `__Host-` e antifalsificação exigem HTTPS).

| # | Passo | Esperado |
|---|---|---|
| 1 | Abrir `/` | 200; título "Um laboratório de mentira, feito de verdade"; menu de Recepção (Início, Pacientes, Agenda, Atendimento, Pedido de exame, Operação, Painel técnico, Laboratório de ataque), tudo menos Início "em breve" |
| 2 | Cookie da sessão | `__Host-bancada.sid`: HttpOnly, Secure, SameSite=Lax, Path=/, sem Expires |
| 3 | Trocar Papel para Analista no seletor (envio automático por JS) | Página recarrega; operador ANA-01; menu do Analista (Soroteca, Equipamento, Qualidade, Resultados, Auditoria...); **id da sessão muda** |
| 4 | Trocar Operador para ANA-02 | Operador atualizado; id muda de novo |
| 5 | Botão Tema | `data-theme` alterna claro/escuro |
| 6 | Reiniciar sessão | Volta a Recepção com id novo |
| 7 | `/nao-existe` | 404 genérico "Página não encontrada", sem criar sessão |
| 8 | `/erro/500` | 500 genérico, sem stack trace |
| 9 | Fontes | Zero `<script>` inline, zero `onclick`/`onchange`, zero `style=` no HTML do servidor |
| 10 | Celular (400 px) | Sem rolagem horizontal da página; menu vira faixa rolável |
| 11 | Console | Sem erro (404/500 provocados nos passos 7 e 8 são esperados) |

Resultado de 01/11/2026: passou nos 11 passos. Defeito achado no navegador e corrigido: no modo local (HTTP) o cookie de antifalsificação com prefixo `__Host-` era descartado pelo navegador, porque sem Secure o prefixo é inválido; agora o modo local usa o nome sem prefixo (teste automatizado cobre os dois modos).
