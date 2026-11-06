# Prontuário

**O que é:** Alergias, evoluções e exames anteriores do paciente.

**Preparo:** `Bancada__CookiesSeguros=false ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Bancada.Web -c Release`

## Roteiro (serve de guião do vídeo)

1. Como Médico, abra /prontuario/8.
2. Clique em Corrigir numa evolução e registre.
3. A original continua visível, marcada como corrigida (RN-011).
4. Troque para Coleta: só as alergias aparecem.

## O que há por trás

Gatilhos no banco recusam UPDATE e DELETE na evolução (RN-010).

## Destaque para o vídeo

Correção que não apaga nada.

## Critério

Sem erro no console, sem 4xx inesperado, sem rolagem horizontal em 400 px, claro e escuro.
