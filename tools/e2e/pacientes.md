# Pacientes

**O que é:** Cadastro fictício: 60 pacientes, sem CPF, telefone ou endereço.

**Preparo:** `Bancada__CookiesSeguros=false ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Bancada.Web -c Release`

## Roteiro (serve de guião do vídeo)

1. Abra /pacientes e busque "PAC-0014".
2. Busque um payload do corpus (' OR '1'='1): volta vazio, sem erro.
3. Abra a ficha. Recepção vê só o cadastral; troque para Coleta e veja as alergias.

## O que há por trás

Consulta parametrizada e escape de curingas do LIKE. A ficha mostra a linha do tempo tirada da auditoria da sessão.

## Destaque para o vídeo

SQL injection que vira dado.

## Critério

Sem erro no console, sem 4xx inesperado, sem rolagem horizontal em 400 px, claro e escuro.
