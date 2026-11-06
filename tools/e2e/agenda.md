# Agenda

**O que é:** Grade de 30 minutos por médico, em duas semanas.

**Preparo:** `Bancada__CookiesSeguros=false ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Bancada.Web -c Release`

## Roteiro (serve de guião do vídeo)

1. Como Recepção, abra /agenda e clique num horário livre.
2. Agende PAC-0002 e confirme.
3. Reagende e cancele: o horário volta a ficar livre (RN-004).
4. Troque para Médico: a agenda fica só de consulta.

## O que há por trás

RN-001 tem índice único filtrado no banco. RN-005: horário vencido vira falta ao abrir a agenda.

## Destaque para o vídeo

Dois avisos: horário ocupado e horário no passado.

## Critério

Sem erro no console, sem 4xx inesperado, sem rolagem horizontal em 400 px, claro e escuro.
