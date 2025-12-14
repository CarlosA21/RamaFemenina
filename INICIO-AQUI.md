# ?? GUÍA COMPLETA - Compilación y Publicación de RamaFemenina (WinUI 3)

## ?? SCRIPTS DISPONIBLES

### ?? Scripts de Configuración

| Script | Descripción | Cuándo usarlo |
|--------|-------------|---------------|
| **0_Configurar_Conexion.bat** | Configurador interactivo de conexión a base de datos | Antes de compilar por primera vez o al cambiar de servidor SQL |
| **0_Limpieza_Profunda.bat** | Limpieza profunda de carpetas bin/obj y cache | Cuando hay errores de compilación persistentes |

### ??? Scripts de Compilación

| Script | Descripción | Cuándo usarlo |
|--------|-------------|---------------|
| **1_Compilar_Release.bat** | Compila el proyecto en Release y genera resources.pri | Primer paso antes de publicar |

### ?? Scripts de Publicación

| Script | Descripción | Cuándo usarlo |
|--------|-------------|---------------|
| **Publicar_WinUI_Completo.bat** | Publica la aplicación con todas las dependencias | Después de compilar con éxito |

### ?? Scripts de Diagnóstico

| Script | Descripción | Cuándo usarlo |
|--------|-------------|---------------|
| **2_Verificar_Publicacion.bat** | Verifica archivos críticos en la publicación | Después de publicar, antes de distribuir |
| **3_Diagnostico_BaseDatos.bat** | Diagnostica problemas de conexión a BD | Cuando la app no se conecta a SQL Server |

---

## ?? FLUJO DE TRABAJO COMPLETO

### Para Compilar y Ejecutar LOCALMENTE

```
1. 0_Configurar_Conexion.bat    (Opcional, solo si necesita cambiar la BD)
2. 1_Compilar_Release.bat       (Compila y genera resources.pri)
3. 3_Diagnostico_BaseDatos.bat  (Verifica configuración de BD)
4. Ejecutar: bin\x64\Release\...\RamaFemenina.exe
```

### Para PUBLICAR y DISTRIBUIR

```
1. 0_Configurar_Conexion.bat    (Configure la BD del cliente)
2. 1_Compilar_Release.bat       (Compila en Release)
3. Publicar_WinUI_Completo.bat  (Publica con todas las DLLs)
4. 2_Verificar_Publicacion.bat  (Verifica que todo esté OK)
5. Distribuir carpeta: bin\publish-final\
```

### Si HAY ERRORES de COMPILACIÓN

```
1. 0_Limpieza_Profunda.bat     (Limpia bin/obj y cache)
2. 1_Compilar_Release.bat      (Intenta compilar nuevamente)

Si persiste:
3. Ver: ERROR-XAML-COMPILER.md (Guía de solución de problemas)
```

---

## ?? DOCUMENTACIÓN COMPLETA

### ?? Solución de Errores

| Documento | Descripción |
|-----------|-------------|
| [ERROR-XAML-COMPILER.md](ERROR-XAML-COMPILER.md) | ? Error de compilación XAML (MSB3073) |
| [SOLUCION-ERROR-RESOURCES-PRI.md](SOLUCION-ERROR-RESOURCES-PRI.md) | Error "Cannot locate resource from themeresources.xaml" |
| [CONFIGURAR-CONEXION-BD.md](CONFIGURAR-CONEXION-BD.md) | ? Cómo configurar la conexión a SQL Server |

### ?? Guías de Referencia

| Documento | Descripción |
|-----------|-------------|
| [README-PUBLICACION.md](README-PUBLICACION.md) | Guía completa de publicación |
| [PROBLEMA-PUBLISHTRIMMED-WINUI3.md](PROBLEMA-PUBLISHTRIMMED-WINUI3.md) | Por qué NO usar PublishTrimmed |
| [PROBLEMA-DLLS-WINUI3.md](PROBLEMA-DLLS-WINUI3.md) | Problemas con DLLs faltantes |

---

## ?? SOLUCIÓN RÁPIDA DE PROBLEMAS

### ? Error: "The command XamlCompiler.exe exited with code 1"

**Solución rápida:**
```cmd
0_Limpieza_Profunda.bat
1_Compilar_Release.bat
```

**Guía completa:** [ERROR-XAML-COMPILER.md](ERROR-XAML-COMPILER.md)

---

### ? Error: "Cannot locate resource from themeresources.xaml"

**Solución rápida:**
```cmd
1_Compilar_Release.bat
```
Verifique que se genere `resources.pri`

**Guía completa:** [SOLUCION-ERROR-RESOURCES-PRI.md](SOLUCION-ERROR-RESOURCES-PRI.md)

---

### ? Error: "No se puede conectar a la base de datos"

**Solución rápida:**
```cmd
0_Configurar_Conexion.bat    (Configurar servidor SQL)
1_Compilar_Release.bat       (Recompilar)
3_Diagnostico_BaseDatos.bat  (Verificar)
```

