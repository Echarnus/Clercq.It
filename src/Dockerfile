# Build stage for .NET API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS dotnet-build
WORKDIR /src

# Copy all project files first to enable restore with dependencies
COPY Clercq.It.Domain/Clercq.It.Domain.csproj Clercq.It.Domain/
COPY Clercq.It.Application/Clercq.It.Application.csproj Clercq.It.Application/
COPY Clercq.It.Infrastructure/Clercq.It.Infrastructure.csproj Clercq.It.Infrastructure/
COPY Clercq.It.ServiceDefaults/Clercq.It.ServiceDefaults.csproj Clercq.It.ServiceDefaults/
COPY ClercqIt.Api/ClercqIt.Api.csproj ClercqIt.Api/

# Restore all dependencies
RUN dotnet restore ClercqIt.Api/ClercqIt.Api.csproj

# Copy all source code
COPY Clercq.It.Domain/ Clercq.It.Domain/
COPY Clercq.It.Application/ Clercq.It.Application/
COPY Clercq.It.Infrastructure/ Clercq.It.Infrastructure/
COPY Clercq.It.ServiceDefaults/ Clercq.It.ServiceDefaults/
COPY ClercqIt.Api/ ClercqIt.Api/

# Build and publish
RUN dotnet build ClercqIt.Api/ClercqIt.Api.csproj -c Release -o /app/build
RUN dotnet publish ClercqIt.Api/ClercqIt.Api.csproj -c Release -o /app/publish

# Build stage for Next.js frontend  
FROM node:23-alpine AS node-base
RUN apk add --no-cache libc6-compat
RUN npm config set registry https://registry.npmjs.org/ && \
    npm config set timeout 300000 && \
    npm install -g pnpm@10.12.4

# Install dependencies for Next.js
FROM node-base AS node-deps
WORKDIR /app
COPY ClercqIt.Web/package.json ClercqIt.Web/pnpm-lock.yaml ./
RUN pnpm config set registry https://registry.npmjs.org/ && \
    pnpm install --frozen-lockfile --network-timeout 300000

# Build Next.js application
FROM node-base AS node-builder
WORKDIR /app
COPY --from=node-deps /app/node_modules ./node_modules
COPY ClercqIt.Web/ .
RUN pnpm run build

# Final runtime stage with nginx
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# Install nginx and Node.js
RUN apk add --no-cache nginx nodejs npm

# Create nginx user and directories
RUN addgroup -g 1003 -S nginx || true \
    && adduser -S nginx -u 1003 -G nginx || true \
    && mkdir -p /etc/nginx/conf.d /var/log/nginx /var/cache/nginx/client_temp \
    && chown -R nginx:nginx /var/log/nginx /var/cache/nginx 2>/dev/null || true

# Create non-root users
RUN addgroup -g 1001 -S nodejs \
    && adduser -S nextjs -u 1001 -G nodejs \
    && addgroup -g 1002 -S dotnet \
    && adduser -S dotnetuser -u 1002 -G dotnet

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy .NET API
COPY --from=dotnet-build --chown=dotnetuser:dotnet /app/publish /app/api

# Copy Next.js application
COPY --from=node-builder --chown=nextjs:nodejs /app/public /app/frontend/public
COPY --from=node-builder --chown=nextjs:nodejs /app/.next/standalone /app/frontend/
COPY --from=node-builder --chown=nextjs:nodejs /app/.next/static /app/frontend/.next/static

# Create startup script
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