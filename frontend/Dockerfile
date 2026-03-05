FROM node:22-alpine AS base

# --- Dependencies ---
FROM base AS deps
RUN apk add --no-cache libc6-compat
WORKDIR /app

COPY .yarnrc.yml package.json yarn.lock ./
COPY apps/next/package.json apps/next/package.json
COPY packages/tsconfig/package.json packages/tsconfig/package.json
COPY packages/trpc/package.json packages/trpc/package.json
COPY packages/domain/package.json packages/domain/package.json

RUN corepack enable && corepack prepare yarn@4.12.0 --activate
RUN yarn install --immutable

# --- Builder ---
FROM base AS builder
WORKDIR /app

RUN corepack enable && corepack prepare yarn@4.12.0 --activate

COPY --from=deps /app/ ./
COPY . .

RUN yarn build --filter=@telemetryslice/next

# --- Runner ---
FROM base AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

COPY --from=builder --chown=nextjs:nodejs /app/apps/next/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/apps/next/.next/static ./apps/next/.next/static

USER nextjs

EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

CMD ["node", "apps/next/server.js"]
