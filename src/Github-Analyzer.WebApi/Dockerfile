FROM mcr.microsoft.com/dotnet/sdk:10.0.300 AS build
WORKDIR /src

COPY ["Github-Analyzer.slnx", "./"]
COPY ["src/Github-Analyzer.WebApi/Github-Analyzer.WebApi.csproj", "src/Github-Analyzer.WebApi/"]
COPY ["src/Github-Analyzer.ServiceDefaults/Github-Analyzer.ServiceDefaults.csproj", "src/Github-Analyzer.ServiceDefaults/"]
COPY ["src/Github-Analyzer.Analysis/Github-Analyzer.Analysis.csproj", "src/Github-Analyzer.Analysis/"]

RUN dotnet restore "src/Github-Analyzer.WebApi/Github-Analyzer.WebApi.csproj"

COPY . .
WORKDIR /src/src/Github-Analyzer.WebApi
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false /p:DebugType=none /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.300-alpine AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Github-Analyzer.WebApi.dll"]
