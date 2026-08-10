@echo off
call "%~dp0build.cmd" /p:GameBranch=public %*
exit /b %ERRORLEVEL%
