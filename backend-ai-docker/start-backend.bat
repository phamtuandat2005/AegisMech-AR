@echo off
echo ===================================================
echo 3D MECH AR CHESS - BACKEND AI CONTAINER LAUNCHER
echo ===================================================
echo Building and running Docker container on port 8000...
docker compose up --build -d
echo.
echo Checking service health status...
timeout /t 5 >nul
curl -f http://localhost:8000/health
echo.
echo AI Service is running at: http://localhost:8000
echo OpenAPI Documentation: http://localhost:8000/docs
echo ===================================================
pause
