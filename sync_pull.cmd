@echo off
setlocal enableextensions

REM === Always execute from this script's folder ===
cd /d "%~dp0"

REM === Guardrails: git and repo ===
where git >nul 2>nul || (echo [ERRO] Git nao encontrado no PATH.& pause & exit /b 1)
git rev-parse --is-inside-work-tree >nul 2>nul || (echo [ERRO] Esta pasta nao eh um repositorio Git.& pause & exit /b 1)

REM === Current branch ===
for /f "delims=" %%b in ('git rev-parse --abbrev-ref HEAD 2^>nul') do set "BRANCH=%%b"
if not defined BRANCH (
  echo [ERRO] Nao foi possivel detectar a branch atual.
  pause
  exit /b 1
)
echo Branch atual: %BRANCH%

echo.
echo === git fetch ===
git fetch || goto :err

echo.
echo === git status ===
git status

REM === Detect changes (any) ===
for /f %%c in ('git status --porcelain ^| find /v /c ""') do set ANY=%%c
echo.
echo [INFO] Arquivos alterados (qualquer estado) = %ANY%

REM === Decide pull command ===
set "PULLCMD=git pull --rebase"

REM If no upstream, fall back to explicit remote/branch
git rev-parse --abbrev-ref --symbolic-full-name "@{u}" >nul 2>nul || (
  echo [WARN] Branch %BRANCH% sem upstream configurado. Usando origin/%BRANCH%...
  set "PULLCMD=git pull --rebase origin %BRANCH%"
)

REM If there are local changes, offer autostash
if not "%ANY%"=="0" (
  echo.
  echo [ATENCAO] Existem mudancas locais nao aplicadas.
  echo Deseja usar --autostash para guardar temporariamente? ^(S/N^)
  set "ANS="
  set /p "ANS=> "
  if /I "%ANS%"=="S" (
    set "PULLCMD=%PULLCMD% --autostash"
  ) else (
    echo.
    echo [DICA] Voce pode commitar, stashear ^(git stash^) ou responder S para autostash.
  )
)

echo.
echo === %PULLCMD% ===
%PULLCMD% || goto :err

echo.
echo [OK] Pull com rebase concluido com sucesso.
echo Pressione qualquer tecla para sair.
pause >nul
exit /b 0

:err
echo.
echo [ERRO] Pull falhou. Resolva conflitos ou revise as mensagens acima.
echo Pressione qualquer tecla para sair.
pause
exit /b 1