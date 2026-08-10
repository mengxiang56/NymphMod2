@echo off
call "%~dp0build.cmd" /p:GameBranch=public-beta %*
exit /b %ERRORLEVEL%
