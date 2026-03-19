#!/bin/bash

API_DIR="$(dirname "$0")/QuizAI.Api"
DESKTOP_DIR="$(dirname "$0")/QuizAI.Desktop"

echo "[1/2] Starting API on http://localhost:5127 ..."
cd "$API_DIR" && dotnet run --launch-profile http &
API_PID=$!

echo "Waiting for API to start..."
for i in {1..20}; do
    if curl -s http://localhost:5127/swagger/index.html > /dev/null 2>&1; then
        echo "API is up!"
        break
    fi
    sleep 1
done

echo "[2/2] Starting Desktop (pointing to localhost)..."
cd "$DESKTOP_DIR" && QUIZAI_API_URL=http://localhost:5127/api/ dotnet run &
DESKTOP_PID=$!

echo ""
echo "Running:"
echo "  API     -> http://localhost:5127  (PID $API_PID)"
echo "  Desktop -> local app              (PID $DESKTOP_PID)"
echo ""
echo "Press Ctrl+C to stop both."

trap "kill $API_PID $DESKTOP_PID 2>/dev/null; echo 'Stopped.'" INT TERM
wait
