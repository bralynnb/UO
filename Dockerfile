FROM node:24-bookworm-slim
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --omit=dev --ignore-scripts && npm cache clean --force
COPY server ./server
COPY Assets/TSFM/Resources/block.json ./Assets/TSFM/Resources/block.json
COPY scripts/check-build.mjs ./scripts/check-build.mjs
RUN npm run check:build && mkdir -p /app/data && chown -R node:node /app/data
ENV NODE_ENV=production PORT=8080 DATABASE_PATH=/app/data/market.sqlite
USER node
EXPOSE 8080
VOLUME ["/app/data"]
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s CMD node -e "fetch('http://127.0.0.1:'+(process.env.PORT||8080)+'/readyz').then(r=>process.exit(r.ok?0:1)).catch(()=>process.exit(1))"
CMD ["node", "server/src/index.mjs"]
