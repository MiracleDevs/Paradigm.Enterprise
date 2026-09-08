# syntax=docker/dockerfile:1

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.302-noble
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/runtime:10.0.10-noble

FROM ${DOTNET_SDK_IMAGE} AS build
ARG SQLPACKAGE_VERSION=170.4.83
WORKDIR /source

COPY build/Paradigm.Version.props build/Paradigm.Version.props
COPY examples/beacon-ar/Directory.Build.props examples/beacon-ar/Directory.Build.props
COPY examples/beacon-ar/Directory.Packages.props examples/beacon-ar/Directory.Packages.props
COPY examples/beacon-ar/global.json examples/beacon-ar/global.json
COPY examples/beacon-ar/src/BeaconAr.DatabaseBootstrap examples/beacon-ar/src/BeaconAr.DatabaseBootstrap
COPY examples/beacon-ar/src/database examples/beacon-ar/src/database

RUN dotnet restore examples/beacon-ar/src/BeaconAr.DatabaseBootstrap/BeaconAr.DatabaseBootstrap.csproj --locked-mode \
    && dotnet publish examples/beacon-ar/src/BeaconAr.DatabaseBootstrap/BeaconAr.DatabaseBootstrap.csproj --configuration Release --no-restore --output /out/bootstrap \
    && dotnet build examples/beacon-ar/src/database/BeaconAr.Database.sqlproj --configuration Release --output /out/database \
    && test "$(find /out/database -maxdepth 1 -name 'BeaconAr.Database.dacpac' -type f | wc -l)" -eq 1 \
    && dotnet tool install Microsoft.SqlPackage --tool-path /tools --version "${SQLPACKAGE_VERSION}" \
    && /tools/sqlpackage /Version | grep -F "${SQLPACKAGE_VERSION}"

FROM ${DOTNET_RUNTIME_IMAGE} AS runtime
USER root

RUN apt-get update \
    && apt-get install --yes --no-install-recommends ca-certificates curl \
    && curl --fail --silent --show-error --location --output /tmp/packages-microsoft-prod.deb https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb \
    && dpkg -i /tmp/packages-microsoft-prod.deb \
    && rm /tmp/packages-microsoft-prod.deb \
    && apt-get update \
    && ACCEPT_EULA=Y apt-get install --yes --no-install-recommends mssql-tools18 \
    && sqlcmd_help="$(/opt/mssql-tools18/bin/sqlcmd -? 2>&1 || true)" \
    && printf '%s\n' "${sqlcmd_help}" | grep -Eq '^Version 18\.' \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

ENV PATH="/opt/mssql-tools18/bin:/opt/sqlpackage:${PATH}"
WORKDIR /app

COPY --from=build /tools /opt/sqlpackage
COPY --from=build /out/bootstrap ./
COPY --from=build /out/database/BeaconAr.Database.dacpac ./database/BeaconAr.Database.dacpac
COPY --from=build /source/examples/beacon-ar/src/database/scripts ./database/scripts
COPY --from=build /source/examples/beacon-ar/src/database/bootstrap ./database/bootstrap

RUN mkdir -p /app/artifacts \
    && chown -R "${APP_UID}:${APP_UID}" /app /opt/sqlpackage \
    && /opt/sqlpackage/sqlpackage /Version | grep -F '170.4.83'

USER ${APP_UID}
ENTRYPOINT ["dotnet", "BeaconAr.DatabaseBootstrap.dll"]
