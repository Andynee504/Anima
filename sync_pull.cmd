@echo off
cd /d "%~dp0"
setlocal enableextensions

echo === git fetch ===
git fetch
if errorlevel 1 (
  echo.
  echo [ERRO] Falha no fetch. Verifique rede/remote.
  pause
  exit /b 1
)

echo.
echo === git status ===
git status

echo.
echo Pressione qualquer tecla para executar: git pull --rebase  (CTRL+C para abortar)
pause >nul

echo.
echo === git pull --rebase ===
git pull --rebase
if errorlevel 1 (
  echo.
  echo [ERRO] Pull com rebase falhou. Resolva conflitos e tente novamente.
  pause
  exit /b 1
)

echo.
echo [OK] Pull com rebase concluido com sucesso.
echo Pressione qualquer tecla para sair.
pause >nul
exit /b 0
