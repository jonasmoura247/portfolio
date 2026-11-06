---
tipo: arquitetura
projeto: Bancada
atualizado: 2026-11-01
---

# Sessão e isolamento

O coração do modelo de segurança: **cada visitante vive num mundo próprio**. Não há "usuário" nem login. Há uma sessão, um banco em memória e um papel.

## Ciclo de vida

```
1ª visita ──► cria SessaoDeDemonstracao ──► cookie __Host-bancada.sid
                │  banco = cópia do modelo (backup do SQLite)
                │  chave de cifra = 32 bytes aleatórios
                │  papel = Recepcao (padrão)
                ▼
          requisições (uma por vez por sessão)
                │  trocar papel ──► id da sessão renovado (anti-fixação)
                │  carregar cenário ──► reinicia o banco a partir do modelo + estado do cenário
                ▼
 expira (30 min ocioso ou 2 h no total) ou "Reiniciar" ──► descarta banco, zera a chave
```

## O que a sessão guarda

| Item | Detalhe |
|---|---|
| `Id` | 256 bits de `RandomNumberGenerator`, codificado em base64url. É o próprio valor do cookie |
| Banco | Conexão SQLite `:memory:` mantida aberta enquanto a sessão existir |
| Chave de cifra | AES-256, só em memória ([[Cifra-e-Chaves]]) |
| Papel | `Recepcao`, `Medico`, `Coleta`, `Analista`, `Admin` |
| Operador | O profissional fictício "logado" dentro do papel (`ANA-01`, `ANA-02`, `MED-01`...). Dá sentido à separação de deveres (RN-054) e aparece em toda linha da auditoria. Trocar de operador também renova o id da sessão |
| Relógio de simulação | Fator (×1, ×60, ×600), pausado ou não, horário simulado |
| Contadores | Registros criados, eventos de auditoria, tentativas bloqueadas |
| Criada/Último acesso | Para TTL |

## Limites (valores iniciais, calibrar na Fase 1)

| Limite | Valor |
|---|---|
| Tempo ocioso | 30 minutos (deslizante) |
| Tempo total | 2 horas |
| Sessões simultâneas | 200; ao passar disso, descarta a mais ociosa |
| Registros criados por sessão | 500 |
| Eventos de auditoria por sessão | 5.000 |
| Tamanho estimado do banco | 5 MB; ao passar, a sessão recebe aviso e novas criações são recusadas |

Os limites existem por segurança (disponibilidade, [[Modelo-de-Ameacas]] STRIDE-D) e ficam em configuração.

## Concorrência

A conexão SQLite não suporta uso paralelo seguro. Cada sessão tem um `SemaphoreSlim(1)`: duas requisições da mesma sessão **entram em fila**; de sessões diferentes correm em paralelo. Teste cria 50 requisições simultâneas na mesma sessão e confere que nenhuma corrompe estado.

## Criação do banco

1. Na subida, o app monta o **banco-modelo** (migrations + semente de [[Dados-de-Semente]]) uma vez.
2. Cada sessão nova recebe cópia via API de backup do SQLite, em milissegundos.
3. Os textos de evolução da semente são cifrados com a chave da sessão logo após a cópia (o modelo guarda a semente em claro, só em memória, nunca em arquivo).
4. `PRAGMA foreign_keys = ON` em toda conexão.

## Cookie

| Atributo | Valor |
|---|---|
| Nome | `__Host-bancada.sid` (o prefixo `__Host-` exige Secure, Path=/ e sem Domain) |
| `HttpOnly` | sim |
| `Secure` | sim |
| `SameSite` | Lax |
| Validade | de sessão (sem `Expires`) |

Cookie com valor que não existe no repositório de sessões é tratado como visita nova (cria sessão e substitui). Nunca revela se o id "quase existia".

## Papel

Trocar de papel é uma ação de demonstração, aberta a todos. Ela **renova o id** e grava evento de auditoria. A autorização vem do papel na sessão do servidor, nunca de campo enviado pelo cliente.

## Reinício e cenários

"Reiniciar sessão" descarta tudo e começa de novo. "Carregar cenário" ([[Personas-e-Jornada]]) reinicia o banco e aplica um estado pronto (ex.: valor crítico).

## Testes obrigatórios

- Sessão A não lê, não altera e não enumera nada da sessão B (teste com dois clientes).
- Cookie sem os atributos exigidos falha o teste de cabeçalhos.
- TTL ocioso e total expiram com `IRelogio` manual.
- Teto de 200 sessões descarta a mais ociosa.
- Trocar papel renova o id e o id antigo deixa de valer.
- Estouro de cota recusa criação e registra evento.

## Relacionadas

[[Modelo-de-Dados]] · [[Cabecalhos-e-Configuracao-Segura]] · [[Cifra-e-Chaves]]
