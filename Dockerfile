# Imagem do Bancada. Fase 2 troca a imagem final para incluir o executor de testes (Node e binário de testes).
# Imagens base fixadas por digest (a tag pode mudar de conteúdo; o digest não). O Dependabot propõe a atualização.
FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:07ed28d9e7279248fcea27334880f6c415b42c631c39b8bc5b3caf13ec6fdaa2 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Bancada.sln --locked-mode \
 && dotnet publish src/Bancada.Web/Bancada.Web.csproj -c Release -o /app --no-restore

# Chiseled: sem shell e sem gerenciador de pacotes; roda como usuário sem privilégio.
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled@sha256:06caf0e18ebd929e8fb7013b5514e097b62958335dd421b54ecf957dffa48758 AS final
WORKDIR /app
COPY --from=build /app .
# Sem canal de diagnóstico (depurador, dumps): menos superfície e funciona com sistema de arquivos só de leitura.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
ENV Bancada__Executor__Habilitado=false
USER $APP_UID
EXPOSE 8080
# A hospedagem faz o health check em GET /saude (a imagem chiseled não tem shell nem curl).
# Rodar com: --read-only --cap-drop ALL --security-opt no-new-privileges --memory 512m --pids-limit 200 (ver RELATORIO-AUDITORIA-CYBERSECURITY.md).
ENTRYPOINT ["dotnet", "Bancada.Web.dll"]
