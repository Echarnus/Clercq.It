# Build stage for .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
WORKDIR /src

# Accept version build arguments
ARG VERSION=0.0.0
ARG GIT_SHA=unknown
ARG BUILD_DATE=unknown

# Configure environment for production builds
ENV NUGET_XMLDOC_MODE=skip
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Copy nuget config first
COPY nuget.config .
COPY Directory.Packages.props .

# Copy project files and restore dependencies
COPY Clercq.It.Domain/Clercq.It.Domain.csproj Clercq.It.Domain/
COPY Clercq.It.Application/Clercq.It.Application.csproj Clercq.It.Application/
COPY Clercq.It.Infrastructure/Clercq.It.Infrastructure.csproj Clercq.It.Infrastructure/
COPY Clercq.It.ServiceDefaults/Clercq.It.ServiceDefaults.csproj Clercq.It.ServiceDefaults/
COPY ClercqIt.Api/ClercqIt.Api.csproj ClercqIt.Api/

# Restore dependencies with SSL configuration
RUN dotnet restore ClercqIt.Api/ClercqIt.Api.csproj --disable-parallel --verbosity minimal

# Copy all source code (excluding AppHost - not needed for production)
COPY Clercq.It.Domain/ Clercq.It.Domain/
COPY Clercq.It.Application/ Clercq.It.Application/
COPY Clercq.It.Infrastructure/ Clercq.It.Infrastructure/
COPY Clercq.It.ServiceDefaults/ Clercq.It.ServiceDefaults/
COPY ClercqIt.Api/ ClercqIt.Api/

# Build and publish
RUN dotnet build ClercqIt.Api/ClercqIt.Api.csproj -c Release -o /app/build
RUN dotnet publish ClercqIt.Api/ClercqIt.Api.csproj -c Release -o /app/publish

# Build stage for Next.js frontend  
FROM node:24-slim AS node-base
RUN npm install -g pnpm@10.12.4

# Install dependencies for Next.js
FROM node-base AS node-deps
WORKDIR /app
COPY ClercqIt.Web/package.json ClercqIt.Web/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

# Build Next.js application
FROM node-base AS node-builder
WORKDIR /app

# Accept version build arguments for creating version.json
ARG VERSION=0.0.0
ARG GIT_SHA=unknown
ARG BUILD_DATE=unknown

COPY --from=node-deps /app/node_modules ./node_modules
COPY ClercqIt.Web/ .

# Create version.json file before building Next.js app
# This ensures the file is included in the build output
RUN echo "{\"version\":\"${VERSION}\",\"gitSha\":\"${GIT_SHA}\",\"buildDate\":\"${BUILD_DATE}\"}" > /app/public/version.json

RUN pnpm run build

# Final runtime stage starting from nginx
FROM nginx:1.30-trixie AS final
WORKDIR /app

# Accept version build arguments again for final stage
ARG VERSION=0.0.0
ARG GIT_SHA=unknown
ARG BUILD_DATE=unknown

# Take the Debian security updates released since the base image was built
# (Trivy blocks on fixable HIGH/CRITICAL findings, issue #281), and install
# curl for health checks and ICU for .NET globalization. The .NET API fails
# fast at startup without ICU; bookworm happened to ship it, trixie does not (#383).
RUN apt-get update && apt-get upgrade -y && apt-get install -y curl libicu76 && rm -rf /var/lib/apt/lists/*

# Copy .NET runtime from Microsoft's runtime image
COPY --from=mcr.microsoft.com/dotnet/aspnet:10.0 /usr/share/dotnet /usr/share/dotnet
COPY --from=mcr.microsoft.com/dotnet/aspnet:10.0 /usr/bin/dotnet /usr/bin/dotnet

# Copy the Node.js binary only: start.sh runs `node server.js`, and leaving npm
# out of the runtime image drops its dependencies from the attack surface.
COPY --from=node:24-slim /usr/local/bin/node /usr/local/bin/node

# Everything runs as one unprivileged user (issue #280). nginx keeps listening
# on port 80 through the CAP_NET_BIND_SERVICE file capability, so the
# container port in Terraform stays the same. nginx writes its pid and temp
# files to paths that user owns; the service logs are pre-created for it.
RUN apt-get update && apt-get install -y --no-install-recommends libcap2-bin \
    && setcap cap_net_bind_service=+ep /usr/sbin/nginx \
    && apt-get purge -y libcap2-bin && apt-get autoremove -y && rm -rf /var/lib/apt/lists/* \
    && useradd --uid 10001 --user-group --no-create-home --shell /usr/sbin/nologin clercqit \
    && sed -i -e '/^user /d' -e 's#^pid .*#pid /tmp/nginx.pid;#' /etc/nginx/nginx.conf \
    && chown -R clercqit:clercqit /var/cache/nginx \
    && touch /var/log/api.log /var/log/frontend.log \
    && chown clercqit:clercqit /var/log/api.log /var/log/frontend.log

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy .NET API
COPY --from=dotnet-build --chown=10001:10001 /app/publish /app/api

# Copy Next.js application (version.json is already included in public directory from build stage)
COPY --from=node-builder --chown=10001:10001 /app/public /app/frontend/public
COPY --from=node-builder --chown=10001:10001 /app/.next/standalone /app/frontend/
COPY --from=node-builder --chown=10001:10001 /app/.next/static /app/frontend/.next/static

# Startup script: starts the API and Next.js, then nginx, and exits when any of
# them stops so the container is restarted (src/start.sh, issue #222).
COPY --chmod=755 start.sh /app/start.sh

# For docker run and local orchestration; Scaleway uses its own probe on the port.
HEALTHCHECK --interval=30s --timeout=5s --start-period=90s --retries=3 \
    CMD curl -fsS http://localhost:5000/health > /dev/null && curl -fsS -o /dev/null http://localhost/ || exit 1

EXPOSE 80
# The nginx base image stops with SIGQUIT; start.sh is PID 1 and handles TERM.
STOPSIGNAL SIGTERM

ENV NODE_ENV=production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ROOT=/usr/share/dotnet
ENV DOTNET_RUNNING_IN_CONTAINER=true

USER clercqit

# Override nginx's default entrypoint to prevent auto-start
# This ensures nginx only starts when our script explicitly launches it
ENTRYPOINT []
CMD ["/app/start.sh"]
