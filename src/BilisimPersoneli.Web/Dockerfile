FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BilisimPersoneli.sln ./
COPY src/BilisimPersoneli.Domain/BilisimPersoneli.Domain.csproj src/BilisimPersoneli.Domain/
COPY src/BilisimPersoneli.Application/BilisimPersoneli.Application.csproj src/BilisimPersoneli.Application/
COPY src/BilisimPersoneli.Infrastructure/BilisimPersoneli.Infrastructure.csproj src/BilisimPersoneli.Infrastructure/
COPY src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj src/BilisimPersoneli.Web/

RUN dotnet restore src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj

COPY src/ src/
RUN dotnet publish src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BilisimPersoneli.Web.dll"]
