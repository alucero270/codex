@echo off
setlocal

set "RepoRoot=%~dp0"
set "ValidationScript=%RepoRoot%ops\validate-platform-readiness.ps1"

if not exist "%ValidationScript%" (
  echo Expected validation script at "%ValidationScript%".
  exit /b 1
)

echo Running Strata baseline validation bootstrap...
powershell -ExecutionPolicy Bypass -File "%ValidationScript%"
exit /b %ERRORLEVEL%
