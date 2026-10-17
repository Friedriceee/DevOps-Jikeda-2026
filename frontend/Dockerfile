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
FROM nginxinc/nginx-unprivileged:stable-alpine@sha256:782bb89768cc87b7f95c18b95baa1af250e5d451f26794f59363540161147c01 AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
USER 101:101
EXPOSE 8080
