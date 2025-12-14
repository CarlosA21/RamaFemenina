@echo off
echo ================================================
echo EJECUTAR CON LOG DE ERRORES - RamaFemenina
echo ================================================
echo.

cd /d "%~dp0"

:: Buscar el ejecutable
set EXE_PATH=
if exist "E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe" (
    set EXE_PATH=E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe
) else if exist "E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe" (
    set EXE_PATH=E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe
) else if exist "E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe" (
    set EXE_PATH=E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe
) else if exist "E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe" (
    set EXE_PATH=E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\RamaFemenina.exe
) else (
    echo ERROR: No se encontro RamaFemenina.exe
    echo.
    echo Busque manualmente el archivo RamaFemenina.exe en las carpetas:
    echo - E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\
    echo - E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\publish-final\
    echo.
    pause
    exit /b 1
)

echo Ejecutable encontrado en:
echo %EXE_PATH%
echo.
echo Ejecutando la aplicacion...
echo (Los errores se guardaran automaticamente en app_error_log.txt)
echo.

start "" "%EXE_PATH%"

echo.
echo La aplicacion se ha iniciado.
echo.
echo Si NO se abrio la ventana:
echo   1. Revise el archivo: app_error_log.txt
echo   2. Ejecute: DiagnosticarProblema.bat
echo.
pause
