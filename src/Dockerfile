# Build stage for .NET API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS dotnet-build
WORKDIR /src
COPY ClercqIt.Api/ClercqIt.Api.csproj ClercqIt.Api/
RUN dotnet restore ClercqIt.Api/ClercqIt.Api.csproj
COPY ClercqIt.Api/ ClercqIt.Api/
RUN dotnet build ClercqIt.Api/ClercqIt.Api.csproj -c Release -o /app/build
RUN dotnet publish ClercqIt.Api/ClercqIt.Api.csproj -c Release -o /app/publish

# Build stage for Next.js frontend  
FROM node:23-alpine AS node-base
RUN npm install -g pnpm@10.12.4

# Install dependencies for Next.js
FROM node-base AS node-deps
RUN apk add --no-cache libc6-compat
WORKDIR /app
COPY ClercqIt.Web/package.json ClercqIt.Web/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

# Build Next.js application
FROM node-base AS node-builder
WORKDIR /app
COPY --from=node-deps /app/node_modules ./node_modules
COPY ClercqIt.Web/ .
RUN pnpm run build

# Final runtime stage with nginx
FROM nginx:alpine AS final
WORKDIR /app

# Install .NET runtime for the API
RUN apk add --no-cache ca-certificates wget \
    && wget https://packages.microsoft.com/config/alpine/3.19/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    || echo "Using alternative method for .NET installation" \
    && apk add --no-cache dotnet9-runtime aspnetcore9-runtime || \
    apk add --no-cache libgcc libssl3 libstdc++ zlib icu-libs krb5-libs libintl

# Install Node.js for the frontend
RUN apk add --no-cache nodejs npm

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