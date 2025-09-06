@echo off
setlocal

REM ===== Garante que estamos na pasta do .bat (evita abrir em System32) =====
pushd "%~dp0"

REM ===== Checa se o Git existe no PATH =====
git --version >NUL 2>&1
if errorlevel 1 (
  echo Git nao encontrado no PATH. Instale o Git ou abra este .bat em um terminal com Git.
  echo.
  pause
  exit /b 1
)

REM ===== Confere se esta pasta e um repositorio Git =====
git rev-parse --is-inside-work-tree >NUL 2>&1
if errorlevel 1 (
  echo Esta pasta nao e um repositorio Git:
  echo   %CD%
  echo.
  echo Dica 1: mova este .bat para a RAIZ do seu repo e rode novamente.
  echo Dica 2: ou edite este arquivo e fixe o caminho do repo assim no topo:
  echo   rem set "REPO=C:\caminho\do\repo"  &&  pushd "%%REPO%%"
  echo.
  pause
  exit /b 1
)

REM ===== Descobre a branch atual =====
for /f "delims=" %%b in ('git rev-parse --abbrev-ref HEAD') do set "BRANCH=%%b"
echo Branch atual: %BRANCH%
echo.

REM ===== Atualiza (pull) sem perder alteracoes locais =====
git pull --rebase --autostash origin %BRANCH%
if errorlevel 1 (
  echo ===== CONFLITOS DETECTADOS =====
  echo Resolva os arquivos com ^<<^<<< e ^>>>^>>> e depois rode:
  echo   git add ^<arquivos^>  &&  git rebase --continue
  echo.
  pause
  exit /b 1
)

REM ===== Mensagem de commit no CMD =====
set "MSG="
set /p MSG=Mensagem do commit (ENTER = "chore: sync"): 
if "%MSG%"=="" set "MSG=chore: sync"

REM ===== Stage e commit apenas se houver algo para commitar =====
git add -A
git diff --cached --quiet
if errorlevel 1 (
  git commit -m "%MSG%"
  if errorlevel 1 (
    echo ERRO no commit. Verifique assinatura/perm.
    echo.
    pause
    exit /b 1
  )
) else (
  echo Nada para commitar (working tree limpo).
)

REM ===== Push =====
git push origin %BRANCH%
if errorlevel 1 (
  echo ERRO no push (credenciais/permissoes?).
  echo.
  pause
  exit /b 1
)

echo.
echo OK: push feito em %BRANCH% com mensagem: "%MSG%"
echo.
pause
exit /b 0
