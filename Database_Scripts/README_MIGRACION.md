# Migración de Base de Datos - Rama Femenina

## Descripción General

Este documento describe el proceso de migración de la base de datos para el sistema Rama Femenina, específicamente para el módulo de **Recibos** (ingresos y egresos).

## Estructura de la Base de Datos

El sistema utiliza SQL Server con las siguientes tablas principales:

1. **acceso** - Gestión de usuarios y autenticación
2. **Pacientes** - Información de pacientes
3. **Donaciones** - Registro de donaciones
4. **Cheques** - Gestión de cheques
5. **Clientes** - Información de clientes
6. **Recibo** - Registro de recibos de ingreso y egreso (NUEVO/ACTUALIZADO)

## Tabla Recibo - Estructura Completa

```sql
CREATE TABLE dbo.Recibo (
    NumeroRecibo INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TipoRecibo NVARCHAR(20) NOT NULL DEFAULT 'Ingreso',
    Fecha DATETIME NOT NULL,
    RecibimosDe NVARCHAR(200) NOT NULL,
    Cedula NVARCHAR(20) NULL,
    Monto DECIMAL(18,2) NOT NULL DEFAULT 0,
    MontoEnLetras NVARCHAR(500) NULL,
    Concepto NVARCHAR(500) NULL,
    EsEfectivo BIT NOT NULL DEFAULT 0,
    EsTransferencia BIT NOT NULL DEFAULT 0,
    EsCheque BIT NOT NULL DEFAULT 0,
    NumeroFacturaNCF NVARCHAR(100) NULL,
    NumeroCheque NVARCHAR(100) NULL,
    Banco NVARCHAR(100) NULL
);
```

### Campos de la Tabla Recibo

| Campo | Tipo | Descripción |
|-------|------|-------------|
| NumeroRecibo | INT IDENTITY | Clave primaria, autoincremental |
| TipoRecibo | NVARCHAR(20) | Tipo de recibo: "Ingreso" o "Egreso" |
| Fecha | DATETIME | Fecha del recibo |
| RecibimosDe | NVARCHAR(200) | Nombre de quien recibe/entrega |
| Cedula | NVARCHAR(20) | Cédula (para recibos de egreso) |
| Monto | DECIMAL(18,2) | Monto del recibo |
| MontoEnLetras | NVARCHAR(500) | Monto escrito en letras |
| Concepto | NVARCHAR(500) | Descripción del concepto |
| EsEfectivo | BIT | Indica si el pago es en efectivo |
| EsTransferencia | BIT | Indica si el pago es por transferencia |
| EsCheque | BIT | Indica si el pago es por cheque |
| NumeroFacturaNCF | NVARCHAR(100) | Número de factura o NCF |
| NumeroCheque | NVARCHAR(100) | Número de cheque (si aplica) |
| Banco | NVARCHAR(100) | Nombre del banco (si es cheque) |

## Scripts de Migración

### 1. Verificar la Base de Datos

Ejecuta el script de verificación para revisar el estado actual de la base de datos:

```bash
Database_Scripts/Verify_Database_Schema.sql
```

Este script:
- Verifica que todas las tablas existan
- Muestra la estructura de cada tabla
- Indica si faltan columnas en la tabla Recibo
- Proporciona un resumen del estado de la base de datos

### 2. Actualizar la Tabla Recibo

Si la tabla Recibo no existe o le faltan columnas, ejecuta:

```bash
Database_Scripts/Update_Recibo_Table.sql
```

Este script:
- Crea la tabla Recibo si no existe
- Agrega las columnas TipoRecibo, Cedula y MontoEnLetras si faltan
- Convierte NumeroRecibo a columna IDENTITY si no lo es
- Preserva los datos existentes durante la migración

### 3. Corregir Columnas IDENTITY (Si es necesario)

Si otras tablas tienen problemas con columnas IDENTITY, ejecuta:

```bash
Database_Scripts/Fix_Identity_Columns.sql
```

Este script corrige las columnas IDENTITY en:
- Donaciones.Iddonacion
- Cheques.idCheque
- Clientes.idCliente
- Recibo.NumeroRecibo

## Proceso de Migración Paso a Paso

### Paso 1: Backup de la Base de Datos

**IMPORTANTE:** Antes de ejecutar cualquier script, realiza un backup completo de tu base de datos.

```sql
BACKUP DATABASE Ramafemenina 
TO DISK = 'C:\Backups\Ramafemenina_Backup_[FECHA].bak'
WITH FORMAT, INIT, NAME = 'Ramafemenina Full Backup';
```

