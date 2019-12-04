FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Clients/CloudMosaic.BlazorFrontend/CloudMosaic.BlazorFrontend.csproj", "Clients/CloudMosaic.BlazorFrontend/"]
COPY ["Clients/CloudMosaic.API.Client/CloudMosaic.API.Client.csproj", "Clients/CloudMosaic.API.Client/"]
COPY ["Communication/CloudMosaic.Communication.Manager/CloudMosaic.Communication.Manager.csproj", "Communication/CloudMosaic.Communication.Manager/"]

RUN dotnet restore "Clients/CloudMosaic.BlazorFrontend/CloudMosaic.BlazorFrontend.csproj"
COPY . .
WORKDIR "/src/Clients/CloudMosaic.BlazorFrontend"
RUN dotnet build "CloudMosaic.BlazorFrontend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CloudMosaic.BlazorFrontend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CloudMosaic.BlazorFrontend.dll"]