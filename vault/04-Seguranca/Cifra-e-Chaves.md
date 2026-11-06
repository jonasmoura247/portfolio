---
tipo: seguranca
projeto: Bancada
atualizado: 2026-11-01
---

# Cifra e chaves

## O que é cifrado

Os campos de **texto livre** do prontuário (evolução, alergia descritiva) e os motivos de rejeição/descarte. Identificadores, estados e datas ficam em claro: precisam ser consultados e não revelam conteúdo.

O objetivo é demonstrar **dado em repouso protegido** ([[Controles-e-SecPlus]], domínio 3), não proteger dado real (não existe).

## Algoritmo

AES-256-GCM da biblioteca da própria plataforma (`System.Security.Cryptography.AesGcm`, MIT como todo o .NET). Nenhum pacote de terceiros.

| Parâmetro | Valor |
|---|---|
| Chave | 32 bytes de `RandomNumberGenerator`, **uma por sessão** |
| Nonce | 12 bytes aleatórios, **novo a cada cifra** |
| Tag | 16 bytes |
| Dados autenticados (AAD) | `tabela:campo:id` do registro |
| Formato guardado | `base64( versao(1) ‖ nonce(12) ‖ tag(16) ‖ texto cifrado )` |

A AAD amarra o texto ao seu registro: copiar um valor cifrado de uma linha para outra faz a decifragem falhar.

## Ciclo da chave

1. Nasce com a sessão ([[Sessao-e-Isolamento]]).
2. Vive só em memória, dentro do objeto da sessão; nunca em disco, log, cookie ou resposta.
3. Morre com a sessão: ao descartar, `CryptographicOperations.ZeroMemory` zera o buffer.
4. Não há rotação (a vida máxima da chave é 2 horas). A coluna de versão prepara a rotação, mas não é implementada (YAGNI).

## Porta e testes

`ICifra` com `Cifrar(texto, aad)` e `Decifrar(valor, aad)`.

| Teste | Garante |
|---|---|
| Ida e volta com textos de 0, 1, 2.000 caracteres e Unicode | Funciona nos limites |
| Dois cifrados do mesmo texto são diferentes | Nonce não repete |
| Mudar um byte do valor guardado | Decifrar falha (integridade) |
| AAD errada | Decifrar falha |
| Chave de outra sessão | Decifrar falha |
| Coluna no banco não contém o texto original | Está mesmo cifrado |
| Chave zerada após descartar a sessão | Buffer todo zero |
| Versão desconhecida | Rejeita com erro de domínio |

## Cifra de semente

Os textos da semente nascem em claro no banco-modelo (só em memória). Logo após a cópia para a sessão, `CifrarSemente` cifra todos com a chave da sessão ([[Sessao-e-Isolamento]]). Teste confere que, depois disso, nenhuma linha de `evolucao` guarda texto legível.

## Fora de escopo

TLS é responsabilidade da hospedagem (com HSTS no app). Não há cifra de disco porque não há disco: tudo é memória.

## Relacionadas

[[Modelo-de-Dados]] · [[Dados-Ficticios-e-LGPD]]
