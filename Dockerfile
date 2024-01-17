ARG DOTNET_VERSION=8.0
ARG HELM_VERSION=3.13.3

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS base
ENV VERIFY_CHECKSUM=false
RUN apk add --update --no-cache curl bash
RUN curl -fsSL -o get_helm.sh https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 && chmod 700 get_helm.sh && ./get_helm.sh --version ${HELM_VERSION}
RUN apk del curl
WORKDIR /source
COPY src/*.csproj .
RUN dotnet restore
COPY src .


FROM base as build
WORKDIR /source
RUN dotnet build -c Release --no-restore


FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build


FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS release
ENV VERIFY_CHECKSUM=false
RUN apk add --update --no-cache curl bash
RUN curl -fsSL -o get_helm.sh https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 && chmod 700 get_helm.sh && ./get_helm.sh --version ${HELM_VERSION}
RUN apk del curl
RUN addgroup -g 10001 -S redsail && adduser -u 10001 -S redsail -G redsail
USER redsail
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [ "dotnet", "KubeStatus.dll" ]
