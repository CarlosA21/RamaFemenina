@echo off
echo ================================================
echo DIAGNOSTICO - Verificacion de Publicacion WinUI
echo ================================================
echo.

cd /d "%~dp0"

set PUBLISH_DIR=bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\
set ERRORS=0
set WARNINGS=0

echo Verificando carpeta de publicacion: %PUBLISH_DIR%
echo.

if not exist "%PUBLISH_DIR%" (
    echo ERROR: La carpeta de publicacion no existe
    echo.
    echo SOLUCION: Ejecute primero Publicar_WinUI_Completo.bat
    pause
    exit /b 1
)

echo ================================================
echo ARCHIVOS CRITICOS (imprescindibles)
echo ================================================
echo.

if exist "%PUBLISH_DIR%\RamaFemenina.exe" (
    echo [OK] RamaFemenina.exe
) else (
    echo [ERROR] RamaFemenina.exe NO ENCONTRADO
    set /a ERRORS+=1
)

if exist "%PUBLISH_DIR%\resources.pri" (
    echo [OK] resources.pri
    for %%A in ("%PUBLISH_DIR%\resources.pri") do echo      Tama�o: %%~zA bytes
) else (
    echo [ERROR] resources.pri NO ENCONTRADO
    echo         Este archivo es CRITICO - sin el la app fallara con:
    echo         "Cannot locate resource from themeresources.xaml"
    set /a ERRORS+=1
)

if exist "%PUBLISH_DIR%\Microsoft.UI.Xaml.dll" (
    echo [OK] Microsoft.UI.Xaml.dll
) else (
    echo [ERROR] Microsoft.UI.Xaml.dll NO ENCONTRADO
    set /a ERRORS+=1
)

if exist "%PUBLISH_DIR%\Microsoft.WindowsAppRuntime.dll" (
    echo [OK] Microsoft.WindowsAppRuntime.dll
) else (
    echo [ERROR] Microsoft.WindowsAppRuntime.dll NO ENCONTRADO
    set /a ERRORS+=1
)

if exist "%PUBLISH_DIR%\Microsoft.Windows.AppLifecycle.Projection.dll" (
    echo [OK] Microsoft.Windows.AppLifecycle.Projection.dll
) else (
    echo [ERROR] Microsoft.Windows.AppLifecycle.Projection.dll NO ENCONTRADO
    set /a ERRORS+=1
)

echo.
echo ================================================
echo ARCHIVOS DE CONFIGURACION
echo ================================================
echo.

if exist "%PUBLISH_DIR%\appsettings.json" (
    echo [OK] appsettings.json
    for %%A in ("%PUBLISH_DIR%\appsettings.json") do echo      Tama�o: %%~zA bytes
    
    REM Leer y mostrar informaci�n b�sica sin contrase�as
    echo.
    echo Contenido (primeras l�neas):
    powershell -Command "Get-Content '%PUBLISH_DIR%\appsettings.json' -First 5"
) else (
    echo [ERROR] appsettings.json NO ENCONTRADO
    echo         La aplicaci�n no podr� conectarse a la base de datos
    set /a ERRORS+=1
)

echo.
echo ================================================
echo ARCHIVOS IMPORTANTES (recomendados)
echo ================================================
echo.

if exist "%PUBLISH_DIR%\Microsoft.ui.xaml.dll.mui" (
    echo [OK] Microsoft.ui.xaml.dll.mui (recursos de interfaz)
) else (
    echo [ADVERTENCIA] Microsoft.ui.xaml.dll.mui no encontrado
    set /a WARNINGS+=1
)

if exist "%PUBLISH_DIR%\Microsoft.EntityFrameworkCore.dll" (
    echo [OK] Microsoft.EntityFrameworkCore.dll
) else (
    echo [ADVERTENCIA] Microsoft.EntityFrameworkCore.dll no encontrado
    set /a WARNINGS+=1
)

