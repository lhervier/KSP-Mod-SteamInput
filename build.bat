@echo off
setlocal

echo ===============================
echo Building KSPModSteamInput
echo ===============================

if "%KSPDIR%"=="" goto :no_kspdir

set "KSP_DATA_DIR=%KSPDIR%\KSP_x64_Data"
if not exist "%KSP_DATA_DIR%\Managed\Assembly-CSharp.dll" goto :no_managed

echo Using KSPDIR: %KSPDIR%
echo Using KSP_DATA_DIR: %KSP_DATA_DIR%

echo Removing Release folder
if exist Release rmdir /s /q Release
if errorlevel 1 (
    echo ERROR: Failed to remove the Release folder
    exit /b 1
)

echo Creating Release folder
mkdir Release\KSPModSteamInput
if errorlevel 1 (
    echo ERROR: Failed to create the Mod folder
    exit /b 1
)

echo Building Mod DLL
dotnet build KSPModSteamInput.csproj -p:KSPDIR="%KSPDIR%" -p:KSP_DATA_DIR="%KSP_DATA_DIR%"
if errorlevel 1 (
    echo ERROR: Failed to build the Mod DLL
    exit /b 1
)

echo Copying Mod dll files
copy /y "Output\bin\KSPModSteamInput.dll" "Release\KSPModSteamInput"
if errorlevel 1 (
    echo ERROR: Failed to copy the Mod DLL
    exit /b 1
)

echo Copying settings
copy /y "GameData\KSPModSteamInput\settings.cfg" "Release\KSPModSteamInput"
if errorlevel 1 (
    echo ERROR: Failed to copy the config file
    exit /b 1
)


echo Zipping Mod
powershell -Command "Compress-Archive -Path 'Release\KSPModSteamInput\*' -DestinationPath 'Release\KSPModSteamInput.zip' -Force"
if errorlevel 1 (
    echo ERROR: Failed to zip the Mod
    exit /b 1
)

echo Removing Mod folder
rmdir /s /q Release\KSPModSteamInput
if errorlevel 1 (
    echo ERROR: Failed to remove the Mod folder
    exit /b 1
)

echo Build Complete
echo.
echo Run at: %date% %time%
exit /b 0

:no_kspdir
echo ERROR: KSPDIR is not set - set it to your Kerbal Space Program install path
exit /b 1

:no_managed
echo ERROR: KSP managed assemblies not found at: %KSP_DATA_DIR%\Managed
exit /b 1
