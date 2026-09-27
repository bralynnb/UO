FROM python:3.12-slim-bookworm
RUN apt-get update && apt-get install -y --no-install-recommends git curl ca-certificates build-essential bison flex libpng-dev && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY . .
RUN pip install --no-cache-dir -r requirements.txt && sh tools/bootstrap.sh
ENV PORT=8000 SCUMMC_ROOT=/app/.toolchain/scummc
RUN useradd -m game && chown -R game:game /app
USER game
EXPOSE 8000
CMD ["python", "serve.py"]
