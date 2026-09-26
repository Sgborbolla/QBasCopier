@echo off
setlocal EnableExtensions
title "QBasCopier y Transfer - Build"
cd /d "%~dp0"
set "SDK=dotnet"

where %SDK% >nul 2>&1
if errorlevel 1 (
    echo.
    echo  [ERROR] .NET SDK 10 not found on this PC.
    echo  Install it from: https://dotnet.microsoft.com/download/dotnet/10.0
    echo  (choose "SDK x64" for Windows).
    echo.
    pause
    exit /b 1
)

echo.
echo  [1/4] Publishing QBasCopier^&Transfer (portable, win-x64)...
if exist "dist\Windows" rmdir /s /q "dist\Windows" >nul 2>&1
%SDK% publish "QBasCopier\QBasCopier.csproj" -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:ErrorOnDuplicatePublishOutputFiles=false ^
    -o "dist\windows-tmp" || goto :err
mkdir "dist\Windows" >nul 2>&1
rem el apphost conserva el nombre del ensamblado; aqui se renombra al nombre largo
move /y "dist\windows-tmp\QBasCopier.exe" "dist\Windows\QBasCopier-y-Transfer.exe" >nul || goto :err
rmdir /s /q "dist\windows-tmp" >nul 2>&1

echo  [2/4] Embedding app.exe into the Setup project...
copy /y "dist\Windows\QBasCopier-y-Transfer.exe" "QBasCopierSetup\Assets\app.exe" >nul || goto :err

echo  [3/4] Publishing QBasCopier^&Transfer Setup (installer)...
%SDK% publish "QBasCopierSetup\QBasCopierSetup.csproj" -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:ErrorOnDuplicatePublishOutputFiles=false ^
    -o "dist\setup-tmp" || goto :err
move /y "dist\setup-tmp\QBasCopierSetup.exe" "dist\Windows\QBasCopier-y-Transfer Setup.exe" >nul || goto :err
rmdir /s /q "dist\setup-tmp" >nul 2>&1

echo  [4/4] Done.
echo.
echo  ============================================================
echo   LISTO!  Todo en dist\Windows\ :
echo    - "QBasCopier-y-Transfer.exe"        (portable, listo para copiar a PC)
echo    - "QBasCopier-y-Transfer Setup.exe"  (instalador con musica e idioma)
echo   Linux -^> dist\Linux\   macOS -^> dist\macOS\  (build.sh / make-app.sh)
echo   Android -^> dist\QBasCopier-y-Transfer-v1.1.apk  (build-android.sh)
echo  ============================================================
echo.
pause
exit /b 0

:err
echo.
echo  [ERROR] Build failed. See messages above.
echo  Fix: make sure the .NET 10 SDK is installed and QBasCopier compiles.
echo.
pause
exit /b 1
