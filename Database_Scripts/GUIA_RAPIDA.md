# GUÍA RÁPIDA DE MIGRACIÓN - MÓDULO DE RECIBOS

## ?? Resumen

Esta migración actualiza la base de datos para soportar completamente el módulo de **Recibos** con las siguientes características:

- ? Recibos de Ingreso y Egreso
- ? Múltiples tipos de pago (Efectivo, Transferencia, Cheque)
- ? Campos adicionales: Cédula, Monto en letras, NCF
- ? Numeración automática de recibos

## ?? Pasos para Migrar (3 MINUTOS)

### 1?? BACKUP (OBLIGATORIO)

Antes de hacer cualquier cambio, **SIEMPRE** haz un backup de tu base de datos:

```sql
BACKUP DATABASE Ramafemenina 
TO DISK = 'C:\Backups\Ramafemenina_Backup.bak'
WITH FORMAT, INIT;
```

### 2?? VERIFICAR ESTADO ACTUAL

Ejecuta este script para ver qué necesitas actualizar:

```
?? Database_Scripts/Verify_Database_Schema.sql
```

### 3?? EJECUTAR MIGRACIÓN COMPLETA

Ejecuta el script principal de migración:

```
?? Database_Scripts/Complete_Migration.sql
```

Este script:
- ? Crea la tabla Recibo si no existe
- ? Agrega columnas faltantes (TipoRecibo, Cedula, MontoEnLetras)
- ? Convierte NumeroRecibo a columna IDENTITY
- ? Preserva todos los datos existentes
- ? Verifica el resultado final

### 4?? VERIFICAR LA APLICACIÓN

1. Abre Visual Studio
2. Verifica la cadena de conexión en `appsettings.json`
3. Compila y ejecuta el proyecto (F5)
4. Navega al módulo de Recibos
5. Prueba crear un nuevo recibo

## ?? Estructura de la Nueva Tabla Recibo

| Campo | Tipo | Descripción |
|-------|------|-------------|
| NumeroRecibo | INT IDENTITY | ID autoincremental |
| **TipoRecibo** | NVARCHAR(20) | "Ingreso" o "Egreso" ? NUEVO |
| Fecha | DATETIME | Fecha del recibo |
| RecibimosDe | NVARCHAR(200) | Nombre de quien recibe/entrega |
| **Cedula** | NVARCHAR(20) | Cédula (para egresos) ? NUEVO |
| Monto | DECIMAL(18,2) | Monto del recibo |
| **MontoEnLetras** | NVARCHAR(500) | Monto en letras ? NUEVO |
| Concepto | NVARCHAR(500) | Descripción |
| EsEfectivo | BIT | Pago en efectivo |
| EsTransferencia | BIT | Pago por transferencia |
| EsCheque | BIT | Pago por cheque |
| NumeroFacturaNCF | NVARCHAR(100) | Número de factura/NCF |
| NumeroCheque | NVARCHAR(100) | Número de cheque |
| Banco | NVARCHAR(100) | Banco del cheque |

## ?? Verificación Post-Migración

Ejecuta estas consultas para verificar:

```sql
-- Ver estructura de la tabla
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.Recibo')
ORDER BY c.column_id;

-- Verificar que NumeroRecibo es IDENTITY
SELECT is_identity 
FROM sys.columns 
WHERE object_id = OBJECT_ID('dbo.Recibo') 
AND name = 'NumeroRecibo';
-- Debe retornar 1

-- Ver recibos existentes
SELECT * FROM Recibo;
```

## ?? Datos de Prueba

Inserta algunos recibos de prueba:

```sql
-- Recibo de Ingreso
INSERT INTO Recibo (TipoRecibo, Fecha, RecibimosDe, Monto, MontoEnLetras, Concepto, EsEfectivo)
VALUES ('Ingreso', GETDATE(), 'Juan Pérez', 5000.00, 'Cinco mil pesos 00/100', 'Donación mensual', 1);

-- Recibo de Egreso
INSERT INTO Recibo (TipoRecibo, Fecha, RecibimosDe, Cedula, Monto, MontoEnLetras, Concepto, EsTransferencia, NumeroFacturaNCF)
VALUES ('Egreso', GETDATE(), 'María García', '001-1234567-8', 3500.00, 'Tres mil quinientos pesos 00/100', 'Pago de servicios', 1, 'B0100000001');

-- Ver los recibos insertados
SELECT * FROM Recibo;
```

## ? Solución de Problemas

### Error: "La tabla Recibo no existe"
? **Solución:** Ejecuta `Complete_Migration.sql`

### Error: "Invalid column name 'TipoRecibo'"
? **Solución:** Ejecuta `Update_Recibo_Table.sql` o `Complete_Migration.sql`

### Error: "Cannot insert explicit value for identity column"
? **Solución:** Ejecuta `Complete_Migration.sql` para convertir NumeroRecibo a IDENTITY

### Error de conexión a la base de datos
? **Solución:** Verifica `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=Ramafemenina;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

## ?? Archivos del Proyecto

```
Database_Scripts/
??? ? Complete_Migration.sql          (Ejecuta este primero)
??? ? Update_Recibo_Table.sql         (Alternativa específica para Recibo)
??? ? Fix_Identity_Columns.sql        (Si hay problemas con IDENTITY)
??? ? Verify_Database_Schema.sql      (Para verificar el estado)
??? ? README_MIGRACION.md             (Documentación completa)
```

## ?? ¿Necesitas Ayuda?

1. Revisa el archivo `README_MIGRACION.md` para documentación detallada
2. Verifica los logs de error en Visual Studio (Output window)
3. Revisa los mensajes de SQL Server al ejecutar los scripts

## ? Después de la Migración

La aplicación ahora soporta:

- ?? Crear recibos de ingreso y egreso
- ?? Múltiples formas de pago
- ??? Imprimir recibos con configuración personalizable
- ?? Generar reportes de recibos
- ?? Búsqueda y filtrado avanzado
- ?? Editar y eliminar recibos existentes

¡La migración está completa! ??

---

**Última actualización:** 2024  
**Versión:** 1.0
