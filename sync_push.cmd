@echo off
setlocal enableextensions

cd /d "%~dp0"

where git >nul 2>nul || (echo [ERRO] Git nao encontrado no PATH.& pause & exit /b 1)
git rev-parse --is-inside-work-tree >nul 2>nul || (echo [ERRO] Esta pasta nao eh um repositorio Git.& pause & exit /b 1)

for /f "delims=" %%b in ('git rev-parse --abbrev-ref HEAD 2^>nul') do set "BRANCH=%%b"

if /I not "%BRANCH%"=="main" (
  echo Trocando da branch %BRANCH% para main...
  git checkout main || goto :err
) else (
  echo Ja esta na branch main.
)

echo.
echo === git fetch ===
git fetch || goto :err

echo.
echo === git add -A ===
git add -A || goto :err

echo.
echo === git status ===
git status

for /f %%c in ('git diff --staged --name-only ^| find /v /c ""') do set STAGED=%%c
for /f %%c in ('git status --porcelain ^| find /v /c ""') do set ANY=%%c

echo.
echo [INFO] Arquivos STAGED = %STAGED%
echo [INFO] Arquivos alterados (qualquer estado) = %ANY%

IF "%STAGED%"=="0" IF NOT "%ANY%"=="0" (
  echo.
  echo Ha mudancas nao staged/untracked. Deseja adiciona-las agora? (S/N)
  set "ANS="
  set /p "ANS=> "
  if /I "%ANS%"=="S" (
    echo.
    echo === git add -A (again) ===
    git add -A || goto :err
    for /f %%c in ('git diff --staged --name-only ^| find /v /c ""') do set STAGED=%%c
    echo [INFO] Agora STAGED = %STAGED%
  )
)

if "%STAGED%"=="0" (
  echo.
  echo [INFO] Nao ha arquivos staged; pulando commit.
) else (
  echo.
  echo === Mensagem do commit ===
  echo (digite a mensagem e pressione ENTER; sem aspas)
  set "TMPPOW=%TEMP%\gitmsg_%RANDOM%_%RANDOM%.txt"
  powershell -NoProfile -Command "$m = Read-Host 'Mensagem'; Set-Content -LiteralPath $env:TMPPOW -Value $m -Encoding UTF8" || goto :err
  for %%A in ("%TMPPOW%") do set SIZE=%%~zA
  if "%SIZE%"=="0" (
    echo [INFO] Mensagem vazia; pulando commit.
    del "%TMPPOW%" >nul 2>nul
  ) else (
    echo.
    echo === git commit -F "%TMPPOW%" ===
    git commit -F "%TMPPOW%" || (del "%TMPPOW%" >nul 2>nul & goto :err)
    del "%TMPPOW%" >nul 2>nul
  )
)

echo.
echo === git push ===
git push && goto :ok

echo.
echo [WARN] Push falhou. Tentando configurar upstream para origin/main...
git rev-parse --abbrev-ref --symbolic-full-name "@{u}" >nul 2>nul || (
  git push --set-upstream origin main || goto :err
  goto :ok
)

goto :err

:ok
echo.
echo [OK] Push concluido na branch main.
echo Pressione qualquer tecla para sair.
pause >nul
exit /b 0

:err
echo.
echo [ERRO] Algum comando falhou. Verifique as mensagens acima.
echo Pressione qualquer tecla para sair.
pause
exit /b 1