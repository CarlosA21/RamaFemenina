# ?? Guía de Publicación - RamaFemenina (WinUI 3)

## ?? PROCESO CORRECTO DE PUBLICACIÓN

Siga estos pasos **EN ORDEN** para publicar correctamente la aplicación WinUI 3:

### ?? Paso 0: Configurar Conexión a Base de Datos (PRIMERO)
```cmd
Edite: appsettings.json
```

**¿Por qué es importante?**
- Define a qué servidor SQL Server se conectará la aplicación
- Contiene usuario, contraseña y nombre de base de datos
- Se copia a la carpeta de compilación

**Guía completa:** Ver [CONFIGURAR-CONEXION-BD.md](CONFIGURAR-CONEXION-BD.md)

---

### ? Paso 1: Compilar en Release
```cmd
1_Compilar_Release.bat
```

**¿Qué hace?**
- Limpia carpetas bin y obj
- Restaura paquetes NuGet
- Compila en modo Release con Platform=x64
- **Genera resources.pri** (archivo crítico para WinUI)
- **Copia appsettings.json** a la carpeta de compilación

**Verificar:** Debe ver el mensaje `[OK] resources.pri generado correctamente`

---

### ? Paso 2: Publicar la Aplicación
```cmd
Publicar_WinUI_Completo.bat
```

**¿Qué hace?**
- Publica la aplicación con dotnet publish
- **Copia resources.pri** a la carpeta de publicación
- Copia DLLs de Windows App SDK
- Verifica que todos los archivos críticos estén presentes

**Resultado:** Carpeta `bin\publish-final` con todos los archivos necesarios

---

### ? Paso 3: Verificar la Publicación
```cmd
2_Verificar_Publicacion.bat
```

**¿Qué hace?**
- Verifica que resources.pri esté presente
- Verifica todas las DLLs de WinUI y Windows App SDK
- **Verifica que appsettings.json esté presente**
- Muestra un diagnóstico completo

**Si todo está OK:** La aplicación está lista para distribuir

---

### ?? Paso 4 (Opcional): Diagnosticar Conexión a BD
```cmd
3_Diagnostico_BaseDatos.bat
```

**¿Qué hace?**
- Verifica que appsettings.json esté presente y sea válido
- Muestra el connection string configurado (sin contraseña)
- Verifica DLLs de Entity Framework
- Muestra logs de la aplicación
- Prueba la conexión al servidor SQL (opcional)

**Cuándo usarlo:**
- Cuando la aplicación no se conecta a la base de datos
- Para verificar la configuración antes de distribuir
- Para diagnosticar problemas de conectividad

---

## ?? ERRORES COMUNES

### Error 1: "Cannot locate resource from themeresources.xaml"

**Causa:** Falta el archivo `resources.pri` en la carpeta de publicación.

**Solución:**
1. Ejecute `1_Compilar_Release.bat`
2. Verifique que se genere resources.pri
3. Ejecute `Publicar_WinUI_Completo.bat` nuevamente

**Detalles completos:** Ver [SOLUCION-ERROR-RESOURCES-PRI.md](SOLUCION-ERROR-RESOURCES-PRI.md)

---

### Error 2: "No se puede conectar a la base de datos"

**Causa:** El servidor SQL Server no es accesible o las credenciales son incorrectas.

**Solución:**
1. Edite `appsettings.json` con la configuración correcta
2. Ejecute `1_Compilar_Release.bat` para copiar el archivo
3. Ejecute `3_Diagnostico_BaseDatos.bat` para verificar
4. Revise el log: `bin\x64\Release\...\app_error_log.txt`

**Guía completa:** Ver [CONFIGURAR-CONEXION-BD.md](CONFIGURAR-CONEXION-BD.md)

---

## ?? ARCHIVOS CRÍTICOS NECESARIOS

La carpeta de compilación **DEBE** contener:

| Archivo | ¿Por qué es crítico? |
|---------|----------------------|
| `resources.pri` | Recursos de WinUI (temas, estilos, controles). Sin este, la app falla inmediatamente |
| `appsettings.json` | Configuración de conexión a base de datos. Sin este, la app no se conecta a SQL |
| `Microsoft.UI.Xaml.dll` | Framework de interfaz de WinUI 3 |
| `Microsoft.WindowsAppRuntime.dll` | Runtime de Windows App SDK |
| `Microsoft.EntityFrameworkCore.dll` | ORM para acceso a base de datos |
| `Microsoft.EntityFrameworkCore.SqlServer.dll` | Proveedor de SQL Server para Entity Framework |
| `Microsoft.Data.SqlClient.dll` | Cliente de SQL Server |

