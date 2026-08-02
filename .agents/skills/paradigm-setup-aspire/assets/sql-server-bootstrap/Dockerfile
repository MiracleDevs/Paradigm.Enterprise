# syntax=docker/dockerfile:1

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.302-noble
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/runtime:10.0.10-noble

FROM ${DOTNET_SDK_IMAGE} AS build
ARG BOOTSTRAP_PROJECT
ARG DATABASE_PROJECT
ARG SQLPACKAGE_VERSION=170.4.83
WORKDIR /source

COPY . .

RUN test -n "${BOOTSTRAP_PROJECT}" \
    && test -n "${DATABASE_PROJECT}" \
    && dotnet restore "${BOOTSTRAP_PROJECT}" --locked-mode \
    && dotnet publish "${BOOTSTRAP_PROJECT}" --configuration Release --no-restore --output /out/bootstrap \
    && dotnet build "${DATABASE_PROJECT}" --configuration Release --output /out/database \
    && test "$(find /out/database -maxdepth 1 -name '*.dacpac' -type f | wc -l)" -eq 1 \
    && database_directory="$(dirname "${DATABASE_PROJECT}")" \
    && test -d "${database_directory}/scripts" \
    && mkdir -p /out/database-assets/bootstrap \
    && cp -R "${database_directory}/scripts" /out/database-assets/scripts \
    && if [ -d "${database_directory}/bootstrap" ]; then cp -R "${database_directory}/bootstrap/." /out/database-assets/bootstrap/; fi \
    && dotnet tool install Microsoft.SqlPackage --tool-path /tools --version "${SQLPACKAGE_VERSION}" \
    && /tools/sqlpackage /Version | grep -F "${SQLPACKAGE_VERSION}"

FROM ${DOTNET_RUNTIME_IMAGE} AS runtime
ARG BOOTSTRAP_ASSEMBLY
USER root

RUN test -n "${BOOTSTRAP_ASSEMBLY}" \
    && apt-get update \
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

ENV BOOTSTRAP_ASSEMBLY=${BOOTSTRAP_ASSEMBLY}
ENV PATH="/opt/mssql-tools18/bin:/opt/sqlpackage:${PATH}"
WORKDIR /app

COPY --from=build /tools /opt/sqlpackage
COPY --from=build /out/bootstrap ./
COPY --from=build /out/database/*.dacpac ./database/
COPY --from=build /out/database-assets/scripts ./database/scripts
COPY --from=build /out/database-assets/bootstrap ./database/bootstrap

RUN mkdir -p /app/artifacts \
    && chown -R "${APP_UID}:${APP_UID}" /app /opt/sqlpackage

USER ${APP_UID}
ENTRYPOINT ["sh", "-c", "exec dotnet \"/app/${BOOTSTRAP_ASSEMBLY}\""]
