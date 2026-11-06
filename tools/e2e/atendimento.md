# Atendimento

**O que é:** Fila do dia, check-in e consulta com evolução.

**Preparo:** `Bancada__CookiesSeguros=false ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Bancada.Web -c Release`

## Roteiro (serve de guião do vídeo)

1. Como Recepção, faça o check-in do PAC-0001.
2. Como Médico (MED-01), inicie o atendimento do PAC-0014.
3. Tente concluir sem evolução: recusado.
4. Registre a evolução e conclua.
5. Troque para MED-02 e abra o atendimento: aviso de outro profissional (RN-007).

## O que há por trás

Máquina de estados com teste exaustivo (RN-008). Texto cifrado em AES-256-GCM com chave da sessão.

## Destaque para o vídeo

Só o médico do horário atende.

## Critério

Sem erro no console, sem 4xx inesperado, sem rolagem horizontal em 400 px, claro e escuro.
