# Build stage for .NET API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS dotnet-build
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
FROM node:23-slim AS node-base
# Configure npm for restricted environments
ENV NODE_TLS_REJECT_UNAUTHORIZED=0
RUN npm config set strict-ssl false || true
RUN npm config set registry https://registry.npmjs.org/ || true
RUN npm install -g pnpm@10.12.4 || echo "Warning: pnpm installation failed"

# Install dependencies for Next.js
FROM node-base AS node-deps
WORKDIR /app
COPY ClercqIt.Web/package.json ClercqIt.Web/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile || npm install

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

RUN pnpm run build || npm run build

# Final runtime stage starting from nginx
FROM nginx:1.29-bookworm AS final
WORKDIR /app

# Accept version build arguments again for final stage
ARG VERSION=0.0.0
ARG GIT_SHA=unknown
ARG BUILD_DATE=unknown

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy .NET runtime from Microsoft's runtime image
COPY --from=mcr.microsoft.com/dotnet/aspnet:9.0 /usr/share/dotnet /usr/share/dotnet
COPY --from=mcr.microsoft.com/dotnet/aspnet:9.0 /usr/bin/dotnet /usr/bin/dotnet

# Copy Node.js from Node.js image  
COPY --from=node:23-slim /usr/local/bin/node /usr/local/bin/node
COPY --from=node:23-slim /usr/local/lib/node_modules /usr/local/lib/node_modules

# Create symlinks for npm and npx
RUN ln -s /usr/local/lib/node_modules/npm/bin/npm-cli.js /usr/local/bin/npm \
    && ln -s /usr/local/lib/node_modules/npm/bin/npx-cli.js /usr/local/bin/npx

# Create non-root users for running services (simple approach)
RUN useradd --uid 1001 nextjs 2>/dev/null || true \
    && useradd --uid 1002 dotnetuser 2>/dev/null || true

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy .NET API
COPY --from=dotnet-build --chown=1002:1002 /app/publish /app/api

# Copy Next.js application (version.json is already included in public directory from build stage)
COPY --from=node-builder --chown=1001:1001 /app/public /app/frontend/public
COPY --from=node-builder --chown=1001:1001 /app/.next/standalone /app/frontend/
COPY --from=node-builder --chown=1001:1001 /app/.next/static /app/frontend/.next/static

# Create startup script that starts all services
RUN echo '#!/bin/sh' > /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo 'set -e' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Start .NET API in background' >> /app/start.sh && \
    echo 'echo "Starting .NET API..."' >> /app/start.sh && \
    echo 'cd /app/api' >> /app/start.sh && \
    echo 'su dotnetuser -c "/usr/share/dotnet/dotnet ClercqIt.Api.dll --urls=http://0.0.0.0:5000" > /var/log/api.log 2>&1 &' >> /app/start.sh && \
    echo 'API_PID=$!' >> /app/start.sh && \
    echo 'echo "Started .NET API with PID $API_PID"' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Start Next.js frontend in background' >> /app/start.sh && \
    echo 'echo "Starting Next.js frontend..."' >> /app/start.sh && \
    echo 'cd /app/frontend' >> /app/start.sh && \
    echo 'su nextjs -c "PORT=3000 HOSTNAME=0.0.0.0 node server.js" > /var/log/frontend.log 2>&1 &' >> /app/start.sh && \
    echo 'FRONTEND_PID=$!' >> /app/start.sh && \
    echo 'echo "Started Next.js frontend with PID $FRONTEND_PID"' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Wait for .NET API to be ready' >> /app/start.sh && \
    echo 'echo "Waiting for .NET API to be ready..."' >> /app/start.sh && \
    echo 'for i in $(seq 1 60); do' >> /app/start.sh && \
    echo '  # Check if API process is still running' >> /app/start.sh && \
    echo '  if ! kill -0 $API_PID 2>/dev/null; then' >> /app/start.sh && \
    echo '    echo "ERROR: .NET API process died. Last 50 lines of log:"' >> /app/start.sh && \
    echo '    tail -n 50 /var/log/api.log' >> /app/start.sh && \
    echo '    exit 1' >> /app/start.sh && \
    echo '  fi' >> /app/start.sh && \
    echo '  if curl -sf http://localhost:5000/health > /dev/null 2>&1; then' >> /app/start.sh && \
    echo '    echo ".NET API is ready!"' >> /app/start.sh && \
    echo '    break' >> /app/start.sh && \
    echo '  fi' >> /app/start.sh && \
    echo '  if [ $i -eq 60 ]; then' >> /app/start.sh && \
    echo '    echo "ERROR: .NET API failed to start within 60 seconds. Last 50 lines of log:"' >> /app/start.sh && \
    echo '    tail -n 50 /var/log/api.log' >> /app/start.sh && \
    echo '    exit 1' >> /app/start.sh && \
    echo '  fi' >> /app/start.sh && \
    echo '  echo "Waiting for .NET API... ($i/60)"' >> /app/start.sh && \
    echo '  sleep 1' >> /app/start.sh && \
    echo 'done' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Wait for Next.js frontend to be ready' >> /app/start.sh && \
    echo 'echo "Waiting for Next.js frontend to be ready..."' >> /app/start.sh && \
    echo 'for i in $(seq 1 60); do' >> /app/start.sh && \
    echo '  # Check if frontend process is still running' >> /app/start.sh && \
    echo '  if ! kill -0 $FRONTEND_PID 2>/dev/null; then' >> /app/start.sh && \
    echo '    echo "ERROR: Next.js frontend process died. Last 50 lines of log:"' >> /app/start.sh && \
    echo '    tail -n 50 /var/log/frontend.log' >> /app/start.sh && \
    echo '    exit 1' >> /app/start.sh && \
    echo '  fi' >> /app/start.sh && \
    echo '  if curl -sf http://localhost:3000 > /dev/null 2>&1; then' >> /app/start.sh && \
    echo '    echo "Next.js frontend is ready!"' >> /app/start.sh && \
    echo '    break' >> /app/start.sh && \
    echo '  fi' >> /app/start.sh && \
    echo '  if [ $i -eq 60 ]; then' >> /app/start.sh && \
    echo '    echo "ERROR: Next.js frontend failed to start within 60 seconds. Last 50 lines of log:"' >> /app/start.sh && \
    echo '    tail -n 50 /var/log/frontend.log' >> /app/start.sh && \
    echo '    exit 1' >> /app/start.sh && \
    echo '  fi' >> /app/start.sh && \
    echo '  echo "Waiting for Next.js frontend... ($i/60)"' >> /app/start.sh && \
    echo '  sleep 1' >> /app/start.sh && \
    echo 'done' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo 'echo "All services are ready! Starting nginx..."' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Trap signals to ensure clean shutdown of all services' >> /app/start.sh && \
    echo 'trap "echo \"Shutting down...\"; kill $API_PID $FRONTEND_PID 2>/dev/null; nginx -s quit; exit" TERM INT' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Start nginx in foreground' >> /app/start.sh && \
    echo 'exec nginx -g "daemon off;"' >> /app/start.sh

RUN chmod +x /app/start.sh

EXPOSE 80

ENV NODE_ENV=production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ROOT=/usr/share/dotnet
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Override nginx's default entrypoint to prevent auto-start
# This ensures nginx only starts when our script explicitly launches it
ENTRYPOINT []
CMD ["/app/start.sh"]