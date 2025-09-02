@echo off
setlocal
set BRANCH=main

REM 1) Garante estar atualizado sem perder alteracoes locais
git pull --autostash --rebase origin %BRANCH% || (
  echo ===== CONFLITOS DETECTADOS =====
  echo Resolva, depois:
  echo   git add <arquivos>  &&  git rebase --continue
  exit /b 1
)

REM 2) Pergunta mensagem de commit (popup via PowerShell; fallback console)
set "MSG="
for /f "usebackq delims=" %%a in (`powershell -NoProfile -Command "Add-Type -AssemblyName Microsoft.VisualBasic; $m=[Microsoft.VisualBasic.Interaction]::InputBox('Mensagem do commit:','Git Commit',''); if ([string]::IsNullOrWhiteSpace($m)) { '___EMPTY___' } else { $m }"`) do set "MSG=%%a"
if "%MSG%"=="___EMPTY___" (
  set /p MSG=Mensagem do commit (console): 
)
if "%MSG%"=="" set "MSG=chore: sync"

REM 3) Comita somente se houver algo staged
git add -A
git diff --cached --quiet || git commit -m "%MSG%"

REM 4) Envia
git push origin %BRANCH%
endlocal
