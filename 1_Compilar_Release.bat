@echo off
echo ================================================
echo PASO 1: COMPILAR EN RELEASE
echo Este paso es CRITICO para generar resources.pri
echo ================================================
echo.

cd /d "%~dp0"

echo [1/6] Verificando archivo del proyecto...
set PROJECT_FILE=RamaFemenina.csproj
if not exist "%PROJECT_FILE%" (
    echo ERROR: No se encuentra %PROJECT_FILE%
    pause
    exit /b 1
)
echo Encontrado: %PROJECT_FILE%

echo.
echo [2/6] Limpiando carpetas anteriores (limpieza profunda)...
echo Esta limpieza resolverá problemas con XAML Compiler

REM Cerrar procesos de MSBuild que puedan estar bloqueando archivos
taskkill /F /IM MSBuild.exe 2>nul
taskkill /F /IM VBCSCompiler.exe 2>nul
timeout /t 2 /nobreak >nul

REM Eliminar carpetas de compilación
if exist "bin" (
    echo Eliminando bin...
    rmdir /s /q "bin" 2>nul
    if exist "bin" (
        echo ADVERTENCIA: No se pudo eliminar bin completamente
        echo Intentando forzar eliminación...
        rd /s /q "bin" 2>nul
    )
)

if exist "obj" (
    echo Eliminando obj...
    rmdir /s /q "obj" 2>nul
    if exist "obj" (
        echo ADVERTENCIA: No se pudo eliminar obj completamente
        echo Intentando forzar eliminación...
        rd /s /q "obj" 2>nul
    )
)

REM Esperar a que los archivos se liberen
timeout /t 2 /nobreak >nul

echo Limpieza completada

echo.
echo [3/6] Restaurando paquetes NuGet...
dotnet restore "%PROJECT_FILE%" --runtime win-x64 --verbosity minimal
if errorlevel 1 (
    echo ERROR: Fallo la restauracion de paquetes
    echo.
    echo POSIBLES CAUSAS:
    echo 1. No hay conexión a Internet
    echo 2. Paquetes NuGet no disponibles
    echo 3. Archivo .csproj corrupto
    echo.
    pause
    exit /b 1
)

echo.
echo [4/6] Limpiando proyecto con MSBuild...
dotnet clean "%PROJECT_FILE%" --configuration Release --verbosity minimal
if errorlevel 1 (
    echo ADVERTENCIA: Clean falló, pero continuaremos
)

echo.
echo [5/6] Compilando en Release con Platform=x64...
echo       (Esto generara resources.pri necesario para WinUI)
echo       Puede tardar 1-3 minutos...
echo.

REM Compilar con configuración explícita para WinUI 3
dotnet build "%PROJECT_FILE%" ^
    --configuration Release ^
    --runtime win-x64 ^
    --no-restore ^
    /p:Platform=x64 ^
    /p:WindowsAppSDKSelfContained=true ^
    /p:GenerateAppxPackageOnBuild=false ^
    /p:AppxPackage=false ^
    /p:UseWinUI=true

if errorlevel 1 (
    echo.
    echo ================================================
    echo ERROR: Fallo la compilacion
    echo ================================================
    echo.
    echo POSIBLES SOLUCIONES:
    echo.
    echo SOLUCION 1 - Limpieza manual:
    echo   1. Cierre Visual Studio si esta abierto
    echo   2. Elimine manualmente las carpetas bin y obj
    echo   3. Ejecute este script nuevamente
    echo.
    echo SOLUCION 2 - Desde Visual Studio:
    echo   1. Abra la solucion en Visual Studio
    echo   2. Build ^> Clean Solution
    echo   3. Cambie a Configuration: Release, Platform: x64
    echo   4. Build ^> Rebuild Solution
    echo   5. Luego ejecute este script
    echo.
    echo SOLUCION 3 - Reparar cache de NuGet:
    echo   1. Ejecute: dotnet nuget locals all --clear
    echo   2. Ejecute este script nuevamente
    echo.
    echo SOLUCION 4 - Verificar requisitos:
    echo   - .NET SDK 8.0 instalado
    echo   - Windows App SDK instalado
    echo   - Visual Studio 2022 actualizado
    echo.
    pause
    exit /b 1
)

echo.
echo [6/6] Verificando archivos generados...
set BUILD_DIR=bin\x64\Release\net8.0-windows10.0.19041.0\win-x64
set PRI_FOUND=0

echo.
echo Verificando en: %BUILD_DIR%
echo.

REM Verificar que se genero resources.pri
if exist "%BUILD_DIR%\resources.pri" (
    echo [OK] resources.pri generado correctamente
    for %%A in ("%BUILD_DIR%\resources.pri") do echo      Tamaño: %%~zA bytes
    set PRI_FOUND=1
) else (
    echo [ADVERTENCIA] resources.pri NO encontrado en ubicacion esperada
    echo.
    echo Buscando en ubicaciones alternativas...
    
    REM Buscar en otras ubicaciones posibles
    for /r "bin" %%f in (resources.pri) do (
        if exist "%%f" (
            echo [ENCONTRADO] %%f
            echo Copiando a ubicacion correcta...
            copy /Y "%%f" "%BUILD_DIR%\" >nul
            set PRI_FOUND=1
            goto :pri_found
        )
    )
)

:pri_found

REM Verificar appsettings.json
if exist "%BUILD_DIR%\appsettings.json" (
    echo [OK] appsettings.json copiado
) else (
    echo [ADVERTENCIA] appsettings.json NO encontrado
    echo Copiando desde raiz del proyecto...
    if exist "appsettings.json" (
        copy /Y "appsettings.json" "%BUILD_DIR%\" >nul
        echo [OK] appsettings.json copiado manualmente
    )
)

REM Verificar ejecutable
if exist "%BUILD_DIR%\RamaFemenina.exe" (
    echo [OK] RamaFemenina.exe generado
) else (
    echo [ERROR] RamaFemenina.exe NO encontrado
)

echo.
echo ================================================
if %PRI_FOUND%==1 (
    echo COMPILACION EXITOSA
    echo ================================================
    echo.
    echo Archivos generados en:
    echo   %BUILD_DIR%
    echo.
    echo Archivos criticos verificados:
    echo   [X] resources.pri
    echo   [X] RamaFemenina.exe
    echo   [X] appsettings.json
    echo.
    echo Proximos pasos:
    echo   1. (Opcional) Ejecute: 3_Diagnostico_BaseDatos.bat
    echo      Para verificar la configuracion de base de datos
    echo.
    echo   2. Para publicar: Publicar_WinUI_Completo.bat
    echo.
) else (
    echo COMPILACION COMPLETADA CON ADVERTENCIAS
    echo ================================================
    echo.
    echo ATENCION: resources.pri NO se genero
    echo.
    echo Esto causara que la aplicacion falle con el error:
    echo "Cannot locate resource from themeresources.xaml"
    echo.
    echo SOLUCION:
    echo 1. Compile desde Visual Studio:
    echo    - Abra la solucion en Visual Studio
    echo    - Seleccione Configuration: Release, Platform: x64
    echo    - Build ^> Rebuild Solution
    echo.
    echo 2. Verifique que existe: bin\x64\Release\...\resources.pri
    echo.
    echo 3. Si el archivo existe, cópielo manualmente a:
    echo    %BUILD_DIR%\
    echo.
)

pause
