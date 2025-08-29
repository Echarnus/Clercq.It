# Use the official Node.js runtime as a parent image
FROM node:23-alpine AS base

# Install pnpm globally using corepack (recommended way)
RUN corepack enable && corepack prepare pnpm@10.12.4 --activate

# Install dependencies only when needed
FROM base AS deps
RUN apk add --no-cache libc6-compat
WORKDIR /app

# Install dependencies based on the preferred package manager
COPY ClercqIt.Web/package.json ClercqIt.Web/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile || pnpm install --force

# Rebuild the source code only when needed
FROM base AS builder
WORKDIR /app
COPY --from=deps app/node_modules ./node_modules
COPY ClercqIt.Web/. .

# Build the application
RUN pnpm run build

# Use the official Nginx image to the
FROM nginx:latest AS runner

# Install Node.js on the Nginx image
RUN apt-get update && \
    apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_23.x | bash - && \
    apt-get install -y nodejs && \
    node --version && npm --version

# Production image, copy all the files and run next
FROM runner AS final
WORKDIR /app

ENV NODE_ENV production

RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

COPY --from=builder /app/public ./public

# Set the correct permission for prerender cache
RUN mkdir .next
RUN chown nextjs:nodejs .next

# Automatically leverage output traces to reduce image size
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs

EXPOSE 80

ENV PORT 3000
ENV HOSTNAME "0.0.0.0"

CMD ["node", "server.js && nginx -g 'daemon off;'"]