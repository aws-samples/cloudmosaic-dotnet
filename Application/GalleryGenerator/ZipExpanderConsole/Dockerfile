FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY GalleryGenerator/ZipExpanderConsole/ZipExpanderConsole.csproj GalleryGenerator/ZipExpanderConsole/
RUN dotnet restore GalleryGenerator/ZipExpanderConsole/ZipExpanderConsole.csproj
COPY . .
WORKDIR /src/GalleryGenerator/ZipExpanderConsole
RUN dotnet build ZipExpanderConsole.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish ZipExpanderConsole.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ZipExpanderConsole.dll"]
