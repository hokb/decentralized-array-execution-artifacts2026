#!/usr/bin/env bash
set -euo pipefail

IMAGE_NAME="artefact1:latest"
CONTAINER_NAME="artefact1-run"
OUT_DIR="$(pwd)/result"

mkdir -p "$OUT_DIR"

echo "Building Docker image..."
docker build -t "$IMAGE_NAME" .

echo "Removing old container if it exists..."
docker rm -f "$CONTAINER_NAME" >/dev/null 2>&1 || true

echo "Running container..."
docker run --rm \
  --name "$CONTAINER_NAME" \
  -v "$OUT_DIR:/result" \
  "$IMAGE_NAME"

echo "Done. Output files, if any, are in: $OUT_DIR"