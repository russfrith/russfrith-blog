FROM node:26-bookworm-slim AS build
WORKDIR /src
COPY package.json tsconfig.json ./
RUN npm install
COPY src ./src
RUN npm run build

FROM node:26-bookworm-slim
WORKDIR /app
COPY --from=build /src/dist ./dist
ENTRYPOINT ["node", "/app/dist/wordfreq.js"]
