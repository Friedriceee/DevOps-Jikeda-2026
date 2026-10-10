# syntax=docker/dockerfile:1

# ---- build ----------------------------------------------------------------
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY . .
# Shows the "decline" test card on the payment step. Staging only; leave false for anything customer-facing.
ARG VITE_ENABLE_TEST_PAYMENT=false
ENV VITE_ENABLE_TEST_PAYMENT=$VITE_ENABLE_TEST_PAYMENT
RUN npm run build

# ---- runtime --------------------------------------------------------------
# Unprivileged nginx: runs as a non-root user and listens on 8080.
FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