if exist "%PUBLISH_DIR%\Microsoft.EntityFrameworkCore.SqlServer.dll" (
    echo [OK] Microsoft.EntityFrameworkCore.SqlServer.dll
) else (
    echo [ADVERTENCIA] Microsoft.EntityFrameworkCore.SqlServer.dll no encontrado
    echo              La aplicaci�n no podr� conectarse a SQL Server
    set /a WARNINGS+=1
)

echo.
echo ================================================
echo ARCHIVOS NATIVOS DE WINDOWS APP SDK
echo ================================================
echo.

set NATIVE_OK=0
for %%F in (
    "Microsoft.WindowsAppRuntime.Bootstrap.dll"
    "Microsoft.WindowsAppRuntime.Insights.dll"
    "Microsoft.InteractiveExperiences.Projection.dll"
) do (
    if exist "%PUBLISH_DIR%\%%~F" (
        echo [OK] %%~F
        set /a NATIVE_OK+=1
    ) else (
        echo [FALTA] %%~F
    )
)

if %NATIVE_OK% LSS 2 (
    echo.
    echo [ADVERTENCIA] Faltan DLLs nativas de WindowsAppSDK
    echo               La aplicacion podria necesitar Windows App SDK Runtime instalado
    set /a WARNINGS+=1
)

echo.
echo ================================================
echo LOGS DE LA APLICACION
echo ================================================
echo.

if exist "%PUBLISH_DIR%\app_error_log.txt" (
    echo Se encontr� archivo de log de ejecuciones anteriores:
    echo.
    echo �ltimas 20 l�neas del log:
    echo ----------------------------------------
    powershell -Command "Get-Content '%PUBLISH_DIR%\app_error_log.txt' -Tail 20"
    echo ----------------------------------------
    echo.
    echo Para ver el log completo, abra: %PUBLISH_DIR%\app_error_log.txt
) else (
    echo No hay logs de ejecuciones anteriores
    echo El log se crear� en: %PUBLISH_DIR%\app_error_log.txt
)

echo.
echo ================================================
echo RESUMEN DEL DIAGNOSTICO
echo ================================================
echo.

if %ERRORS% GTR 0 (
    echo Estado: CRITICO - %ERRORS% errores encontrados
    echo.
    echo La aplicacion NO funcionara correctamente.
    echo.
    echo SOLUCION:
    echo 1. Ejecute: 1_Compilar_Release.bat
    echo 2. Verifique que se genere resources.pri
    echo 3. Ejecute: Publicar_WinUI_Completo.bat
    echo 4. Verifique que appsettings.json se copie
    echo.
) else if %WARNINGS% GTR 0 (
    echo Estado: ADVERTENCIA - %WARNINGS% advertencias
    echo.
    echo La aplicacion deberia funcionar, pero puede necesitar
    echo Windows App SDK Runtime instalado en la PC destino.
    echo.
    echo Descarga: https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
    echo.
) else (
    echo Estado: PERFECTO - Todos los archivos presentes
    echo.
    echo La aplicacion esta lista para distribuir!
    echo.
    echo Pasos para probar:
    echo 1. Ejecute: %PUBLISH_DIR%\RamaFemenina.exe
    echo 2. Revise el log en: %PUBLISH_DIR%\app_error_log.txt
    echo 3. Si funciona, empaquete la carpeta para distribucion
    echo.
    echo Para distribuir a otras PCs:
    echo - Copie TODA la carpeta %PUBLISH_DIR%
    echo - Asegurese de incluir appsettings.json con la configuracion correcta
    echo - Instale Windows App SDK Runtime en la PC destino (si es necesario)
    echo.
)

echo ================================================
echo UBICACION DE LA PUBLICACION
echo ================================================
echo.
echo %CD%\%PUBLISH_DIR%
echo.

if %ERRORS% GTR 0 (
    pause
    exit /b 1
) else (
    pause
    exit /b 0
)
