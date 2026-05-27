@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "buildConfig=%~1"
if "%buildConfig%"=="" set "buildConfig=Release"
set "scriptDir=%~dp0"
set "projectDir=!scriptDir!.."
set "binDir=!projectDir!\bin\!buildConfig!\net9.0"
set "sourceFile=!binDir!\Data\AppSettings\appsettings.prod.json"
set "destinationDirectory=!binDir!"
set "fileToDelete=!binDir!\appsettings.json"
set "fileToRename=!binDir!\appsettings.prod.json"
set "newFileName=appsettings.json"
echo Copying "!sourceFile!" to "!destinationDirectory!"
copy "!sourceFile!" "!destinationDirectory!" 2>nul
if errorlevel 1 echo Warning: Source file not found, skipping copy.
echo Deleting "!fileToDelete!"
del "!fileToDelete!" 2>nul
echo Renaming "!fileToRename!" to "!newFileName!"
ren "!fileToRename!" "!newFileName!" 2>nul
if /I not "%buildConfig%"=="Release" (
  echo Tool pack/update skipped for configuration: %buildConfig%
  echo Batch file completed.
  endlocal
  exit /b 0
)
set "packageSource=!projectDir!\bin\Release"
set "counterFile=!projectDir!\.toolversion.counter"
set "lockFile=!projectDir!\.postbuild.lock"
if exist "!lockFile!" (
  echo Post-build recursion guard hit. Skipping.
  endlocal
  exit /b 0
)
echo 1 > "!lockFile!"
set "counter=0"
if exist "!counterFile!" set /p counter=<"!counterFile!"
for /f "delims=0123456789" %%A in ("!counter!") do set "counter=0"
set /a counter=counter+1
>"!counterFile!" echo !counter!
set "versionPrefix=1.0.0"
set "versionSuffix=dev.!counter!"
set "fullVersion=%versionPrefix%-%versionSuffix%"
echo Packing .NET tool (version %fullVersion%)...
dotnet pack "!projectDir!\cmd_shrtcts.csproj" -c Release -p:Version=%fullVersion% -p:IsPacking=true --no-build
if errorlevel 1 goto :error
echo Updating global tool (cc) to version %fullVersion%...
set "maxRetries=3"
set "retryCount=0"
:retry_update
dotnet tool update --global --add-source "!packageSource!" --version %fullVersion% cmd_shrtcts 2>nul
if errorlevel 1 (
  set /a retryCount=retryCount+1
  if !retryCount! lss %maxRetries% (
    echo Retry !retryCount! of %maxRetries%: Waiting for file locks to release...
    timeout /t 2 /nobreak >nul
    goto :retry_update
  )
  echo Tool update failed after %maxRetries% retries. Trying fresh install...
  dotnet tool uninstall --global cmd_shrtcts 2>nul
  dotnet tool install --global --add-source "!packageSource!" --version %fullVersion% cmd_shrtcts
  if errorlevel 1 (
    echo Warning: Tool install failed. This may be due to file locks from Visual Studio.
    echo The .nupkg was created successfully. Run 'dotnet tool update --global --add-source "!packageSource!" cmd_shrtcts' manually after build.
  )
)
del "!lockFile!" 2>nul
echo Batch file completed.
endlocal
exit /b 0
:error
del "!lockFile!" 2>nul
echo Post-build failed.
endlocal
exit /b 1