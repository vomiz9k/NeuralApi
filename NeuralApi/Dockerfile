#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app_src
COPY ["NeuralApi/NeuralApi.csproj", "NeuralApi/"]
RUN dotnet restore "NeuralApi/NeuralApi.csproj"
COPY . .
WORKDIR "/app_src/NeuralApi"
RUN dotnet build "NeuralApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NeuralApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NeuralApi.dll"]