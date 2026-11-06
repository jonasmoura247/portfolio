# Operação

**O que é:** Tempo de resposta, fila por equipamento e ocupação da soroteca.

**Preparo:** `Bancada__CookiesSeguros=false ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Bancada.Web -c Release`

## Roteiro (serve de guião do vídeo)

1. Abra /operacao: gráficos em SVG gerados no servidor.
2. Envie uma amostra e volte: a fila muda.

## O que há por trás

SVG só com atributos, sem style: a CSP segue sem inline.

## Destaque para o vídeo

Rosca e barras.

## Critério

Sem erro no console, sem 4xx inesperado, sem rolagem horizontal em 400 px, claro e escuro.
