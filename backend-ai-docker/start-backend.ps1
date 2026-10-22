Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  AEGISMECH-AR - BACKEND AI SERVICE (VIRTUAL ENVIRONMENT)" -ForegroundColor Yellow
Write-Host "=================================================================" -ForegroundColor Cyan

Set-Location -Path $PSScriptRoot

# 1. Kiem tra va khoi tao moi truong ao (.venv)
$venvActivate = Join-Path $PSScriptRoot ".venv\Scripts\Activate.ps1"
if (-not (Test-Path $venvActivate)) {
    Write-Host "[1/3] Chua tim thay moi truong ao (.venv). Dang khoi tao..." -ForegroundColor Yellow
    python -m venv .venv
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Khong the tao moi truong ao. Vui long kiem tra Python da duoc cai dat." -ForegroundColor Red
        exit 1
    }
    Write-Host "[INFO] Da tao thanh cong moi truong ao tai .venv" -ForegroundColor Green
} else {
    Write-Host "[1/3] Da tim thay moi truong ao (.venv)." -ForegroundColor Green
}

# 2. Kich hoat moi truong ao
Write-Host "[2/3] Kich hoat moi truong ao .venv..." -ForegroundColor Cyan
& $venvActivate

# 3. Cai dat thu vien phu thuoc vao .venv
Write-Host "[3/3] Kiem tra va cai dat dependencies vao .venv (requirements.txt)..." -ForegroundColor Cyan
python -m pip install -r requirements.txt

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  Backend dang chay tai:   http://localhost:8000" -ForegroundColor Green
Write-Host "  Tai lieu API (Swagger):  http://localhost:8000/docs" -ForegroundColor Cyan
Write-Host "  Trang thai Healthcheck:  http://localhost:8000/health" -ForegroundColor Yellow
Write-Host "  Nhan Ctrl+C de dung server." -ForegroundColor White
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

python -m uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
