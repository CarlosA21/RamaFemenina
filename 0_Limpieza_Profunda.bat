@echo off
echo ================================================
echo LIMPIEZA PROFUNDA DEL PROYECTO
echo ================================================
echo.
echo ADVERTENCIA: Este script eliminará TODOS los archivos
echo             de compilación y cache de NuGet local.
echo.
echo Esto es útil cuando hay errores de compilación
echo persistentes, especialmente con XAML Compiler.
echo.

choice /C SN /M "Desea continuar con la limpieza profunda"

if errorlevel 2 goto :end
if errorlevel 1 goto :clean

:clean
cd /d "%~dp0"

echo.
echo [1/7] Cerrando procesos de compilación...
taskkill /F /IM MSBuild.exe 2>nul
taskkill /F /IM VBCSCompiler.exe 2>nul
taskkill /F /IM dotnet.exe 2>nul
timeout /t 2 /nobreak >nul
echo Procesos cerrados

echo.
echo [2/7] Eliminando carpetas bin y obj...
if exist "bin" (
    echo Eliminando bin...
    rmdir /s /q "bin" 2>nul
    if exist "bin" (
        echo Forzando eliminación de bin...
        rd /s /q "bin" 2>nul
        timeout /t 1 /nobreak >nul
    )
)

if exist "obj" (
    echo Eliminando obj...
    rmdir /s /q "obj" 2>nul
    if exist "obj" (
        echo Forzando eliminación de obj...
        rd /s /q "obj" 2>nul
        timeout /t 1 /nobreak >nul
    )
)

echo.
echo [3/7] Eliminando archivos temporales de Visual Studio...
del /s /q *.user 2>nul
del /s /q *.suo 2>nul
del /s /q *.cache 2>nul
if exist ".vs" (
    rmdir /s /q ".vs" 2>nul
)
echo Archivos temporales eliminados

echo.
echo [4/7] Limpiando cache de NuGet local...
echo NOTA: Esto puede tardar 1-2 minutos
dotnet nuget locals all --clear
echo Cache de NuGet limpiado

echo.
echo [5/7] Limpiando cache de compilación temporal...
if exist "%TEMP%\RamaFemenina" (
    rmdir /s /q "%TEMP%\RamaFemenina" 2>nul
)
if exist "%LOCALAPPDATA%\Temp\RamaFemenina" (
    rmdir /s /q "%LOCALAPPDATA%\Temp\RamaFemenina" 2>nul
)
echo Cache temporal limpiado

echo.
echo [6/7] Verificando limpieza...
set CLEAN_OK=1

if exist "bin" (
    echo [ADVERTENCIA] bin todavia existe
    set CLEAN_OK=0
)

if exist "obj" (
    echo [ADVERTENCIA] obj todavia existe
    set CLEAN_OK=0
)

if %CLEAN_OK%==1 (
    echo [OK] Limpieza completada exitosamente
) else (
    echo [ADVERTENCIA] Algunas carpetas no se pudieron eliminar
    echo              Intente cerrar Visual Studio y ejecutar nuevamente
)

echo.
echo [7/7] Esperando 3 segundos para que el sistema libere archivos...
timeout /t 3 /nobreak >nul

echo.
echo ================================================
echo LIMPIEZA PROFUNDA COMPLETADA
echo ================================================
echo.
echo El proyecto ha sido limpiado completamente.
echo.
echo Proximos pasos:
echo 1. Ejecute: 1_Compilar_Release.bat
echo 2. Si sigue fallando, compile desde Visual Studio:
echo    - Abra la solución
echo    - Build ^> Clean Solution
echo    - Build ^> Rebuild Solution
echo.

goto :end

:end
pause
