@echo off
setlocal
set "ROOT=%~dp0.."
set "TOOLS=%ROOT%\tools\platform-tools"
set "ADB=%TOOLS%\adb.exe"
set "LASTIPFILE=%ROOT%\last_ip.txt"

if not exist "%ADB%" (
  echo ERRO: adb.exe nao encontrado em: %TOOLS%
  pause & exit /b 1
)

if not exist "%LASTIPFILE%" (
  echo Nenhum IP salvo. Rode primeiro: quest_wifi_pair.cmd
  pause & exit /b 1
)

set /p QUESTIP=<"%LASTIPFILE%"
if "%QUESTIP%"=="" (
  echo Arquivo de IP vazio. Rode novamente quest_wifi_pair.cmd
  pause & exit /b 1
)

"%ADB%" connect %QUESTIP%:5555
"%ADB%" devices
endlocal
pause
