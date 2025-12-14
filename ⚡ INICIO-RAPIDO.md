# ? INICIO RÁPIDO - RamaFemenina

## ?? SI SOLO QUIERES COMPILAR Y EJECUTAR

```cmd
1_Compilar_Release.bat
```

Luego ejecuta: `bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\RamaFemenina.exe`

---

## ?? SI QUIERES PUBLICAR PARA DISTRIBUIR

```cmd
1. 1_Compilar_Release.bat
2. Publicar_WinUI_Completo.bat
3. 2_Verificar_Publicacion.bat
```

Distribuye la carpeta: `bin\publish-final\`

---

## ?? SI HAY UN ERROR DE COMPILACIÓN

```cmd
1. 0_Limpieza_Profunda.bat
2. 1_Compilar_Release.bat
```

Si persiste, ve a: `ERROR-XAML-COMPILER.md`

---

## ??? SI NO SE CONECTA A LA BASE DE DATOS

```cmd
1. 0_Configurar_Conexion.bat
2. 1_Compilar_Release.bat
3. 3_Diagnostico_BaseDatos.bat
```

Más detalles en: `CONFIGURAR-CONEXION-BD.md`

---

## ? SI LA APP NO INICIA (Error de recursos)

**Error:** "Cannot locate resource from themeresources.xaml"

**Solución:**
```cmd
1_Compilar_Release.bat
```

Verifica que se genere: `resources.pri`

Más info en: `SOLUCION-ERROR-RESOURCES-PRI.md`

---

## ?? DOCUMENTACIÓN COMPLETA

- **INICIO-AQUI.md** - Índice de todos los scripts y documentos
- **RESUMEN-SOLUCION-COMPLETA.md** - Resumen de todo lo implementado
- **README-PUBLICACION.md** - Guía detallada de publicación

---

## ?? SOPORTE

1. Revisa el log: `bin\x64\Release\...\app_error_log.txt`
2. Ejecuta: `2_Verificar_Publicacion.bat` (después de compilar)
3. Ejecuta: `3_Diagnostico_BaseDatos.bat` (problemas de BD)
4. Consulta la documentación en la carpeta del proyecto

---

**Última actualización:** Noviembre 2024  
**Versión:** 1.0
