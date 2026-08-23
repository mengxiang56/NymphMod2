@echo off
setlocal
cd /d "%~dp0"

set "UPLOADER=%~dp0ModUploader.exe"
set "PUBLIC_DIR=D:\spire mod\NYMPH-STS2\artifacts\workshop\public"
set "BETA_DIR=D:\spire mod\NYMPH-STS2\artifacts\workshop\public-beta"

if not exist "%UPLOADER%" (
    echo ERROR: ModUploader.exe was not found beside this script.
    goto :fatal
)

if not exist "%PUBLIC_DIR%\workshop.json" (
    echo ERROR: Public workshop files were not found:
    echo %PUBLIC_DIR%
    goto :fatal
)

if not exist "%BETA_DIR%\workshop.json" (
    echo ERROR: Public-beta workshop files were not found:
    echo %BETA_DIR%
    goto :fatal
)

echo ============================================================
echo Uploading Nymph - public
echo ============================================================
"%UPLOADER%" upload -w "%PUBLIC_DIR%"
set "PUBLIC_RESULT=%ERRORLEVEL%"

echo.
echo ============================================================
echo Uploading Nymph - public-beta
echo ============================================================
"%UPLOADER%" upload -w "%BETA_DIR%"
set "BETA_RESULT=%ERRORLEVEL%"

echo.
echo ============================================================
echo Upload results
echo ============================================================
echo Public exit code:      %PUBLIC_RESULT%
echo Public-beta exit code: %BETA_RESULT%

if not "%PUBLIC_RESULT%"=="0" echo WARNING: Public upload reported a failure.
if not "%BETA_RESULT%"=="0" echo WARNING: Public-beta upload reported a failure.
if "%PUBLIC_RESULT%"=="0" if "%BETA_RESULT%"=="0" echo Both uploads completed successfully.

echo.
pause
exit /b 0

:fatal
echo.
pause
exit /b 1
