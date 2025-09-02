@echo off
set BRANCH=main
git pull --autostash --rebase origin %BRANCH% || (
  echo ===== CONFLITOS DETECTADOS =====
  echo Edite os arquivos marcados <<<<<<< e >>>>>>>, salve e rode:
  echo   git add <arquivos>  &&  git rebase --continue
  exit /b 1
)