### Paso 2: Verificar el Estado Actual

1. Abre SQL Server Management Studio
2. Conéctate a tu servidor de base de datos
3. Ejecuta el script `Verify_Database_Schema.sql`
4. Revisa el output para identificar qué necesita ser actualizado

### Paso 3: Ejecutar Scripts de Migración

Basado en los resultados de la verificación:

1. Si la tabla Recibo no existe o está incompleta:
   ```sql
   -- Ejecutar en SQL Server Management Studio
   USE Ramafemenina;
   GO
   -- Copiar y ejecutar el contenido de Update_Recibo_Table.sql
   ```

2. Si hay problemas con columnas IDENTITY en otras tablas:
   ```sql
   -- Ejecutar Fix_Identity_Columns.sql
   ```

### Paso 4: Verificar la Migración

Después de ejecutar los scripts, vuelve a ejecutar `Verify_Database_Schema.sql` para confirmar que todo está correcto.

### Paso 5: Actualizar la Aplicación

La aplicación ya está configurada para usar la nueva estructura de la tabla Recibo. Asegúrate de que:

1. El archivo `appsettings.json` tiene la cadena de conexión correcta
2. El contexto de Entity Framework (`RamaFemeninaContext`) está actualizado
3. El modelo `Recibo.cs` coincide con la estructura de la base de datos

## Configuración de Entity Framework

El contexto de la base de datos está configurado en `Data/RamaFemeninaContext.cs` con el siguiente mapeo:

```csharp
modelBuilder.Entity<Recibo>(entity =>
{
    entity.ToTable("Recibo");
    entity.HasKey(e => e.NumeroRecibo);
    entity.Property(e => e.NumeroRecibo)
        .HasColumnName("NumeroRecibo")
        .ValueGeneratedOnAdd()
        .UseIdentityColumn();
    entity.Property(e => e.TipoRecibo).HasColumnName("TipoRecibo").IsRequired();
    entity.Property(e => e.Fecha).HasColumnName("Fecha").IsRequired();
    // ... más configuraciones
});
```

## Solución de Problemas

### Problema: "La tabla Recibo no existe"

**Solución:** Ejecuta `Update_Recibo_Table.sql` para crear la tabla.

### Problema: "Faltan columnas TipoRecibo o Cedula"

**Solución:** Ejecuta `Update_Recibo_Table.sql` que agregará las columnas faltantes.

### Problema: "Error al insertar registros - violación de identidad"

**Solución:** Ejecuta `Fix_Identity_Columns.sql` para corregir la configuración IDENTITY de las columnas.

### Problema: "Error de conexión a la base de datos"

**Solución:** 
1. Verifica la cadena de conexión en `appsettings.json`
2. Asegúrate de que el servidor SQL Server esté en ejecución
3. Verifica que el usuario tenga permisos en la base de datos

## Validación Post-Migración

Después de la migración, verifica que:

1. ? La tabla Recibo existe con todas las columnas
2. ? NumeroRecibo es una columna IDENTITY
3. ? Los datos existentes se migraron correctamente
4. ? La aplicación puede conectarse a la base de datos
5. ? Se pueden crear nuevos recibos sin errores
6. ? Se pueden editar y eliminar recibos existentes

## Datos de Prueba

Para probar la funcionalidad, puedes insertar algunos datos de ejemplo:

```sql
-- Insertar recibo de ingreso
INSERT INTO Recibo (TipoRecibo, Fecha, RecibimosDe, Cedula, Monto, MontoEnLetras, Concepto, EsEfectivo, EsTransferencia, EsCheque)
VALUES ('Ingreso', GETDATE(), 'Juan Pérez', NULL, 5000.00, 'Cinco mil pesos 00/100', 'Donación mensual', 1, 0, 0);

-- Insertar recibo de egreso
INSERT INTO Recibo (TipoRecibo, Fecha, RecibimosDe, Cedula, Monto, MontoEnLetras, Concepto, EsEfectivo, EsTransferencia, EsCheque)
VALUES ('Egreso', GETDATE(), 'María García', '001-1234567-8', 3500.00, 'Tres mil quinientos pesos 00/100', 'Pago de servicios', 0, 1, 0);

-- Verificar los datos insertados
SELECT * FROM Recibo;
```

## Contacto y Soporte

Para más información sobre la migración o problemas técnicos, contacta al equipo de desarrollo.

---

**Versión del documento:** 1.0  
**Última actualización:** [Fecha actual]  
**Autor:** Equipo de Desarrollo - Rama Femenina
