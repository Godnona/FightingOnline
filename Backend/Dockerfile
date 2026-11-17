FROM node:22-alpine AS build

WORKDIR /app
COPY package.json package-lock.json ./
COPY prisma.config.ts tsconfig.json tsup.config.ts ./
COPY prisma ./prisma
RUN npm ci

COPY @types ./@types
COPY src ./src
RUN npm run build && npm prune --omit=dev --omit=peer

FROM node:22-alpine AS runtime

ENV NODE_ENV=production
WORKDIR /app

COPY --from=build /app/package.json /app/package-lock.json ./
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/build ./build

USER node
EXPOSE 3333
CMD ["npm", "start"]
