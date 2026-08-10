@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\prepare-workshop.ps1" %*
exit /b %ERRORLEVEL%