---

## ?? DISTRIBUCIÓN A OTRAS PCs

### Opción 1: Instalación Completa (Recomendado)

**En la PC destino:**

1. Instale Windows App SDK Runtime:
   ```
   https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
   ```

2. Copie la carpeta `bin\publish-final` completa

3. **Edite `appsettings.json`** con la configuración de conexión de la PC destino

4. Ejecute `RamaFemenina.exe`

### Opción 2: Portátil (Si todos los archivos están presentes)

Si el script de verificación muestra "PERFECTO":

1. Copie la carpeta `bin\publish-final` completa
2. **Edite `appsettings.json`** con la configuración correcta
3. Ejecute `RamaFemenina.exe` directamente
4. **NO necesita** instalación adicional de Windows App SDK

---

## ?? SOLUCIÓN DE PROBLEMAS

### Problema: resources.pri no se genera

**Solución:**

1. Abra la solución en Visual Studio
2. Menú: Build > Clean Solution
3. Seleccione: Configuration = **Release**, Platform = **x64**
4. Menú: Build > Rebuild Solution
5. Verifique manualmente: `bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\resources.pri`

### Problema: La aplicación falla después de copiar resources.pri

**Verificar:**

1. ¿Está instalado Windows App SDK Runtime?
   - Descarga: https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe

2. ¿Están todas las DLLs presentes?
   - Ejecute: `2_Verificar_Publicacion.bat`

3. ¿El archivo resources.pri tiene tamaño > 0?
   - Si es 0 bytes, recompile desde cero

### Problema: No se conecta a la base de datos

**Verificar:**

1. ¿Existe `appsettings.json` en la carpeta de la aplicación?
   - Ejecute: `2_Verificar_Publicacion.bat`

2. ¿El connection string es correcto?
   - Ejecute: `3_Diagnostico_BaseDatos.bat`
   - Ver: [CONFIGURAR-CONEXION-BD.md](CONFIGURAR-CONEXION-BD.md)

3. ¿El servidor SQL está accesible?
   - Pruebe: `ping DIRECCION_SERVIDOR`
   - Pruebe: `telnet DIRECCION_SERVIDOR 1433`

4. ¿Las credenciales son correctas?
   - Pruebe conectarse con SQL Server Management Studio

5. Revise el log de la aplicación:
   - `bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\app_error_log.txt`

### Problema: Error de compilación

**Verificar requisitos:**

- .NET SDK 8.0 instalado
- Windows App SDK 1.8+ instalado
- Visual Studio 2022 con workload de desarrollo de Windows

---

## ?? DOCUMENTOS DE REFERENCIA

- **[CONFIGURAR-CONEXION-BD.md](CONFIGURAR-CONEXION-BD.md)** - Cómo configurar la conexión a base de datos ?
- [SOLUCION-ERROR-RESOURCES-PRI.md](SOLUCION-ERROR-RESOURCES-PRI.md) - Solución detallada del error de resources.pri
- [PROBLEMA-PUBLISHTRIMMED-WINUI3.md](PROBLEMA-PUBLISHTRIMMED-WINUI3.md) - Por qué NO usar PublishTrimmed
- [PROBLEMA-DLLS-WINUI3.md](PROBLEMA-DLLS-WINUI3.md) - Problemas con DLLs faltantes

---

## ?? CARACTERÍSTICAS DE LA PUBLICACIÓN

### ? Incluido
- ? Self-contained (no requiere .NET Runtime instalado)
- ? Windows App SDK incluido (con WindowsAppSDKSelfContained=true)
- ? Todas las dependencias de Entity Framework
- ? Recursos de WinUI (resources.pri)
- ? Archivos de configuración (appsettings.json)

### ? NO incluido (incompatible con WinUI 3)
- ? PublishTrimmed - Rompe la reflexión
- ? PublishReadyToRun - Causa errores en modo unpackaged
- ? PublishSingleFile - No compatible con WinUI

---

## ?? SOPORTE

Si sigue teniendo problemas:

1. Ejecute `2_Verificar_Publicacion.bat` y anote los errores
2. Ejecute `3_Diagnostico_BaseDatos.bat` para problemas de conexión
3. Revise el log de la aplicación (si se genera)
4. Consulte la documentación relevante en la carpeta del proyecto

---

**Última actualización:** Noviembre 2024  
**Versión de Windows App SDK:** 1.8.251106002  
**Target Framework:** net8.0-windows10.0.19041.0
