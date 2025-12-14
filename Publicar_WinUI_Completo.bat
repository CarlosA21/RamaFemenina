@echo off
echo ================================================
echo SOLUCION DEFINITIVA - WinUI 3 + Windows App SDK
echo PUBLICAR CON TODAS LAS DEPENDENCIAS
echo ================================================
echo.

cd /d "%~dp0"

echo IMPORTANTE: Este script resuelve el problema de DLLs faltantes
echo            de Windows App SDK (WinUI 3)
echo.
echo Cierre Visual Studio antes de continuar
echo.
pause

echo.
echo [1/9] Verificando que Visual Studio este cerrado...
tasklist /FI "IMAGENAME eq devenv.exe" 2>NUL | find /I /N "devenv.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo.
    echo ADVERTENCIA: Visual Studio esta abierto
    echo Se recomienda cerrarlo
    echo.
    pause
)

echo.
echo [2/9] Verificando archivo del proyecto...
set PROJECT_FILE=RamaFemenina.csproj
if not exist "%PROJECT_FILE%" (
    echo ERROR: No se encuentra %PROJECT_FILE%
    goto :error
)
echo Encontrado: %PROJECT_FILE%

echo.
echo [3/9] Limpiando completamente...
if exist "bin" rmdir /s /q "bin" 2>nul
if exist "obj" rmdir /s /q "obj" 2>nul
echo Carpetas bin y obj eliminadas

echo.
echo [4/9] Restaurando paquetes...
REM IMPORTANTE: Restaurar con la configuracion de Release y runtime especifico
dotnet restore "%PROJECT_FILE%" --runtime win-x64
if errorlevel 1 goto :error

echo.
echo [5/9] Compilando en modo Release...
dotnet build "%PROJECT_FILE%" --configuration Release --runtime win-x64 --no-restore
if errorlevel 1 goto :error

echo.
echo [6/9] Publicando con WindowsAppSDK...
echo       (Esto puede tardar 3-7 minutos)
echo.

REM IMPORTANTE: 
REM - NO usar PublishTrimmed con WinUI 3 - causa problemas con reflexion
REM - NO usar PublishReadyToRun con WinUI 3 - causa error K1094
REM - NO usar PublishSingleFile con WinUI 3 - no es compatible
REM - WindowsAppSDKSelfContained=true es CRITICO para que funcione
dotnet publish "%PROJECT_FILE%" ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "bin\publish-final" ^
    /p:PublishSingleFile=false ^
    /p:PublishReadyToRun=false ^
    /p:PublishTrimmed=false ^
    /p:TrimMode=none ^
    /p:SelfContained=true ^
    /p:RuntimeIdentifier=win-x64 ^
    /p:WindowsPackageType=None ^
    /p:WindowsAppSDKSelfContained=true ^
    /p:Platform=x64

if errorlevel 1 goto :error

echo.
echo [7/9] Copiando archivos PRI (recursos de WinUI)...
set PUBLISH_DIR=bin\publish-final

REM Buscar y copiar resources.pri desde bin\x64\Release
if exist "bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\resources.pri" (
    echo Copiando resources.pri desde Release...
    copy /Y "bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\resources.pri" "%PUBLISH_DIR%\" >nul
) else if exist "bin\Release\net8.0-windows10.0.19041.0\win-x64\resources.pri" (
    echo Copiando resources.pri desde Release alternativo...
    copy /Y "bin\Release\net8.0-windows10.0.19041.0\win-x64\resources.pri" "%PUBLISH_DIR%\" >nul
) else (
    echo ADVERTENCIA: resources.pri no encontrado - la app podria no funcionar
)

REM Copiar Microsoft.ui.xaml.dll.mui (recursos de interfaz)
if exist "bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\Microsoft.ui.xaml.dll.mui" (
    echo Copiando Microsoft.ui.xaml.dll.mui...
    copy /Y "bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\Microsoft.ui.xaml.dll.mui" "%PUBLISH_DIR%\" >nul
)

echo.
echo [8/9] Copiando archivos adicionales de WindowsAppSDK...

REM Buscar los archivos de WindowsAppSDK en paquetes NuGet
set USERPROFILE_PATH=%USERPROFILE%
set NUGET_PACKAGES=%USERPROFILE_PATH%\.nuget\packages

REM Buscar la versión de WindowsAppSDK instalada
for /f "delims=" %%i in ('dir /b /ad "%NUGET_PACKAGES%\microsoft.windowsappsdk" 2^>nul') do set APPSDK_VERSION=%%i

