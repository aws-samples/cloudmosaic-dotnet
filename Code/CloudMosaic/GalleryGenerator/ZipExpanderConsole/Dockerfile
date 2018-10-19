FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ZipExpanderConsole/ZipExpanderConsole.csproj ZipExpanderConsole/
RUN dotnet restore ZipExpanderConsole/ZipExpanderConsole.csproj
COPY . .
WORKDIR /src/ZipExpanderConsole
RUN dotnet build ZipExpanderConsole.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish ZipExpanderConsole.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ZipExpanderConsole.dll"]
