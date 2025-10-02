#!/bin/sh
# Wait for Redpanda to be ready before starting Ingest.Sim
set -e
host="${KAFKA%%:*}"
port="${KAFKA##*:}"
echo "Waiting for Kafka broker $host:$port to be available..."
until nc -z "$host" "$port"; do
  echo "Kafka not available yet... sleeping"
  sleep 2
done
echo "Kafka is up! Starting Ingest.Sim..."
exec dotnet Ingest.Sim.dll
