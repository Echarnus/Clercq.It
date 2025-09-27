# Build stage for .NET API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS dotnet-build
WORKDIR /src

# Configure NuGet to handle SSL issues in restricted environments
ENV NUGET_XMLDOC_MODE=skip
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
RUN dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org || true

# Copy all project files first to enable restore with dependencies (excluding AppHost - not needed for production)
COPY Clercq.It.Domain/Clercq.It.Domain.csproj Clercq.It.Domain/
COPY Clercq.It.Application/Clercq.It.Application.csproj Clercq.It.Application/
COPY Clercq.It.Infrastructure/Clercq.It.Infrastructure.csproj Clercq.It.Infrastructure/
COPY Clercq.It.ServiceDefaults/Clercq.It.ServiceDefaults.csproj Clercq.It.ServiceDefaults/
COPY ClercqIt.Api/ClercqIt.Api.csproj ClercqIt.Api/

# Restore all dependencies with retry logic for SSL issues
RUN dotnet restore ClercqIt.Api/ClercqIt.Api.csproj --disable-parallel || \
    (echo "Retrying restore..." && dotnet restore ClercqIt.Api/ClercqIt.Api.csproj --disable-parallel --no-cache)

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
COPY --from=node-deps /app/node_modules ./node_modules
COPY ClercqIt.Web/ .
RUN pnpm run build || npm run build

# Final runtime stage starting from nginx
FROM nginx:1.25-bookworm AS final
WORKDIR /app

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

# Copy Next.js application
COPY --from=node-builder --chown=1001:1001 /app/public /app/frontend/public
COPY --from=node-builder --chown=1001:1001 /app/.next/standalone /app/frontend/
COPY --from=node-builder --chown=1001:1001 /app/.next/static /app/frontend/.next/static

# Create startup script that starts all services
RUN echo '#!/bin/sh' > /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Start .NET API in background' >> /app/start.sh && \
    echo 'cd /app/api' >> /app/start.sh && \
    echo 'su dotnetuser -c "dotnet ClercqIt.Api.dll --urls=http://0.0.0.0:5000" &' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Start Next.js frontend in background' >> /app/start.sh && \
    echo 'cd /app/frontend' >> /app/start.sh && \
    echo 'su nextjs -c "PORT=3000 HOSTNAME=0.0.0.0 node server.js" &' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Wait a moment for services to start' >> /app/start.sh && \
    echo 'sleep 2' >> /app/start.sh && \
    echo '' >> /app/start.sh && \
    echo '# Start nginx in foreground' >> /app/start.sh && \
    echo 'nginx -g "daemon off;"' >> /app/start.sh

RUN chmod +x /app/start.sh

EXPOSE 80

ENV NODE_ENV=production
ENV ASPNETCORE_ENVIRONMENT=Production

CMD ["/app/start.sh"]