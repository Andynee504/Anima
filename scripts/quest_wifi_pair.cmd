@echo off
setlocal
set "ROOT=%~dp0.."
set "TOOLS=%ROOT%\tools\platform-tools"
set "ADB=%TOOLS%\adb.exe"

if not exist "%ADB%" (
  echo ERRO: adb.exe nao encontrado em: %TOOLS%
  pause & exit /b 1
)

echo 1) Conecte o Quest via USB e aceite "USB debugging".
pause

"%ADB%" tcpip 5555

echo.
set /p QUESTIP=2) Digite o IP do Quest (Settings > Wi-Fi > engrenagem): 
if "%QUESTIP%"=="" ( echo IP vazio. & pause & exit /b 1 )

"%ADB%" connect %QUESTIP%:5555
if errorlevel 1 ( echo Falhou conectar. Confira o IP e a rede. & pause & exit /b 1 )

echo %QUESTIP%>"%ROOT%\last_ip.txt"
echo Conectado em %QUESTIP%:5555
"%ADB%" devices
endlocal
pause
