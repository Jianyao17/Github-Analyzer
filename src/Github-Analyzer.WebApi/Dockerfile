FROM mcr.microsoft.com/dotnet/sdk:10.0.203 AS build
WORKDIR /src

COPY ["global.json", "./"]
COPY ["Github-Analyzer.slnx", "./"]
COPY ["src/Github-Analyzer.WebApi/Github-Analyzer.WebApi.csproj", "src/Github-Analyzer.WebApi/"]
COPY ["src/Github-Analyzer.ServiceDefaults/Github-Analyzer.ServiceDefaults.csproj", "src/Github-Analyzer.ServiceDefaults/"]
COPY ["src/Github-Analyzer.Analysis/Github-Analyzer.Analysis.csproj", "src/Github-Analyzer.Analysis/"]

RUN dotnet restore "src/Github-Analyzer.WebApi/Github-Analyzer.WebApi.csproj"

RUN apt-get update && apt-get install -y git

COPY . .
WORKDIR /src/src/Github-Analyzer.WebApi

# Extract commit hash dynamically, handling multiline C# arrays and comments using Perl and xargs
# Note: The root path in this build stage is /src/src/Github-Analyzer.WebApi
RUN apt-get update && apt-get install -y git perl

RUN perl -0777 -ne 's|/\*.*?\*/||gs; s|//.*||g; if (/CodeGraphAnalyzerPaths\s*[^=]*=\s*\[(.*?)\]/s) { my $c = $1; while ($c =~ /"([^"]+)"/g) { print "\"$1\" "; } }' Config/AnalysisConfig.cs | \
    xargs -r git log -1 --format="%H" -- > codegraph_version.txt || true ; \
    if [ ! -s codegraph_version.txt ]; then echo "dev" > codegraph_version.txt; fi

RUN perl -0777 -ne 's|/\*.*?\*/||gs; s|//.*||g; if (/StatisticAnalyzerPaths\s*[^=]*=\s*\[(.*?)\]/s) { my $c = $1; while ($c =~ /"([^"]+)"/g) { print "\"$1\" "; } }' Config/AnalysisConfig.cs | \
    xargs -r git log -1 --format="%H" -- > statistic_version.txt || true ; \
    if [ ! -s statistic_version.txt ]; then echo "dev" > statistic_version.txt; fi

RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false /p:DebugType=none /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine  AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV TMPDIR=/var/tmp
EXPOSE 8080

COPY --from=build /app/publish .
# Use the pre-configured docker appsettings (sensitive fields omitted, provided via ENV)
COPY --from=build /src/src/Github-Analyzer.WebApi/appsettings.docker.json ./appsettings.json
COPY --from=build /src/src/Github-Analyzer.WebApi/*_version.txt ./

ENTRYPOINT ["dotnet", "Github-Analyzer.WebApi.dll"]
