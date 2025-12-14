@echo off
echo ============================================
echo   RAMA FEMENINA - Crear Paquete Distribucion
echo ============================================
echo.

set FECHA=%date:~-4%%date:~3,2%%date:~0,2%
set CARPETA_ORIGEN=E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish
set CARPETA_DESTINO=E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina_v1.0_%FECHA%
set ARCHIVO_ZIP=E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina_v1.0_Setup_%FECHA%.zip

echo Fecha: %FECHA%
echo.
echo Carpeta origen: %CARPETA_ORIGEN%
echo Carpeta destino: %CARPETA_DESTINO%
echo.

REM Crear carpeta de distribución
echo [1/4] Creando carpeta de distribucion...
if exist "%CARPETA_DESTINO%" (
    echo Eliminando carpeta anterior...
    rmdir /s /q "%CARPETA_DESTINO%"
)
mkdir "%CARPETA_DESTINO%"
echo OK

REM Copiar archivos
echo.
echo [2/4] Copiando archivos de aplicacion...
xcopy "%CARPETA_ORIGEN%\*.*" "%CARPETA_DESTINO%\" /E /I /Y /Q
echo OK

REM Copiar README
echo.
echo [3/4] Copiando documentacion...
copy "E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\README_DISTRIBUCION.txt" "%CARPETA_DESTINO%\README.txt" /Y
echo OK

REM Crear ZIP (requiere PowerShell)
echo.
echo [4/4] Creando archivo ZIP...
powershell -command "Compress-Archive -Path '%CARPETA_DESTINO%' -DestinationPath '%ARCHIVO_ZIP%' -Force"
echo OK

echo.
echo ============================================
echo   PAQUETE CREADO EXITOSAMENTE
echo ============================================
echo.
echo Carpeta: %CARPETA_DESTINO%
echo Archivo ZIP: %ARCHIVO_ZIP%
echo.
echo Presione cualquier tecla para abrir la carpeta...
pause >nul

explorer "%CARPETA_DESTINO%"

echo.
echo Presione cualquier tecla para salir...
pause >nul
