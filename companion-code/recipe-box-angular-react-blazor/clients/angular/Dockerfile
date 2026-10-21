# syntax=docker/dockerfile:1
# Build context: ./clients/angular (this client is self-contained, per
# CLAUDE.md - no shared/ or seed/ needed at build time).

FROM node:24-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM deps AS build
COPY . .
RUN npx ng build

FROM nginxinc/nginx-unprivileged:1.29-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/recipe-box-angular/browser /usr/share/nginx/html
EXPOSE 8080
