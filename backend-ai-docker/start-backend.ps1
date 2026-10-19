Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "3D MECH AR CHESS - BACKEND AI CONTAINER LAUNCHER" -ForegroundColor Yellow
Write-Host "===================================================" -ForegroundColor Cyan

Set-Location -Path $PSScriptRoot
Write-Host "Launching Docker Compose build & up..." -ForegroundColor Green
docker compose up --build -d

Start-Sleep -Seconds 4
Write-Host "Checking service health..." -ForegroundColor Magenta
try {
    $res = Invoke-RestMethod -Uri "http://localhost:8000/health" -Method Get
    Write-Host "Backend Status: " -NoNewline
    Write-Host "$($res.status) (uptime: $($res.uptime_sec))" -ForegroundColor Green
} catch {
    Write-Host "Waiting for container startup..." -ForegroundColor Yellow
}

Write-Host "Server URL: http://localhost:8000" -ForegroundColor Cyan
Write-Host "API Docs:   http://localhost:8000/docs" -ForegroundColor Cyan
