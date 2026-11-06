# Equipamento

**O que é:** Três analisadores simulados, relógio acelerado e monitor do protocolo.

**Preparo:** `Bancada__CookiesSeguros=false ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Bancada.Web -c Release`

## Roteiro (serve de guião do vídeo)

1. Como ANA-01, abra /equipamentos e envie a amostra.
2. A tela atualiza sozinha enquanto processa.
3. Mude o relógio para ×600.
4. No cenário "Equipamento com erro", a glicose falha e o Reprocessar aparece.
5. Leia o monitor: ASTM com STX, checksum e ETX.

## O que há por trás

O resultado gravado é o que foi lido de volta da mensagem ASTM ou HL7 de verdade.

## Destaque para o vídeo

Barra de progresso com o protocolo passando.

## Critério

Sem erro no console, sem 4xx inesperado, sem rolagem horizontal em 400 px, claro e escuro.
