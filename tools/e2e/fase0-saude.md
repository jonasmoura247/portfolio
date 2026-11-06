# Roteiro E2E · Fase 0 (base)

Subir: `dotnet run --project src/Bancada.Web -c Release` com `ASPNETCORE_URLS=http://127.0.0.1:5099`.

| # | Passo | Esperado |
|---|---|---|
| 1 | Abrir `/saude` | HTTP 200, corpo `ok` |
| 2 | Ler os cabeçalhos | CSP com `default-src 'none'` e sem `unsafe-inline`; HSTS; `nosniff`; `no-referrer`; COOP/CORP; `Cache-Control: no-store` |
| 3 | Procurar `Server` e `X-Powered-By` | Ausentes (Kestrel real, não só TestServer) |
| 4 | Console do navegador | Sem erro (o `favicon` 404 é ignorado) |

Resultado de 01/11/2026: passou nos 4 passos.
