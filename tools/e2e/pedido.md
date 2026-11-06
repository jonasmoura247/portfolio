# Pedido de exame

**O que é:** Catálogo com prévia dos tubos e alerta de alergia.

**Preparo:** `Bancada__CookiesSeguros=false ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Bancada.Web -c Release`

## Roteiro (serve de guião do vídeo)

1. Como Médico, abra /pedidos/novo?paciente=PAC-0047: alerta de látex (RN-013).
2. Marque ELE, GLI e HEM: a prévia mostra dois tubos (RN-021).
3. Envie sem exame: recusado (RN-020).

## O que há por trás

A prévia é JS sem dependência; a regra de verdade está no servidor.

## Destaque para o vídeo

Agrupamento por material.

## Critério

Sem erro no console, sem 4xx inesperado, sem rolagem horizontal em 400 px, claro e escuro.
