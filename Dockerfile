# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ShoppingCart.Api/ShoppingCart.Api.csproj src/ShoppingCart.Api/
COPY src/ShoppingCart.Application/ShoppingCart.Application.csproj src/ShoppingCart.Application/
COPY src/ShoppingCart.Domain/ShoppingCart.Domain.csproj src/ShoppingCart.Domain/
COPY src/ShoppingCart.Infrastructure/ShoppingCart.Infrastructure.csproj src/ShoppingCart.Infrastructure/
RUN dotnet restore src/ShoppingCart.Api/ShoppingCart.Api.csproj

COPY . .
RUN dotnet publish src/ShoppingCart.Api -c Release -o /app/publish

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ShoppingCart.Api.dll"]