if defined APPSDK_VERSION (
    echo Encontrada WindowsAppSDK version: %APPSDK_VERSION%
    
    REM Copiar DLLs de WindowsAppSDK
    set SDK_PATH=%NUGET_PACKAGES%\microsoft.windowsappsdk\%APPSDK_VERSION%\runtimes\win-x64\native
    
    if exist "%SDK_PATH%" (
        echo Copiando archivos de WindowsAppSDK desde NuGet...
        xcopy "%SDK_PATH%\*.dll" "%PUBLISH_DIR%\" /Y /Q 2>nul
    )
    
    REM También copiar desde lib
    set SDK_LIB=%NUGET_PACKAGES%\microsoft.windowsappsdk\%APPSDK_VERSION%\lib\net8.0-windows10.0.19041.0
    if exist "%SDK_LIB%" (
        xcopy "%SDK_LIB%\*.dll" "%PUBLISH_DIR%\" /Y /Q 2>nul
    )
)

echo.
echo [9/9] Verificando archivos criticos...
set ALL_OK=1

echo.
echo Verificando archivos en: %PUBLISH_DIR%
echo.

if not exist "%PUBLISH_DIR%\RamaFemenina.exe" (
    echo ERROR: RamaFemenina.exe no encontrado
    set ALL_OK=0
) else (
    echo OK: RamaFemenina.exe
)

if not exist "%PUBLISH_DIR%\resources.pri" (
    echo CRITICO: resources.pri no encontrado - LA APP NO FUNCIONARA
    set ALL_OK=0
) else (
    echo OK: resources.pri (CRITICO para WinUI)
)

if not exist "%PUBLISH_DIR%\Microsoft.UI.Xaml.dll" (
    echo FALTA: Microsoft.UI.Xaml.dll
    set ALL_OK=0
) else (
    echo OK: Microsoft.UI.Xaml.dll
)

if not exist "%PUBLISH_DIR%\Microsoft.WindowsAppRuntime.dll" (
    echo FALTA: Microsoft.WindowsAppRuntime.dll
    set ALL_OK=0
) else (
    echo OK: Microsoft.WindowsAppRuntime.dll
)

REM Nombre correcto del archivo: incluye .Projection
if not exist "%PUBLISH_DIR%\Microsoft.Windows.AppLifecycle.Projection.dll" (
    echo FALTA: Microsoft.Windows.AppLifecycle.Projection.dll
    set ALL_OK=0
) else (
    echo OK: Microsoft.Windows.AppLifecycle.Projection.dll
)

if not exist "%PUBLISH_DIR%\Microsoft.EntityFrameworkCore.dll" (
    echo ADVERTENCIA: Microsoft.EntityFrameworkCore.dll no encontrado
) else (
    echo OK: Microsoft.EntityFrameworkCore.dll
)

echo.
if %ALL_OK%==0 (
    echo ================================================
    echo ERROR CRITICO: Faltan archivos esenciales
    echo ================================================
    echo.
    echo El archivo resources.pri es CRITICO para WinUI 3
    echo Sin este archivo, la app dara el error:
    echo "Cannot locate resource from 'ms-appx:///Microsoft.UI.Xaml/Themes/themeresources.xaml'"
    echo.
    echo SOLUCION:
    echo 1. Asegurese de compilar en Release primero con Visual Studio
    echo 2. Verifique que existe: bin\x64\Release\...\resources.pri
    echo 3. Si no existe, limpie la solucion y recompile
    echo.
    pause
    goto :error
)

:success
echo.
echo ================================================
echo EXITO - Publicacion completada
echo ================================================
echo.
echo Ubicacion del ejecutable:
echo    %PUBLISH_DIR%\RamaFemenina.exe
echo.
echo IMPORTANTE - Archivos CRITICOS verificados:
echo   [X] resources.pri - Recursos de WinUI
echo   [X] Microsoft.UI.Xaml.dll - Framework UI
echo   [X] Microsoft.WindowsAppRuntime.dll - Runtime
echo.
echo Para distribuir la aplicacion:
echo   1. Copie TODA la carpeta %PUBLISH_DIR%
echo   2. En la PC destino, instale Windows App SDK Runtime:
echo      https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
echo.
echo Proximos pasos:
echo   1. Pruebe ejecutando: %PUBLISH_DIR%\RamaFemenina.exe
echo   2. Si funciona, empaquete la carpeta para distribucion
echo.
goto :end

:error
echo.
echo ================================================
echo ERROR - La publicacion fallo
echo ================================================
echo.
echo Revise los mensajes de error arriba.
echo.
echo SOLUCIONES:
echo   1. Compile primero en Release con Visual Studio (Ctrl+Shift+B)
echo   2. Verifique que se genere resources.pri en bin\x64\Release
echo   3. Instale .NET SDK 8.0 completo (no solo runtime)
echo   4. Instale Windows App SDK:
echo      https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
echo   5. Cierre Visual Studio y vuelva a intentar
echo.

:end
pause
