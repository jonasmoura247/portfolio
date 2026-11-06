# Imagem do Bancada. Fase 2 troca a imagem final para incluir o executor de testes (Node e binário de testes).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Bancada.sln --locked-mode \
 && dotnet publish src/Bancada.Web/Bancada.Web.csproj -c Release -o /app --no-restore

# Chiseled: sem shell e sem gerenciador de pacotes; roda como usuário sem privilégio.
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
ENV Bancada__Executor__Habilitado=false
USER $APP_UID
EXPOSE 8080
# A hospedagem faz o health check em GET /saude (a imagem chiseled não tem shell nem curl).
ENTRYPOINT ["dotnet", "Bancada.Web.dll"]
