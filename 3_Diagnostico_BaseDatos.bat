@echo off
echo ================================================
echo DIAGNOSTICO DE CONEXION A BASE DE DATOS
echo ================================================
echo.

cd /d "%~dp0"

set PUBLISH_DIR=bin\x64\Release\net8.0-windows10.0.19041.0\win-x64

echo Este script verifica la configuracion de conexion a BD
echo.

REM Verificar si existe la carpeta de publicacion
if not exist "%PUBLISH_DIR%" (
    echo ERROR: La carpeta %PUBLISH_DIR% no existe
    echo.
    echo Ejecute primero: Publicar_WinUI_Completo.bat
    pause
    exit /b 1
)

echo ================================================
echo 1. VERIFICANDO APPSETTINGS.JSON
echo ================================================
echo.

if exist "%PUBLISH_DIR%\appsettings.json" (
    echo [OK] appsettings.json encontrado
    echo.
    echo Ubicacion: %PUBLISH_DIR%\appsettings.json
    echo.
    
    REM Mostrar contenido completo
    echo Contenido completo:
    echo ----------------------------------------
    type "%PUBLISH_DIR%\appsettings.json"
    echo.
    echo ----------------------------------------
    echo.
    
    REM Extraer informaci�n del servidor (sin contrase�a)
    echo Informaci�n de conexi�n:
    for /f "tokens=*" %%a in ('powershell -Command "(Get-Content '%PUBLISH_DIR%\appsettings.json' | ConvertFrom-Json).ConnectionStrings.DefaultConnection"') do set CONN_STRING=%%a
    
    if defined CONN_STRING (
        echo Connection String configurado: SI
        echo.
        
        REM Extraer servidor (simplificado)
        for /f "tokens=2 delims=;=" %%a in ('echo %CONN_STRING% ^| findstr /i "Server"') do (
            echo Servidor: %%a
        )
        
        for /f "tokens=2 delims=;=" %%a in ('echo %CONN_STRING% ^| findstr /i "Database"') do (
            echo Base de Datos: %%a
        )
        
        for /f "tokens=2 delims=;=" %%a in ('echo %CONN_STRING% ^| findstr /i "User"') do (
            echo Usuario: %%a
        )
    ) else (
        echo [ADVERTENCIA] No se pudo leer el connection string
    )
) else (
    echo [ERROR] appsettings.json NO encontrado
    echo.
    echo La aplicaci�n NO podr� conectarse a la base de datos
    echo.
    echo SOLUCION:
    echo 1. Verifique que existe: appsettings.json (en la ra�z del proyecto)
    echo 2. Verifique que tenga CopyToOutputDirectory=PreserveNewest en el .csproj
    echo 3. Ejecute nuevamente: Publicar_WinUI_Completo.bat
)

echo.
echo ================================================
echo 2. VERIFICANDO DLLs DE ENTITY FRAMEWORK
echo ================================================
echo.

set EF_OK=0

for %%F in (
    "Microsoft.EntityFrameworkCore.dll"
    "Microsoft.EntityFrameworkCore.SqlServer.dll"
    "Microsoft.EntityFrameworkCore.Relational.dll"
    "Microsoft.Data.SqlClient.dll"
) do (
    if exist "%PUBLISH_DIR%\%%~F" (
        echo [OK] %%~F
        set /a EF_OK+=1
    ) else (
        echo [FALTA] %%~F
    )
)

echo.
if %EF_OK% LSS 4 (
    echo [ADVERTENCIA] Faltan DLLs de Entity Framework
    echo               La conexi�n a la base de datos NO funcionar�
) else (
    echo [OK] Todos los componentes de Entity Framework presentes
)

echo.
echo ================================================
echo 3. VERIFICANDO LOGS DE LA APLICACION
echo ================================================
echo.

if exist "%PUBLISH_DIR%\app_error_log.txt" (
    echo Se encontr� log de la aplicaci�n:
    echo %PUBLISH_DIR%\app_error_log.txt
    echo.
    
    REM Buscar mensajes relacionados con base de datos
    echo Buscando mensajes de conexi�n a BD...
    echo ----------------------------------------
    findstr /i /c:"Connection" /c:"Database" /c:"SQL" "%PUBLISH_DIR%\app_error_log.txt" 2>nul
    echo ----------------------------------------
    echo.
    
    echo �ltimas 30 l�neas del log:
    echo ----------------------------------------
    powershell -Command "Get-Content '%PUBLISH_DIR%\app_error_log.txt' -Tail 30"
    echo ----------------------------------------
    echo.
    
    echo Para ver el log completo, abra:
    echo %PUBLISH_DIR%\app_error_log.txt
) else (
    echo No hay log a�n. Se crear� cuando ejecute la aplicaci�n.
    echo Ubicaci�n del log: %PUBLISH_DIR%\app_error_log.txt
)

