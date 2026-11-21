# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Спочатку лише файли проєктів: restore кешується, поки не змінилися залежності
COPY Directory.Build.props Splitwise.slnx ./
COPY src/Splitwise.Core/Splitwise.Core.csproj src/Splitwise.Core/
COPY src/Splitwise.Infrastructure/Splitwise.Infrastructure.csproj src/Splitwise.Infrastructure/
COPY src/Splitwise.Api/Splitwise.Api.csproj src/Splitwise.Api/
COPY tests/Splitwise.Tests/Splitwise.Tests.csproj tests/Splitwise.Tests/
RUN dotnet restore Splitwise.slnx

COPY . .
RUN dotnet publish src/Splitwise.Api/Splitwise.Api.csproj -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
# Порт за замовчуванням 8080; хостинг (Render) може передати свій через змінну PORT
EXPOSE 8080

ENTRYPOINT ["dotnet", "Splitwise.Api.dll"]
