FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 61032
EXPOSE 44384

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["UI/CloudMosaic.Frontend/CloudMosaic.Frontend.csproj", "UI/CloudMosaic.Frontend/"]
COPY ["CloudMosaic.Common/CloudMosaic.Common.csproj", "CloudMosaic.Common/"]
RUN dotnet restore "UI/CloudMosaic.Frontend/CloudMosaic.Frontend.csproj"
COPY . .
WORKDIR "/src/UI/CloudMosaic.Frontend"
RUN dotnet build "CloudMosaic.Frontend.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "CloudMosaic.Frontend.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CloudMosaic.Frontend.dll"]