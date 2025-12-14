@echo off
echo ================================================
echo DIAGNOSTICO DE PROBLEMAS - RamaFemenina
echo ================================================
echo.

:: Cambiar al directorio donde está el .bat
cd /d "%~dp0"

echo [1] Verificando ubicacion del ejecutable...
echo Directorio actual: %CD%
echo.

:: Buscar el ejecutable en multiples ubicaciones
set EXE_PATH=
if exist "E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe" (
    set EXE_PATH=E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe
    echo Ejecutable encontrado en: %EXE_PATH%
) else if exist "E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe" (
    set EXE_PATH=E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe
    echo Ejecutable encontrado en: %EXE_PATH%
) else if exist "E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe" (
    set EXE_PATH=E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe
    echo Ejecutable encontrado en: %EXE_PATH%
) else if exist "E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe" (
    set EXE_PATH=E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe
    echo Ejecutable encontrado en: %EXE_PATH%
) else if exist "E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe" (
    set EXE_PATH=E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe
    echo Ejecutable encontrado en: %EXE_PATH%
) else (
    echo ERROR: No se encontro RamaFemenina.exe
    echo.
    echo Ubicaciones buscadas:
    echo   - E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\
    echo   - E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\
    echo   - E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\
    echo   - E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\
    echo   - E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\
    echo.
    echo SOLUCION: Ejecute Publicar_x64.bat primero
    pause
    exit /b 1
)

echo.
echo [2] Verificando archivos DLL necesarios...
for %%i in ("%EXE_PATH%") do set EXE_DIR=%%~dpi
cd /d "%EXE_DIR%"
echo Directorio ejecutable: %CD%
echo.

:: Verificar DLLs críticas
set MISSING=0
if not exist "Microsoft.UI.Xaml.dll" (
    echo FALTA: Microsoft.UI.Xaml.dll
    set MISSING=1
) else (
    echo Microsoft.UI.Xaml.dll
)

if not exist "Microsoft.WindowsAppRuntime.dll" (
    echo FALTA: Microsoft.WindowsAppRuntime.dll
    set MISSING=1
) else (
    echo Microsoft.WindowsAppRuntime.dll
)

if not exist "Microsoft.Windows.AppLifecycle.dll" (
    echo FALTA: Microsoft.Windows.AppLifecycle.dll
    set MISSING=1
) else (
    echo Microsoft.Windows.AppLifecycle.dll
)

if not exist "Microsoft.EntityFrameworkCore.dll" (
    echo FALTA: Microsoft.EntityFrameworkCore.dll
    set MISSING=1
) else (
    echo Microsoft.EntityFrameworkCore.dll
)

if %MISSING%==1 (
    echo.
    echo ERROR: Faltan archivos DLL necesarios
    echo.
    echo CAUSA PROBABLE:
    echo   El proyecto NO se publico con --self-contained true
    echo   o Windows App SDK no esta instalado
    echo.
    echo SOLUCION:
    echo   1. Instale Windows App SDK Runtime:
    echo      https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
    echo.
    echo   2. Cierre Visual Studio si esta abierto
    echo.
    echo   3. Ejecute: Publicar_x64.bat
    echo.
    echo   4. Vuelva a ejecutar este diagnostico
    echo.
    pause
    exit /b 1
)

echo.
echo [3] Verificando .NET Runtime 8.0...
dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 8.0" >nul
if errorlevel 1 (
    echo ADVERTENCIA: .NET 8.0 Desktop Runtime NO esta instalado
    echo.
    echo Si publico con --self-contained true, NO es necesario instalar .NET
    echo.
    echo Si publico con --self-contained false, descargue e instale:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo Busque: ".NET Desktop Runtime 8.0.x"
    echo.
) else (
    echo .NET 8.0 Desktop Runtime instalado
)

echo.
echo [4] Verificando Windows App SDK Runtime...
reg query "HKLM\SOFTWARE\Microsoft\WindowsAppRuntime" >nul 2>&1
if errorlevel 1 (
    echo ADVERTENCIA: Windows App SDK Runtime podria no estar instalado
    echo.
    echo Si la aplicacion no abre, instale desde:
    echo https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
    echo.
) else (
    echo Windows App SDK Runtime instalado
)

echo.
echo [5] Verificando archivos de configuracion...
if not exist "appsettings.json" (
    echo ADVERTENCIA: appsettings.json no encontrado
    echo   La app usara valores por defecto
) else (
    echo appsettings.json encontrado
)

echo.
echo [6] Intentando ejecutar con logs detallados...
echo ================================================
echo.
echo EJECUTANDO APLICACION...
echo (Si ve errores a continuacion, tomeles captura)
echo.
echo ================================================
echo.

:: Cambiar al directorio raíz del proyecto para crear logs ahí
cd /d "%~dp0"

:: Ejecutar con variables de entorno para logs
set COREHOST_TRACE=1
set COREHOST_TRACEFILE=startup_log.txt

"%~dp0%EXE_PATH%" 2> error_log.txt

echo.
echo ================================================
echo.
echo Si la aplicacion no abrio, revise:
echo   1. error_log.txt - Errores de ejecucion
echo   2. startup_log.txt - Log de inicio de .NET
echo   3. app_error_log.txt - Errores de la aplicacion (en carpeta del .exe)
echo.

if exist error_log.txt (
    echo Contenido de error_log.txt:
    echo ------------------------------------------------
    type error_log.txt
    echo ------------------------------------------------
    echo.
)

if exist startup_log.txt (
    echo Contenido de startup_log.txt:
    echo ------------------------------------------------
    type startup_log.txt
    echo ------------------------------------------------
)

echo.
echo ================================================
echo DIAGNOSTICO COMPLETADO
echo ================================================
echo.
echo RESUMEN:
if %MISSING%==0 (
    echo   Todas las DLLs estan presentes
) else (
    echo   ERROR: Faltan DLLs - Vea soluciones arriba
)
echo.
echo Si la app no abre:
echo   1. Revise los archivos de log arriba
echo   2. Consulte: EMERGENCIA-ERROR-PLATAFORMA.txt
echo   3. Consulte: SOLUCION-EXE-NO-ABRE.md
echo.
pause
