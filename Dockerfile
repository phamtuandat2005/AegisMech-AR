# -------------------------------------------------------------
# DOCKERFILE: 3D MECH AR CHESS - BACKEND AI SERVICE (ROOT)
# Multi-stage optimized Python 3.10 slim image with PyTorch CPU
# -------------------------------------------------------------
FROM python:3.10-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    DEBIAN_FRONTEND=noninteractive

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

# Copy and install python dependencies
COPY backend-ai-docker/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY backend-ai-docker/app/ /app/app/

EXPOSE 8000

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:${PORT:-8000}/health || exit 1

# Start Uvicorn ASGI server with dynamic Render $PORT support
CMD ["sh", "-c", "uvicorn app.main:app --host 0.0.0.0 --port ${PORT:-8000}"]
