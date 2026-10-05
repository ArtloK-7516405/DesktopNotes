@echo off
setlocal
cd /d "%~dp0"
echo Desktop Notes - build
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: .NET 8 SDK not found. Install it from https://dotnet.microsoft.com/download/dotnet/8.0
  goto :end
)

echo [1/2] Building single exe...
dotnet publish src\DesktopNotes.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if errorlevel 1 (
  echo.
  echo ERROR: dotnet publish failed. Copy the error text above and send it.
  goto :end
)

echo.
echo [2/2] Creating installer...
set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" (
  echo ERROR: Inno Setup 6 not found. Install it from https://jrsoftware.org/isdl.php
  goto :end
)
"%ISCC%" installer.iss
if errorlevel 1 (
  echo ERROR: Inno Setup failed.
  goto :end
)

echo.
echo DONE: installer\DesktopNotes-Setup.exe
:end
echo.
pause
