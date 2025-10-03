@echo off
setlocal ENABLEDELAYEDEXPANSION

REM ==== caminhos ====
set "ROOT=%~dp0.."
set "TOOLS=%ROOT%\tools\platform-tools"
set "ADB=%TOOLS%\adb.exe"

if not exist "%ADB%" (
  echo ERRO: adb.exe nao encontrado em: %TOOLS%
  echo Baixe "platform-tools" do Android SDK e extraia nessa pasta.
  pause & exit /b 1
)

REM ==== start server ====
"%ADB%" kill-server >nul 2>&1
"%ADB%" start-server >nul 2>&1

REM ==== detecta device ====
set "HAS_DEVICE="
for /f "skip=1 tokens=1,2" %%A in ('"%ADB%" devices') do (
  if "%%B"=="device" set "HAS_DEVICE=1"
)

if not defined HAS_DEVICE (
  echo Nenhum dispositivo ADB ativo. Conecte o Quest por USB e aceite o "USB debugging",
  echo ou conecte via Wi-Fi usando os scripts quest_wifi_pair/connect_last.
  pause & exit /b 1
)

REM ==== encontra o APK mais recente ====
set "APK="
for /f "usebackq delims=" %%F in (`powershell -NoP -C ^
  "Get-ChildItem -Path '%ROOT%' -Include *.apk -Recurse | Sort-Object LastWriteTime -Desc | Select-Object -First 1 -Expand FullName"`) do (
  set "APK=%%F"
)

if not defined APK (
  echo Nenhum .apk encontrado no projeto. Gere um build Android (APK) e tente de novo.
  pause & exit /b 1
)

echo Instalando: "%APK%"
"%ADB%" install -r "%APK%"
if errorlevel 1 (
  echo Falha na instalacao. Verifique espaco no device, "Unknown sources" e assinatura.
  pause & exit /b 1
)

echo OK.
endlocal
pause
