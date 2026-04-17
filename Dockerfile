# Use the official ASP.NET Core runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebMkt.csproj", "."]
RUN dotnet restore "./WebMkt.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "WebMkt.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebMkt.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebMkt.dll"]