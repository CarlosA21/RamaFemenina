# ?? Guía de Deployment - RamaFemenina

## ? **Publicación Completada con Éxito**

La aplicación ha sido publicada exitosamente. Los archivos están listos para distribución.

## ?? **Ubicación del Ejecutable**

El ejecutable y todos los archivos necesarios se encuentran en:

```
E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish\
```

### **Archivos Principales:**
- ? `RamaFemenina.exe` - Ejecutable principal
- ? `appsettings.json` - Configuración por defecto
- ? Todas las DLLs necesarias
- ? Assets y recursos

## ?? **Pasos para Distribución**

### **Opción 1: Distribución Simple (Carpeta Completa)**

1. **Navega a la carpeta de publicación:**
   ```
   cd "E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish"
   ```

2. **Copia toda la carpeta `publish`** a donde quieras distribuir

3. **Renombra la carpeta** (opcional):
   ```
   RamaFemenina_v1.0
   ```

4. **Comprime en ZIP** para facilitar la distribución:
   - Clic derecho en la carpeta
   - Enviar a ? Carpeta comprimida
   - Nombre sugerido: `RamaFemenina_v1.0_Setup.zip`

### **Opción 2: Instalador Profesional (Recomendado)**

#### **Usando Inno Setup (Gratuito):**

1. **Descargar Inno Setup:**
   - https://jrsoftware.org/isdl.php
   - Instalar

2. **Crear Script de Instalador:**

Guarda este contenido como `RamaFemenina_Setup.iss`:

```iss
[Setup]
AppName=Rama Femenina
AppVersion=1.0
DefaultDirName={pf}\RamaFemenina
DefaultGroupName=Rama Femenina
OutputDir=E:\SELLING PROJECTS\RAMA FEMENINA\Installer
OutputBaseFilename=RamaFemenina_Setup_v1.0
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin

[Files]
Source: "E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Rama Femenina"; Filename: "{app}\RamaFemenina.exe"
Name: "{commondesktop}\Rama Femenina"; Filename: "{app}\RamaFemenina.exe"

[Run]
Filename: "{app}\RamaFemenina.exe"; Description: "Ejecutar Rama Femenina"; Flags: nowait postinstall skipifsilent
```

3. **Compilar el instalador:**
   - Abrir Inno Setup
   - File ? Open ? Seleccionar `RamaFemenina_Setup.iss`
   - Build ? Compile
   - El instalador se creará en `E:\SELLING PROJECTS\RAMA FEMENINA\Installer\RamaFemenina_Setup_v1.0.exe`

## ?? **Requisitos del Sistema para el Cliente**

### **Requisitos Mínimos:**
- ? Windows 10 versión 1809 o superior
- ? 4 GB RAM
- ? 500 MB espacio en disco
- ? .NET 8 Runtime (incluido en la publicación)
- ? SQL Server 2019 o superior (o SQL Server Express)

### **Notas Importantes:**
- ?? La aplicación es **self-contained** (incluye .NET 8)
- ?? **NO requiere** instalar .NET por separado
- ?? **SÍ requiere** SQL Server instalado y configurado

## ?? **Configuración en el Cliente**

### **Primera Ejecución:**

1. **Copiar archivos** a la ubicación deseada
2. **Ejecutar** `RamaFemenina.exe`
3. **Hacer clic** en "?? Configurar BD"
4. **Completar datos:**
   - Servidor: `localhost` o `.\SQLEXPRESS`
   - Base de Datos: `RamaFemenina`
   - Tipo de autenticación: Windows o SQL Server
5. **Probar conexión**
6. **Guardar**
7. La aplicación se cerrará
8. **Ejecutar nuevamente** `RamaFemenina.exe`

### **Archivo de Configuración Generado:**

Se creará automáticamente: `dbconfig.json`

```json
{
  "Server": "localhost",
  "Database": "RamaFemenina",
  "UserId": "",
  "Password": "",
  "UseIntegratedSecurity": true,
  "TrustServerCertificate": true,
  "ConnectionTimeout": 30
}
```

## ?? **Script de Base de Datos**

**IMPORTANTE:** El cliente debe ejecutar el script de creación de la base de datos ANTES de usar la aplicación.

