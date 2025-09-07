@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM =============================================================
REM  sync_pull_required_note.bat
REM  - Sempre faz PULL com rebase/autostash.
REM  - Exige DESCRICAO no CONSOLE ANTES de qualquer operacao.
REM  - Salva a nota em pull_notes.log na raiz do repo.
REM  - Sempre finaliza com PAUSE.
REM =============================================================

REM 1) Rodar a partir da pasta do .BAT (evita iniciar em System32)
pushd "%~dp0"

REM 2) Checa Git instalado
git --version >NUL 2>&1
if errorlevel 1 (
  echo [ERRO] Git nao encontrado no PATH. Instale o Git e tente novamente.
  echo.
  pause
  exit /b 1
)

REM 3) Confere se e um repo Git
git rev-parse --is-inside-work-tree >NUL 2>&1
if errorlevel 1 (
  echo [ERRO] Esta pasta nao e um repositorio Git:
  echo       %CD%
  echo Mova este .bat para a raiz do repo, ou abra um CMD nesta pasta.
  echo.
  pause
  exit /b 1
)

REM 4) Descobre branch e upstream (se houver)
for /f "delims=" %%b in ('git rev-parse --abbrev-ref HEAD 2^>NUL') do set "BRANCH=%%b"
if not defined BRANCH set "BRANCH=main"

for /f "delims=" %%u in ('git rev-parse --abbrev-ref --symbolic-full-name @{u} 2^>NUL') do set "UPSTREAM=%%u"
if not defined UPSTREAM set "UPSTREAM=(nao configurado)"

echo Branch atual : %BRANCH%
echo Upstream     : %UPSTREAM%
echo.

REM 5) NOTA OBRIGATORIA (antes de qualquer operacao)
:ask_note
set "NOTE="
set /p NOTE=Descricao OBRIGATORIA deste pull: 
set "NOTE_TRIM=%NOTE%"
set "NOTE_TRIM=%NOTE_TRIM: =%"
if "%NOTE_TRIM%"=="" (
  echo A descricao e obrigatoria; tente novamente.
  echo.
  goto :ask_note
)

REM 6) Busca remoto e mostra previsao do que sera baixado
echo.
echo Atualizando referencias do remoto...
git fetch --prune
if errorlevel 1 (
  echo [ERRO] git fetch falhou.
  echo.
  pause
  exit /b 1
)

for /f "delims=" %%c in ('git rev-list --count %BRANCH%..@{u} 2^>NUL') do set "REMOTE_AHEAD=%%c"
for /f "delims=" %%c in ('git rev-list --count @{u}..%BRANCH% 2^>NUL') do set "LOCAL_AHEAD=%%c"

if not defined REMOTE_AHEAD set "REMOTE_AHEAD=?"
if not defined LOCAL_AHEAD  set "LOCAL_AHEAD=?"

REM 7) Salva nota (apos fetch, com metadados de ahead/behind)
set "LOGFILE=pull_notes.log"
echo [%date% %time%] branch=%BRANCH% upstream=%UPSTREAM% remote_ahead=!REMOTE_AHEAD! local_ahead=!LOCAL_AHEAD! :: %NOTE%>> "%LOGFILE%"
echo Nota salva em "%LOGFILE%".
echo.

if not "!REMOTE_AHEAD!"=="0" (
  echo Novidades no remoto (ultimos 10):
  git log --oneline -n 10 %BRANCH%..@{u} 2>nul
  echo.
)

REM 8) Executa o PULL com rebase/autostash usando upstream configurado
echo Executando: git pull --rebase --autostash
echo.
git pull --rebase --autostash
if errorlevel 1 (
  echo.
  echo ===== CONFLITOS DETECTADOS =====
  echo Corrija os conflitos (arquivos com ^<<^<<< e ^>>>^>>>),
  echo depois rode:  git add ^<arquivos^>  &&  git rebase --continue
  echo Para desistir: git rebase --abort
  echo.
  pause
  exit /b 1
)

echo.
git status -uno

echo.
echo [OK] Pull concluido.
echo.
pause
exit /b 0
