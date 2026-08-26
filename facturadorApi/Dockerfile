# Usar una imagen base de .NET SDK para compilar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copiar el archivo de proyecto y restaurar dependencias
COPY facturador.net.csproj ./
RUN dotnet restore

# Copiar el resto y compilar
COPY . ./
RUN rm -rf Tests
RUN dotnet publish facturador.net.csproj -c Release -o out

# Usar imagen runtime para ejecutar
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copiar la aplicación publicada
COPY --from=build-env /app/out .

# Copiar los certificados
COPY facturador-psicopedagoga.crt ./
COPY facturador-psicopedagoga.key ./

EXPOSE 80

ENTRYPOINT ["dotnet", "facturador.net.dll"]