Incluir el archivo `Database_Setup.sql` en la distribución.

## ?? **Contenido de la Distribución**

### **Estructura Recomendada:**

```
RamaFemenina_v1.0/
??? RamaFemenina.exe ? (Ejecutable principal)
??? appsettings.json (Configuración por defecto)
??? Database_Setup.sql (Script de BD)
??? README.txt (Instrucciones)
??? [Todas las DLLs]
??? Assets/
    ??? [Archivos de recursos]
```

### **README.txt Sugerido:**

```text
===========================================
  RAMA FEMENINA - Sistema de Gestión
  Versión 1.0
===========================================

REQUISITOS:
- Windows 10 o superior
- SQL Server 2019 o SQL Server Express

INSTALACIÓN:

1. CONFIGURAR SQL SERVER:
   - Ejecutar el archivo "Database_Setup.sql" en SQL Server
   - Esto creará la base de datos "RamaFemenina"

2. EJECUTAR LA APLICACIÓN:
   - Doble clic en "RamaFemenina.exe"

3. PRIMERA VEZ:
   - Hacer clic en "?? Configurar BD"
   - Ingresar los datos del servidor SQL
   - Probar conexión
   - Guardar y reiniciar

CREDENCIALES POR DEFECTO:
- Usuario: admin
- Contraseña: admin123

?? IMPORTANTE: Cambiar la contraseña después del primer inicio

SOPORTE:
Para problemas técnicos, contactar a:
[Tu email o contacto]

© 2024 Rama Femenina. Todos los derechos reservados.
```

## ?? **Distribución Rápida (Para Entrega Hoy)**

### **Método Express:**

1. **Abrir carpeta de publicación en Explorador:**
   ```
   E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish
   ```

2. **Crear carpeta nueva:**
   ```
   RamaFemenina_v1.0_[FECHA]
   ```

3. **Copiar todo el contenido de `publish`** a la nueva carpeta

4. **Agregar:**
   - Script SQL de creación de BD
   - README.txt con instrucciones
   - Credenciales por defecto

5. **Comprimir en ZIP:**
   - Seleccionar la carpeta
   - Clic derecho ? Enviar a ? Carpeta comprimida
   - Nombrar: `RamaFemenina_v1.0_Setup.zip`

6. **¡LISTO PARA ENTREGAR!** ??

## ? **Checklist Pre-Entrega**

- [ ] Ejecutable funciona correctamente
- [ ] Archivo `appsettings.json` incluido
- [ ] Script SQL de base de datos incluido
- [ ] README.txt con instrucciones
- [ ] Archivo comprimido creado
- [ ] Probado en una máquina limpia (opcional pero recomendado)

## ?? **Verificación Rápida**

Antes de entregar, verifica:

1. **Ejecutar el .exe** desde la carpeta publish
2. **Configurar la BD** con datos de prueba
3. **Hacer login** con admin/admin123
4. **Verificar que todas las páginas cargan**
5. **Crear un registro de prueba** en cada módulo

## ?? **Tamaño del Paquete**

- Carpeta completa: ~150-200 MB
- Archivo ZIP: ~60-80 MB
- Instalador Inno Setup: ~50-70 MB

## ?? **Troubleshooting Cliente**

### **"La aplicación no inicia"**
- Verificar que SQL Server esté ejecutándose
- Ejecutar como Administrador
- Verificar Windows Defender no lo bloqueó

### **"No se puede conectar a la base de datos"**
- Verificar que SQL Server esté corriendo
- Configurar firewall si es servidor remoto
- Verificar credenciales en "Configurar BD"

### **"Error de permisos"**
- Ejecutar como Administrador
- Verificar permisos del usuario SQL
- Verificar permisos de carpeta

---

## ?? **¡Aplicación Lista para Entregar!**

**Ubicación del ejecutable:**
```
E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish\RamaFemenina.exe
```

**Para distribución inmediata:**
1. Abre la carpeta `publish`
2. Copia todo su contenido
3. Comprime en ZIP
4. ¡Entrega!

---

**Versión**: 1.0
**Fecha de Publicación**: 2024
**Plataforma**: Windows x64
**Framework**: .NET 8 (Self-contained)
