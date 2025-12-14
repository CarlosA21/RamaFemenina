@echo off
echo ================================================
echo PUBLICAR PROYECTO - RamaFemenina (x64)
echo ================================================
echo.

cd /d "%~dp0"

echo [1] Limpiando proyecto anterior...
dotnet clean --configuration Release
echo.

echo [2] Restaurando paquetes NuGet...
dotnet restore
echo.

echo [3] Compilando para x64...
echo.

dotnet build -c Release /p:Platform=x64

if errorlevel 1 (
    echo.
    echo ERROR: La compilacion fallo
    echo.
    pause
    exit /b 1
)

echo.
echo [4] Publicando proyecto (Self-contained x64)...
echo.
echo Esto puede tardar varios minutos, por favor espere...
echo.

dotnet publish -c Release -r win-x64 /p:Platform=x64 --self-contained true /p:PublishSingleFile=false /p:PublishReadyToRun=true /p:PublishTrimmed=false

if errorlevel 1 (
    echo.
    echo ERROR: La publicacion fallo
    echo.
    echo Revise los mensajes de error arriba.
    echo.
    pause
    exit /b 1
)

echo.
echo ================================================
echo PUBLICACION COMPLETADA
echo ================================================
echo.
echo El ejecutable esta en:
echo bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\
echo.
echo Ahora ejecute: DiagnosticarProblema.bat
echo.
pause
