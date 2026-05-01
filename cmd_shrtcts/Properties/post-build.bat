@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM Prevent recursive invocation when this script triggers dotnet pack (which builds again)
if /I "%CMD_SHRTCTS_POSTBUILD_RUNNING%"=="1" (
  echo Post-build recursion guard hit. Skipping.
  endlocal
  exit /b 0
)
set "CMD_SHRTCTS_POSTBUILD_RUNNING=1"

REM %1 = MSBuild Configuration (e.g., Debug/Release)
set "buildConfig=%~1"
if "%buildConfig%"=="" set "buildConfig=Release"

REM Set the paths and filenames
set "scriptDir=%~dp0"
set "projectDir=!scriptDir!.."
set "binDir=!projectDir!\bin\!buildConfig!\net9.0"

set "sourceFile=!binDir!\Data\AppSettings\appsettings.prod.json"
set "destinationDirectory=!binDir!"
set "fileToDelete=!binDir!\appsettings.json"
set "fileToRename=!binDir!\appsettings.prod.json"
set "newFileName=appsettings.json"

REM Task 1: Move a file to a new directory
echo Copying "!sourceFile!" to "!destinationDirectory!"
copy "!sourceFile!" "!destinationDirectory!"

REM Task 2: Delete a file
echo Deleting "!fileToDelete!"
del "!fileToDelete!"

REM Task 3: Rename a file
echo Renaming "!fileToRename!" to "!newFileName!"
ren "!fileToRename!" "!newFileName!"

REM Task 4: Pack and update the .NET tool so `sc` points to the latest build
REM IMPORTANT: Only do this for Release builds to avoid Visual Studio build loops.
if /I not "%buildConfig%"=="Release" (
  echo Tool pack/update skipped for configuration: %buildConfig%
  echo Batch file completed.
  endlocal
  exit /b 0
)

set "packageSource=!projectDir!\bin\Release"
set "counterFile=!projectDir!\.toolversion.counter"

REM Read + increment a persistent counter (so each pack has a unique version)
set "counter=0"
if exist "%counterFile%" (
  set /p counter=<"%counterFile%"
)

REM Validate numeric counter
for /f "delims=0123456789" %%A in ("!counter!") do set "counter=0"
set /a counter=counter+1
>"%counterFile%" echo !counter!

set "versionPrefix=1.0.0"
set "versionSuffix=dev.!counter!"
set "fullVersion=%versionPrefix%-%versionSuffix%"

echo Packing .NET tool (version %fullVersion%)...
set "DOTNET_CLI_CONTEXT=postbuild"
dotnet pack "%projectDir%\cmd_shrtcts.csproj" -c Release -p:Version=%fullVersion%
if errorlevel 1 goto :error

echo Updating global tool (cc) to version %fullVersion%...
dotnet tool update --global --add-source "%packageSource%" --version %fullVersion% cmd_shrtcts
if errorlevel 1 (
  echo Tool not installed yet. Installing version %fullVersion%...
  dotnet tool install --global --add-source "%packageSource%" --version %fullVersion% cmd_shrtcts
  if errorlevel 1 goto :error
)

echo Batch file completed.
endlocal
exit /b 0

:error
echo Post-build failed.
endlocal
exit /b 1