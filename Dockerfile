ARG DOTNET_VERSION=5.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS base
WORKDIR /source
COPY src/*.csproj .
RUN dotnet restore
COPY src .


FROM base as build
WORKDIR /source
RUN dotnet build -c Release --no-restore


FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build


FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS release
WORKDIR /app
COPY --from=publish /app/publish ./
ENTRYPOINT [ "dotnet", "KubeStatus.dll" ]
