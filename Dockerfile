# Imagem do Bancada. Fase 2 troca a imagem final para incluir o executor de testes (Node e binário de testes).
# Imagens base fixadas por digest (a tag pode mudar de conteúdo; o digest não). O Dependabot propõe a atualização.
FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:1dfdb27e9c2c345cb598925ccf6b13719ecfebea8467bd4745c23a3c108cdf84 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Bancada.sln --locked-mode \
 && dotnet publish src/Bancada.Web/Bancada.Web.csproj -c Release -o /app --no-restore

# Chiseled: sem shell e sem gerenciador de pacotes; roda como usuário sem privilégio.
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled@sha256:9db8b6b5b268ebf419167693b076f66f5d998cc5771ef11f61c0e4eec376631f AS final
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
