# syntax=docker/dockerfile:1
# Build context: repo root (needs seed/ and shared/test-vectors.json
# alongside the e2e/ source).
FROM mcr.microsoft.com/playwright:v1.64.1-noble
WORKDIR /repo/e2e
COPY e2e/package.json e2e/package-lock.json ./
RUN npm ci
COPY e2e/ ./
COPY seed/ /repo/seed/
CMD ["npx", "playwright", "test"]
