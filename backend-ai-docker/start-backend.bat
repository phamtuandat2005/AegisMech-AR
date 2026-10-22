@echo off

echo =================================================================
echo   AEGISMECH-AR - BACKEND AI SERVICE [VIRTUAL ENVIRONMENT]
echo =================================================================

cd /d "%~dp0"

REM 1. Kiem tra va khoi tao Moi truong ao [.venv]
if not exist ".venv\Scripts\activate.bat" (
    echo [1/3] Chua tim thay moi truong ao [.venv]. Dang tao moi...
    python -m venv .venv
    if errorlevel 1 (
        echo [ERROR] Khong the tao moi truong ao. Vui long kiem tra Python 3.10+ da duoc cai dat.
        pause
        exit /b 1
    )
    echo [INFO] Da tao thanh cong moi truong ao tai .venv
) else (
    echo [1/3] Da tim thay moi truong ao [.venv].
)

REM 2. Kich hoat moi truong ao .venv
echo [2/3] Kich hoat moi truong ao .venv...
call .venv\Scripts\activate.bat
if errorlevel 1 (
    echo [ERROR] Khong the kich hoat .venv.
    pause
    exit /b 1
)

REM 3. Kiem tra va cai dat thu vien phu thuoc vao .venv
echo [3/3] Kiem tra dependencies trong requirements.txt...
python -m pip install -r requirements.txt

echo.
echo =================================================================
echo   Backend dang chay tai:   http://localhost:8000
echo   Tai lieu API [Swagger]:  http://localhost:8000/docs
echo   Trang thai Healthcheck:  http://localhost:8000/health
echo   Nhan Ctrl+C de dung server.
echo =================================================================
echo.

python -m uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload

pause
