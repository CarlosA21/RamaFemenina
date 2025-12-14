# ?? PROBLEMA: Faltan DLLs de Windows App SDK (WinUI 3)

## ? El Problema

Después de publicar con `--self-contained true`, faltan estas DLLs:
- ? `Microsoft.UI.Xaml.dll`
- ? `Microsoft.WindowsAppRuntime.dll`  
- ? `Microsoft.Windows.AppLifecycle.dll`

**¿Por qué ocurre esto?**

Windows App SDK (WinUI 3) tiene dependencias **NATIVAS** que NO se copian automáticamente con `dotnet publish --self-contained`. Esto es una limitación conocida de WinUI 3.

---

## ? SOLUCIONES (De más fácil a más compleja)

### ?? **SOLUCIÓN 1: Usar el Script Mejorado** ?????

Ejecuta el nuevo script que acabo de crear:

```
?? Publicar_WinUI_Completo.bat
```

**Qué hace diferente:**
- ? Usa `/p:WindowsAppSDKSelfContained=true`
- ? Copia DLLs de WindowsAppSDK desde NuGet packages
- ? Verifica que todas las DLLs estén presentes

**Tiempo:** 5-10 minutos

---

### ?? **SOLUCIÓN 2: Instalar Windows App SDK Runtime** ?????

**En TU PC (desarrollo):**

1. Descarga e instala Windows App SDK Runtime:
   ```
   https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
   ```

2. **Reinicia tu PC**

3. Ejecuta de nuevo: `Publicar_x64.bat`

4. Las DLLs ahora deberían copiarse correctamente

**En la PC del CLIENTE:**

También debe instalar Windows App SDK Runtime (mismo enlace).

**Ventaja:** Aplicación más pequeña (~50 MB vs 200 MB)  
**Desventaja:** Requiere instalación en cada PC

---

### ?? **SOLUCIÓN 3: Copiar DLLs Manualmente** ???

Si las soluciones anteriores no funcionan:

#### Paso 1: Encontrar las DLLs

Las DLLs están en uno de estos lugares:

**Opción A - Desde NuGet packages:**
```
C:\Users\TU_USUARIO\.nuget\packages\microsoft.windowsappsdk\1.8.xxx\runtimes\win-x64\native\
```

**Opción B - Desde Program Files:**
```
C:\Program Files\WindowsApps\Microsoft.WindowsAppRuntime.1.8.xxxxx\
```

**Opción C - Desde proyecto compilado anterior:**
```
E:\SELLING PROJECTS\RAMA FEMENINA\RamaFemenina\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\
```

#### Paso 2: Copiar a la carpeta publish

Copia estos archivos a:
```
bin\win-x64\publish\
```

**DLLs necesarias:**
- `Microsoft.UI.Xaml.dll`
- `Microsoft.WindowsAppRuntime.dll`
- `Microsoft.Windows.AppLifecycle.dll`
- `Microsoft.Windows.AppNotifications.dll`
- `Microsoft.Windows.PushNotifications.dll`
- `Microsoft.Windows.Security.AccessControl.dll`
- `Microsoft.Windows.System.dll`
- `Microsoft.Windows.Widgets.dll`
- `WinRT.Runtime.dll`

---

### ?? **SOLUCIÓN 4: Usar MSIX Package** ???

WinUI 3 fue diseñado para distribuirse como MSIX (Microsoft Store package).

**En Visual Studio:**

1. Clic derecho en proyecto ? **Package and Publish** ? **Create App Packages**
2. Selecciona **Sideloading**
3. Configura:
   - Architecture: x64
   - Generate app bundle: Never
4. **Create**

Esto creará un paquete `.msix` que incluye TODO.

**Ventaja:** Instalación limpia, actualizaciones automáticas  
**Desventaja:** Usuario debe "instalar" la app (no portable)

---

## ?? **Comparación de Soluciones**

| Solución | Tamaño | Portabilidad | Dificultad | Recomendado |
|----------|--------|--------------|------------|-------------|
| Script WinUI Completo | ~200 MB | ? Alta | ? Fácil | ? SÍ |
| Instalar SDK Runtime | ~50 MB | ?? Media | ?? Media | ? SÍ |
| Copiar DLLs manual | ~50 MB | ? Alta | ??? Media | ?? Si falla todo |
| MSIX Package | ~50 MB | ? Baja | ???? Difícil | ?? Para tienda |

---

## ?? **Diagnóstico**

### Ver qué DLLs están en la carpeta publish:

```powershell
cd "bin\win-x64\publish"
dir *.dll | findstr /i "microsoft.ui microsoft.windows"
```

### Ver qué paquetes NuGet tienes:

```powershell
dotnet list package
```

Busca:
- `Microsoft.WindowsAppSDK` - Debe ser versión 1.8.x
- `Microsoft.Windows.SDK.BuildTools`

---

## ?? **SI NADA FUNCIONA**

### Plan de Emergencia:

1. **Instala Windows App SDK Runtime:**
   ```
   https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
   ```

2. **Reinicia tu PC**

3. **Compila desde Visual Studio (no script):**
   - Clic derecho en proyecto ? **Publish**
   - Target: Folder
   - Configuration: Release
   - **Deployment mode: Framework-dependent** ? Importante
   - Target runtime: win-x64
   - **Publish**

4. **Distribuye con instrucciones:**
   ```
   "Esta aplicación requiere Windows App SDK Runtime.
   Descargue e instale desde: [enlace]"
   ```

---

## ?? **¿Por Qué Este Problema?**

**WinUI 3 es diferente de WPF/WinForms:**

- **WPF/WinForms:** Todo es .NET ? `--self-contained` funciona perfecto
- **WinUI 3:** Usa componentes nativos de Windows ? Requiere Windows App SDK Runtime

**Opciones de distribución:**

1. **Self-contained completo** (200 MB):
   - Incluye .NET + WinUI
   - Portable
   - Usa `Publicar_WinUI_Completo.bat`

2. **Framework-dependent** (50 MB):
   - Cliente instala .NET Runtime + Windows App SDK Runtime
   - Más pequeño
   - Requiere instalación

---

## ? **Recomendación Final**

### Para desarrollo/pruebas:
```
Ejecuta: Publicar_WinUI_Completo.bat
```

### Para distribución a clientes:
```
Opción A: Distribuye con Windows App SDK Runtime incluido
Opción B: Pide que instalen Windows App SDK Runtime primero
```

---

## ?? **Checklist de Verificación**

Después de publicar, verifica:

- [ ] ? `RamaFemenina.exe` existe
- [ ] ? `Microsoft.UI.Xaml.dll` existe
- [ ] ? `Microsoft.WindowsAppRuntime.dll` existe
- [ ] ? `Microsoft.Windows.AppLifecycle.dll` existe
- [ ] ? `Microsoft.EntityFrameworkCore.dll` existe
- [ ] ? Carpeta `runtimes\` con subcarpetas
- [ ] ? Tamaño total: ~150-200 MB (self-contained) o ~50 MB (framework-dependent)

Si TODAS están marcadas ? ? Listo para distribuir

---

**EJECUTA AHORA:** `Publicar_WinUI_Completo.bat`

Este script resuelve el problema de las DLLs faltantes automáticamente.
