# --- Etapa 1: Compilar y publicar ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EcommerceApp/EcommerceApp.csproj EcommerceApp/
RUN dotnet restore EcommerceApp/EcommerceApp.csproj

COPY EcommerceApp/ EcommerceApp/
RUN dotnet publish EcommerceApp/EcommerceApp.csproj -c Release -o /app/publish --no-restore

# --- Etapa 2: Imagen final, más liviana ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render asigna el puerto en la variable de entorno PORT.
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "EcommerceApp.dll"]
