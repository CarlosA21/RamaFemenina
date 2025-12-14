@echo off
echo ================================================
echo PUBLICAR PROYECTO x64 - RamaFemenina
echo SOLUCION DEFINITIVA - INCLUYE TODAS LAS DLLs
echo ================================================
echo.

cd /d "%~dp0"

echo Este script compilara y publicara el proyecto
echo asegurando que TODAS las DLLs sean incluidas.
echo.
echo IMPORTANTE: Cierre Visual Studio antes de continuar
echo.
pause

echo.
echo [1/7] Verificando que Visual Studio este cerrado...
tasklist /FI "IMAGENAME eq devenv.exe" 2>NUL | find /I /N "devenv.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo.
    echo ADVERTENCIA: Visual Studio esta abierto
    echo    Se recomienda cerrarlo para evitar conflictos
    echo.
    pause
)

echo.
echo [2/7] Buscando archivo del proyecto (.csproj)...
set PROJECT_FILE=RamaFemenina.csproj
if not exist "%PROJECT_FILE%" (
    echo ERROR: No se encuentra %PROJECT_FILE%
    echo Asegurese de ejecutar este script desde la carpeta del proyecto
    goto :error
)
echo Encontrado: %PROJECT_FILE%

echo.
echo [3/7] Eliminando carpetas bin y obj...
if exist "bin" (
    rmdir /s /q "bin" 2>nul
    echo Carpeta bin eliminada
)
if exist "obj" (
    rmdir /s /q "obj" 2>nul
    echo Carpeta obj eliminada
)

echo.
echo [4/7] Restaurando paquetes del proyecto...
dotnet restore "%PROJECT_FILE%"
if errorlevel 1 goto :error

echo.
echo [5/7] Limpiando proyecto...
dotnet clean "%PROJECT_FILE%" --configuration Release
if errorlevel 1 goto :error

echo.
echo [6/7] Publicando (esto puede tardar 3-7 minutos)...
echo       Esto incluira TODAS las DLLs necesarias
echo.

REM Publicar con self-contained=true para incluir TODAS las dependencias
dotnet publish "%PROJECT_FILE%" ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "bin\win-x64\publish" ^
    /p:Platform=x64 ^
    /p:PublishSingleFile=false ^
    /p:PublishReadyToRun=false ^
    /p:PublishTrimmed=false ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    /p:IncludeAllContentForSelfExtract=true

if errorlevel 1 goto :error

echo.
echo [7/7] Verificando DLLs criticas...
set PUBLISH_DIR=bin\win-x64\publish
set DLL_MISSING=0

if not exist "%PUBLISH_DIR%\Microsoft.UI.Xaml.dll" (
    echo ? FALTA: Microsoft.UI.Xaml.dll
    set DLL_MISSING=1
) else (
    echo ? Microsoft.UI.Xaml.dll
)

if not exist "%PUBLISH_DIR%\Microsoft.WindowsAppRuntime.dll" (
    echo ? FALTA: Microsoft.WindowsAppRuntime.dll
    set DLL_MISSING=1
) else (
    echo ? Microsoft.WindowsAppRuntime.dll
)

if not exist "%PUBLISH_DIR%\Microsoft.Windows.AppLifecycle.dll" (
    echo ? FALTA: Microsoft.Windows.AppLifecycle.dll
    set DLL_MISSING=1
) else (
    echo ? Microsoft.Windows.AppLifecycle.dll
)

if %DLL_MISSING%==1 (
    echo.
    echo ? ADVERTENCIA: Faltan DLLs de WinUI
    echo.
    echo Esto puede ocurrir si Windows App SDK no esta instalado correctamente.
    echo.
    echo SOLUCION:
    echo 1. Instale Windows App SDK Runtime:
    echo    https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
    echo.
    echo 2. Reinicie su PC
    echo.
    echo 3. Vuelva a ejecutar este script
    echo.
    pause
)

echo.
echo ================================================
echo EXITO - Publicacion completada
echo ================================================
echo.
echo Ubicacion del ejecutable:
echo    %PUBLISH_DIR%\RamaFemenina.exe
echo.
echo Tamano aproximado de la carpeta: ~150-200 MB
echo (Incluye .NET Runtime y todas las dependencias)
echo.
echo Proximos pasos:
echo    1. Ejecute: DiagnosticarProblema.bat
echo    2. Si todo esta OK, ejecute: Ejecutar_Con_Log.bat
echo.
echo NOTA: Para distribuir la aplicacion, copie TODA la carpeta
echo       %PUBLISH_DIR%
echo       a la computadora de destino.
echo.
goto :end

:error
echo.
echo ================================================
echo ERROR - La publicacion fallo
echo ================================================
echo.
echo Por favor, revise los mensajes de error arriba.
echo.
echo SOLUCIONES COMUNES:
echo   1. Verifique que esta ejecutando el script desde la carpeta del proyecto
echo      (donde esta RamaFemenina.csproj)
echo.
echo   2. Instale .NET SDK 8.0:
echo      https://dotnet.microsoft.com/download/dotnet/8.0
echo.
echo   3. Instale Windows App SDK Runtime:
echo      https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
echo.
echo   4. Cierre Visual Studio completamente y vuelva a intentar
echo.
echo   5. Si el error persiste, elimine manualmente las carpetas
echo      bin y obj, luego ejecute este script de nuevo
echo.

:end
pause
