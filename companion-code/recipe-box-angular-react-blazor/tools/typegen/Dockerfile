# syntax=docker/dockerfile:1
FROM node:24-alpine
WORKDIR /app
COPY tools/typegen/package.json tools/typegen/package-lock.json ./
RUN npm ci
COPY tools/typegen/ ./
ENTRYPOINT ["sh", "generate.sh"]
