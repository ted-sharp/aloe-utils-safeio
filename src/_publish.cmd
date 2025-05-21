@echo off
setlocal enabledelayedexpansion

cd /d %~dp0

echo Publishing Aloe.Utils.SafeIO...

rem Clean previous publish directory
if exist "publish" (
    echo Removing previous publish directory...
    rmdir /s /q publish
)

rem Build the project
echo Building the project...
dotnet build .\Aloe.Utils.SafeIO\Aloe.Utils.SafeIO.csproj -c Release

rem Publish the application
echo Building and publishing...
dotnet publish .\Aloe.Utils.SafeIO\Aloe.Utils.SafeIO.csproj -c Release -r win-x64 -o .\publish\AloeUtilsWafuRomaji

rem Create NuGet package
echo Creating NuGet package...
dotnet pack .\Aloe.Utils.SafeIO\Aloe.Utils.SafeIO.csproj -c Release -o .\publish

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Publish and package creation completed successfully.
) else (
    echo.
    echo Operation failed with error code %ERRORLEVEL%
)

pause