echo.
echo ================================================
echo 4. PRUEBA DE CONECTIVIDAD (OPCIONAL)
echo ================================================
echo.
echo Desea probar la conexi�n al servidor SQL Server?
echo NOTA: Necesita tener instalado sqlcmd
echo.
choice /C SN /M "Probar conexion"

if errorlevel 2 goto :skip_test
if errorlevel 1 goto :test_connection

:test_connection
echo.
echo Extrayendo informaci�n de conexi�n...

REM Leer appsettings.json con PowerShell
for /f "tokens=*" %%a in ('powershell -Command "$json = Get-Content '%PUBLISH_DIR%\appsettings.json' | ConvertFrom-Json; $cs = $json.ConnectionStrings.DefaultConnection; if ($cs -match 'Server=([^;]+)') { $matches[1] }"') do set DB_SERVER=%%a

for /f "tokens=*" %%a in ('powershell -Command "$json = Get-Content '%PUBLISH_DIR%\appsettings.json' | ConvertFrom-Json; $cs = $json.ConnectionStrings.DefaultConnection; if ($cs -match 'Database=([^;]+)') { $matches[1] }"') do set DB_NAME=%%a

for /f "tokens=*" %%a in ('powershell -Command "$json = Get-Content '%PUBLISH_DIR%\appsettings.json' | ConvertFrom-Json; $cs = $json.ConnectionStrings.DefaultConnection; if ($cs -match 'User Id=([^;]+)') { $matches[1] }"') do set DB_USER=%%a

for /f "tokens=*" %%a in ('powershell -Command "$json = Get-Content '%PUBLISH_DIR%\appsettings.json' | ConvertFrom-Json; $cs = $json.ConnectionStrings.DefaultConnection; if ($cs -match 'Password=([^;]+)') { $matches[1] }"') do set DB_PASS=%%a

echo.
echo Servidor: %DB_SERVER%
echo Base de Datos: %DB_NAME%
echo Usuario: %DB_USER%
echo.

if defined DB_SERVER if defined DB_NAME (
    echo Intentando conectar...
    echo.
    
    REM Probar con sqlcmd
    sqlcmd -S "%DB_SERVER%" -d "%DB_NAME%" -U "%DB_USER%" -P "%DB_PASS%" -Q "SELECT @@VERSION" 2>nul
    
    if errorlevel 1 (
        echo.
        echo [ERROR] No se pudo conectar al servidor SQL
        echo.
        echo POSIBLES CAUSAS:
        echo 1. El servidor no esta accesible desde esta PC
        echo 2. Las credenciales son incorrectas
        echo 3. La base de datos no existe
        echo 4. SQL Server no acepta conexiones remotas
        echo 5. Firewall bloqueando el puerto (default: 1433)
        echo.
        echo SOLUCIONES:
        echo - Verifique que SQL Server este ejecutandose
        echo - Pruebe hacer ping al servidor: ping %DB_SERVER%
        echo - Verifique que el puerto este abierto: telnet %DB_SERVER% 1433
        echo - Revise la configuracion de SQL Server
        echo - Verifique usuario y contrase�a en appsettings.json
    ) else (
        echo.
        echo [OK] Conexi�n exitosa a SQL Server
    )
) else (
    echo [ADVERTENCIA] No se pudo extraer informaci�n del servidor
)

:skip_test

echo.
echo ================================================
echo RESUMEN
echo ================================================
echo.

if not exist "%PUBLISH_DIR%\appsettings.json" (
    echo [CRITICO] appsettings.json falta
    echo.
    echo SOLUCION INMEDIATA:
    echo 1. Copie manualmente appsettings.json a %PUBLISH_DIR%\
    echo 2. O ejecute nuevamente: Publicar_WinUI_Completo.bat
) else (
    if %EF_OK% GEQ 4 (
        echo [OK] Configuraci�n correcta
        echo.
        echo La aplicaci�n deber�a poder conectarse a la base de datos.
        echo.
        echo Si a�n no se conecta:
        echo 1. Revise el log: %PUBLISH_DIR%\app_error_log.txt
        echo 2. Verifique que el servidor SQL est� accesible
        echo 3. Verifique las credenciales en appsettings.json
        echo 4. Verifique que la base de datos exista
    ) else (
        echo [ADVERTENCIA] Faltan DLLs de Entity Framework
        echo.
        echo Ejecute nuevamente: Publicar_WinUI_Completo.bat
    )
)

echo.
pause
