#!/bin/sh
# Container entrypoint: starts the .NET API and Next.js, waits until both are
# healthy, then runs nginx in front of them. If any of the three stops, the
# script exits non-zero so the platform restarts the container instead of
# serving 502s from a half-dead container (issue #222).

set -e

# Start .NET API in background
echo "Starting .NET API..."
cd /app/api
/usr/share/dotnet/dotnet ClercqIt.Api.dll --urls=http://0.0.0.0:5000 > /var/log/api.log 2>&1 &
API_PID=$!
echo "Started .NET API with PID $API_PID"

# Start Next.js frontend in background
echo "Starting Next.js frontend..."
cd /app/frontend
PORT=3000 HOSTNAME=0.0.0.0 node server.js > /var/log/frontend.log 2>&1 &
FRONTEND_PID=$!
echo "Started Next.js frontend with PID $FRONTEND_PID"

# Wait for .NET API to be ready
echo "Waiting for .NET API to be ready..."
for i in $(seq 1 60); do
  # Check if API process is still running
  if ! kill -0 $API_PID 2>/dev/null; then
    echo "ERROR: .NET API process died. Last 50 lines of log:"
    tail -n 50 /var/log/api.log
    exit 1
  fi
  if curl -sf http://localhost:5000/health > /dev/null 2>&1; then
    echo ".NET API is ready!"
    break
  fi
  if [ $i -eq 60 ]; then
    echo "ERROR: .NET API failed to start within 60 seconds. Last 50 lines of log:"
    tail -n 50 /var/log/api.log
    exit 1
  fi
  echo "Waiting for .NET API... ($i/60)"
  sleep 1
done

# Wait for Next.js frontend to be ready
echo "Waiting for Next.js frontend to be ready..."
for i in $(seq 1 60); do
  # Check if frontend process is still running
  if ! kill -0 $FRONTEND_PID 2>/dev/null; then
    echo "ERROR: Next.js frontend process died. Last 50 lines of log:"
    tail -n 50 /var/log/frontend.log
    exit 1
  fi
  if curl -sf http://localhost:3000 > /dev/null 2>&1; then
    echo "Next.js frontend is ready!"
    break
  fi
  if [ $i -eq 60 ]; then
    echo "ERROR: Next.js frontend failed to start within 60 seconds. Last 50 lines of log:"
    tail -n 50 /var/log/frontend.log
    exit 1
  fi
  echo "Waiting for Next.js frontend... ($i/60)"
  sleep 1
done

echo "All services are ready! Starting nginx..."
nginx -g "daemon off;" &
NGINX_PID=$!

stop_all() {
  kill $API_PID $FRONTEND_PID $NGINX_PID 2>/dev/null || true
}

# Clean shutdown when the platform stops the container (QUIT is the nginx
# base image's stop signal; the Dockerfile sets TERM, which Scaleway sends).
trap 'echo "Shutting down..."; stop_all; exit 0' TERM INT QUIT

# Supervise: leave as soon as one service is gone. `sleep & wait` keeps the
# trap responsive (a plain sleep would delay it until the sleep ends).
set +e
while kill -0 $API_PID 2>/dev/null && kill -0 $FRONTEND_PID 2>/dev/null && kill -0 $NGINX_PID 2>/dev/null; do
  sleep 5 &
  wait $!
done

if ! kill -0 $API_PID 2>/dev/null; then
  echo "ERROR: .NET API stopped. Last 50 lines of log:"
  tail -n 50 /var/log/api.log
elif ! kill -0 $FRONTEND_PID 2>/dev/null; then
  echo "ERROR: Next.js frontend stopped. Last 50 lines of log:"
  tail -n 50 /var/log/frontend.log
else
  echo "ERROR: nginx stopped."
fi
stop_all
exit 1
