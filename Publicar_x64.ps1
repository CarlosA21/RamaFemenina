# ============================================
# PUBLICAR SIN VISUAL STUDIO - RamaFemenina
# ============================================

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "PUBLICAR x64 - FORZANDO PLATAFORMA" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Ir al directorio del script
Set-Location $PSScriptRoot

Write-Host "??  IMPORTANTE: Cierre Visual Studio antes de continuar" -ForegroundColor Yellow
Write-Host ""
Read-Host "Presione Enter para continuar"

# Buscar archivo del proyecto
$projectFile = "RamaFemenina.csproj"
if (-not (Test-Path $projectFile)) {
    Write-Host ""
    Write-Host "? ERROR: No se encuentra $projectFile" -ForegroundColor Red
    Write-Host "Asegúrese de ejecutar este script desde la carpeta del proyecto" -ForegroundColor Yellow
    Read-Host "Presione Enter para salir"
    exit 1
}

Write-Host "? Encontrado: $projectFile" -ForegroundColor Green

# 1. Limpiar
Write-Host ""
Write-Host "[1/5] Limpiando compilaciones anteriores..." -ForegroundColor Green
if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force -ErrorAction SilentlyContinue }
Write-Host "? Carpetas bin y obj eliminadas" -ForegroundColor Green

# 2. Restaurar
Write-Host ""
Write-Host "[2/5] Restaurando paquetes..." -ForegroundColor Green
dotnet restore $projectFile --runtime win-x64
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "? ERROR al restaurar paquetes" -ForegroundColor Red
    Read-Host "Presione Enter para salir"
    exit 1
}

# 3. Build
Write-Host ""
Write-Host "[3/5] Compilando para x64..." -ForegroundColor Green
dotnet build $projectFile `
    --configuration Release `
    --runtime win-x64 `
    --no-restore `
    /p:Platform=x64 `
    /p:PlatformTarget=x64 `
    /p:RuntimeIdentifier=win-x64

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "? ERROR al compilar" -ForegroundColor Red
    Write-Host ""
    Write-Host "SOLUCIÓN:" -ForegroundColor Yellow
    Write-Host "1. Abra Visual Studio" -ForegroundColor White
    Write-Host "2. Cambie la plataforma de ARM64 a x64 en la barra superior" -ForegroundColor White
    Write-Host "3. Cierre Visual Studio completamente" -ForegroundColor White
    Write-Host "4. Ejecute este script de nuevo" -ForegroundColor White
    Write-Host ""
    Read-Host "Presione Enter para salir"
    exit 1
}

# 4. Publish
Write-Host ""
Write-Host "[4/5] Publicando (esto puede tardar 2-5 minutos)..." -ForegroundColor Green
Write-Host ""

dotnet publish $projectFile `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --no-build `
    /p:Platform=x64 `
    /p:PlatformTarget=x64 `
    /p:RuntimeIdentifier=win-x64 `
    /p:PublishSingleFile=false `
    /p:PublishReadyToRun=false `
    /p:PublishTrimmed=false

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "? ERROR al publicar" -ForegroundColor Red
    Read-Host "Presione Enter para salir"
    exit 1
}

# 5. Éxito
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "? ÉXITO - Publicación completada" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "?? Ubicación del ejecutable:" -ForegroundColor Cyan
Write-Host "   bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\RamaFemenina.exe" -ForegroundColor White
Write-Host ""
Write-Host "?? Próximos pasos:" -ForegroundColor Cyan
Write-Host "   1. Ejecute: DiagnosticarProblema.bat" -ForegroundColor White
Write-Host "   2. Si todo está OK, ejecute: Ejecutar_Con_Log.bat" -ForegroundColor White
Write-Host ""

Read-Host "Presione Enter para salir"