**Guía completa:** [CONFIGURAR-CONEXION-BD.md](CONFIGURAR-CONEXION-BD.md)

---

### ? Error: Faltan DLLs de WinUI o Windows App SDK

**Solución:**
1. Instale Windows App SDK Runtime:
   ```
   https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
   ```

2. Recompile:
   ```cmd
   1_Compilar_Release.bat
   ```

---

## ?? CONCEPTOS CLAVE

### ¿Qué es resources.pri?
Es el archivo de recursos empaquetados de WinUI 3. Contiene todos los temas, estilos y recursos XAML. **Sin este archivo, la aplicación NO funciona.**

### ¿Por qué compilar antes de publicar?
La compilación en Release genera `resources.pri` y otros archivos necesarios que la publicación copia.

### ¿Dónde está el ejecutable compilado?
```
bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\RamaFemenina.exe
```

### ¿Dónde está el ejecutable publicado?
```
bin\publish-final\RamaFemenina.exe
```

### ¿Cuál es la diferencia?
- **Compilado:** Para desarrollo y pruebas locales
- **Publicado:** Para distribuir a otros equipos (incluye todas las dependencias)

---

## ?? CONFIGURACIÓN NECESARIA

### Requisitos del Sistema

- **Sistema Operativo:** Windows 10 versión 1809 o superior
- **.NET SDK:** 8.0 o superior
- **Visual Studio:** 2022 (recomendado) con workload de desarrollo de Windows
- **Windows App SDK:** 1.8 o superior
- **SQL Server:** Cualquier versión (Express, Standard, Developer)

### Variables de Entorno (Opcional)

Si usa rutas personalizadas, configure:
```
DOTNET_CLI_HOME
NUGET_PACKAGES
```

---

## ?? ESTRUCTURA DE CARPETAS

```
RamaFemenina/
??? 0_Configurar_Conexion.bat         ? Configurar BD
??? 0_Limpieza_Profunda.bat           ? Limpiar proyecto
??? 1_Compilar_Release.bat            ? Compilar
??? Publicar_WinUI_Completo.bat       ? Publicar
??? 2_Verificar_Publicacion.bat       ? Verificar
??? 3_Diagnostico_BaseDatos.bat       ? Diagnosticar BD
??? appsettings.json                  ? Configuración de BD
??? RamaFemenina.csproj               ? Archivo del proyecto
??? App.xaml.cs                       ? Código de inicio
??? bin/
?   ??? x64/Release/.../              ? Compilado
?   ??? publish-final/                ? Publicado
??? obj/                              ? Archivos intermedios
??? Documentación/
    ??? README-PUBLICACION.md
    ??? ERROR-XAML-COMPILER.md
    ??? CONFIGURAR-CONEXION-BD.md
    ??? ...
```

---

## ?? SEGURIDAD

### Archivos sensibles (NO subir a Git)

- `appsettings.json` (contiene contraseñas)
- `appsettings.json.backup`
- `bin/` y `obj/` (archivos de compilación)
- `*.user` (configuración personal de Visual Studio)

### Archivos a incluir en Git

- Scripts .bat
- Documentación .md
- `appsettings.json.example` (sin contraseñas)
- Código fuente (.cs, .xaml)
- `RamaFemenina.csproj`

---

## ?? SOPORTE Y RECURSOS

### Documentación oficial

- [WinUI 3 Documentation](https://learn.microsoft.com/windows/apps/winui/winui3/)
- [Windows App SDK](https://learn.microsoft.com/windows/apps/windows-app-sdk/)
- [.NET 8 Documentation](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8)

### Problemas comunes

- [Windows App SDK GitHub Issues](https://github.com/microsoft/WindowsAppSDK/issues)
- [WinUI GitHub Issues](https://github.com/microsoft/microsoft-ui-xaml/issues)

---

## ?? CHECKLIST DE DISTRIBUCIÓN

Antes de distribuir la aplicación a un cliente:

- [ ] Configurar `appsettings.json` con los datos del cliente
- [ ] Compilar: `1_Compilar_Release.bat`
- [ ] Verificar que no hay errores
- [ ] Publicar: `Publicar_WinUI_Completo.bat`
- [ ] Verificar: `2_Verificar_Publicacion.bat`
- [ ] Probar el ejecutable localmente
- [ ] Verificar conexión a BD: `3_Diagnostico_BaseDatos.bat`
- [ ] Crear ZIP de la carpeta `bin\publish-final`
- [ ] Documentar configuración específica del cliente
- [ ] Incluir instalador de Windows App SDK Runtime

---

**Última actualización:** Noviembre 2024  
**Versión:** 1.0  
**Target Framework:** .NET 8 / Windows App SDK 1.8  
**Autor:** Proyecto RamaFemenina
