@echo on
setlocal enableextensions enabledelayedexpansion

REM === Always run from script folder ===
cd /d "%~dp0"

REM === Log file ===
set "LOG=%~dp0sync_push_debug.log"
echo =============================== >> "%LOG%"
echo [%DATE% %TIME%] Start debug run >> "%LOG%"
echo Script: %~f0 >> "%LOG%"

where git >> "%LOG%" 2>&1
if errorlevel 1 (
  echo [ERRO] Git nao encontrado no PATH. >> "%LOG%"
  echo [ERRO] Git nao encontrado no PATH.
  pause
  exit /b 1
)

git rev-parse --is-inside-work-tree >> "%LOG%" 2>&1
if errorlevel 1 (
  echo [ERRO] Nao eh repo git. >> "%LOG%"
  echo [ERRO] Esta pasta nao eh um repositorio Git.
  pause
  exit /b 1
)

for /f "delims=" %%b in ('git rev-parse --abbrev-ref HEAD 2^>nul') do set "BRANCH=%%b"
echo BRANCH=!BRANCH! >> "%LOG%"
echo BRANCH=!BRANCH!
pause

if /I not "!BRANCH!"=="main" (
  echo Trocando da branch !BRANCH! para main... >> "%LOG%" & echo.
  git checkout main >> "%LOG%" 2>&1
  if errorlevel 1 goto ERR
) else (
  echo Ja esta na branch main. >> "%LOG%" & echo.
)
pause

echo === git fetch === >> "%LOG%" & echo.
git fetch >> "%LOG%" 2>&1
if errorlevel 1 goto ERR
pause

echo === git add -A === >> "%LOG%" & echo.
git add -A >> "%LOG%" 2>&1
if errorlevel 1 goto ERR
pause

echo === git status === >> "%LOG%" & echo.
git status >> "%LOG%" & echo.
pause

for /f %%c in ('git diff --staged --name-only ^| find /v /c ""') do set COUNT=%%c
echo STAGED COUNT=!COUNT! >> "%LOG%" & echo.
if "!COUNT!"=="0" goto PUSH

set "TMPMSG=%TEMP%\gitmsg_%RANDOM%_%RANDOM%.txt"
echo Digite a mensagem do commit no Notepad, salve e feche. >> "%LOG%" & echo.
notepad "!TMPMSG!"
for %%A in ("!TMPMSG!") do set SIZE=%%~zA
echo TMPMSG SIZE=!SIZE! >> "%LOG%" & echo.
if "!SIZE!"=="0" (
  echo [INFO] Mensagem vazia, pulando commit. >> "%LOG%" & echo.
) else (
  echo === git commit -F "!TMPMSG!" === >> "%LOG%" & echo.
  git commit -F "!TMPMSG!" >> "%LOG%" 2>&1
  if errorlevel 1 goto ERR
)
del "!TMPMSG!" >nul 2>&1
pause

:PUSH
echo === git push === >> "%LOG%" & echo.
git push 2>&1 >> "%LOG%" & echo.
if errorlevel 1 (
  echo Tentando configurar upstream... >> "%LOG%" & echo.
  git push --set-upstream origin main 2>&1 >> "%LOG%" & echo.
  if errorlevel 1 goto ERR
)

echo [OK] Push concluido. >> "%LOG%" & echo.
echo Log em: "%LOG%"
pause
exit /b 0

:ERR
echo [ERRO] Veja o log: "%LOG%"
type "%LOG%"
pause
exit /b 1