# Auditoria

**O que é:** Cadeia de hash de tudo que aconteceu na sessão.

**Preparo:** `Bancada__CookiesSeguros=false ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Bancada.Web -c Release`

## Roteiro (serve de guião do vídeo)

1. Como Analista, abra /auditoria e filtre por Segurança.
2. Clique em Verificar cadeia: íntegra.
3. Clique em Simular adulteração: quebra no evento certo.

## O que há por trás

A simulação age numa cópia em memória: o banco recusa alteração por gatilho. A tela diz o limite da técnica.

## Destaque para o vídeo

A cadeia acusando a adulteração.

## Critério

Sem erro no console, sem 4xx inesperado, sem rolagem horizontal em 400 px, claro e escuro.
