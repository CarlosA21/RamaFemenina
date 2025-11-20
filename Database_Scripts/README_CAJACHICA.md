# ?? Módulo de Caja Chica - Rama Femenina

## ?? Descripción

El módulo de **Desembolso de Caja Chica** permite gestionar todos los pagos y desembolsos menores realizados a través de la caja chica de la organización.

## ? Características Principales

### ?? Interfaz Moderna y Profesional
- ? Diseño limpio y moderno con WinUI 3
- ? Búsqueda en tiempo real mientras escribes
- ? Tabla responsive con scroll horizontal
- ? Estados visuales para montos (verde < $1000, naranja ? $1000)
- ? Panel de resumen con totales actualizados

### ?? Búsqueda Inteligente
La búsqueda funciona en tiempo real y busca en:
- Número de recibo
- Nombre de la persona a quien se pagó
- Concepto del desembolso
- Cuenta a la que se cargó
- Monto del desembolso

### ?? Gestión Completa (CRUD)
- ? **Crear** - Nuevo desembolso con validaciones
- ? **Leer** - Visualización de todos los desembolsos
- ? **Actualizar** - Editar desembolsos existentes
- ? **Eliminar** - Eliminar con confirmación

### ??? Impresión
- Impresión de desembolsos individuales
- Formato profesional

## ??? Estructura de la Base de Datos

### Tabla: CajaChica

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `idrecibo` | INT IDENTITY | ID autoincremental (clave primaria) |
| `recibo` | INT | Número de recibo físico |
| `fecha` | DATETIME | Fecha del desembolso |
| `nombre` | NVARCHAR(200) | Nombre de quien recibe el pago |
| `monto` | MONEY | Monto del desembolso |
| `cargoa` | NVARCHAR(200) | Cuenta o departamento al que se carga |
| `concepto` | NVARCHAR(500) | Descripción del desembolso |

## ?? Cómo Usar

### 1. Crear un Nuevo Desembolso

1. Haz clic en **"Nuevo Desembolso"** en la barra de herramientas
2. Completa el formulario:
   - **No. Recibo**: Número del recibo físico
   - **Fecha**: Fecha del desembolso
   - **Pagado a**: Nombre de quien recibe el pago
   - **La suma de RD$**: Monto a pagar
   - **Con cargo a**: Cuenta o departamento (opcional)
   - **Por concepto de**: Descripción detallada
3. Haz clic en **"Guardar"**

### 2. Buscar Desembolsos

- Escribe en la barra de búsqueda
- La tabla se filtrará automáticamente mientras escribes
- Búsqueda en todos los campos principales

### 3. Editar un Desembolso

1. Selecciona un desembolso de la lista
2. Haz clic en **"Editar"**
3. Modifica los campos necesarios
4. Haz clic en **"Actualizar"**

### 4. Eliminar un Desembolso

1. Selecciona un desembolso de la lista
2. Haz clic en **"Eliminar"**
3. Confirma la eliminación

### 5. Imprimir un Desembolso

1. Selecciona un desembolso de la lista
2. Haz clic en **"Imprimir"**
3. El documento se enviará a la impresora predeterminada

## ?? Panel de Resumen

El panel inferior muestra:
- **Total de desembolsos**: Cantidad de registros
- **Monto total**: Suma de todos los desembolsos mostrados

*Nota: El panel se actualiza automáticamente al filtrar*

## ?? Diseño de la Interfaz

### Esquema de Colores
- **Verde**: Montos menores a $1,000
- **Naranja/Rojo**: Montos de $1,000 o más

### Componentes Principales

1. **Header**
   - Título del módulo con emoji
   - Descripción breve

2. **Barra de Herramientas**
   - Nuevo Desembolso (siempre activo)
   - Editar (activo si hay selección)
   - Eliminar (activo si hay selección)
   - Imprimir (activo si hay selección)

3. **Barra de Búsqueda**
   - Filtro en tiempo real
   - Icono de búsqueda
   - Placeholder descriptivo

4. **Tabla de Datos**
   - Scroll horizontal y vertical
   - Columnas:
     - No. Recibo
     - Fecha
     - Pagado a
     - Monto (RD$)
     - Con cargo a
     - Por concepto de

5. **Estado Vacío**
   - Se muestra cuando no hay registros
   - Emoji grande
   - Mensaje informativo

6. **Panel de Resumen**
   - Total de desembolsos
   - Monto total acumulado

## ?? Configuración Técnica

### Archivos Principales

```
RamaFemenina/
??? Models/
?   ??? CajaChica.cs
??? Data/
?   ??? RamaFemeninaContext.cs (configuración de CajaChica)
??? CajaChicaPage.xaml
??? CajaChicaPage.xaml.cs
??? Database_Scripts/
    ??? Create_CajaChica_Table.sql
    ??? Update_CajaChica_Table.sql
```

### Modelo de Datos (C#)

```csharp
public class CajaChica
{
    public int IdRecibo { get; set; }          // ID autoincremental
    public int NumeroRecibo { get; set; }      // Número de recibo físico
    public DateTime Fecha { get; set; }        // Fecha del desembolso
    public string PagadoA { get; set; }        // Nombre del beneficiario
    public decimal Monto { get; set; }         // Monto del desembolso
    public string? ConCargoA { get; set; }     // Cuenta/departamento
    public string? Concepto { get; set; }      // Descripción
    
    // Propiedades computadas
    public string FechaFormateada { get; }     // dd/MM/yyyy
    public string MontoFormateado { get; }     // $0.00
    public SolidColorBrush MontoColor { get; } // Verde/Rojo
}
```

## ?? Instalación y Migración

### 1. Ejecutar Script de Migración

```sql
-- En SQL Server Management Studio
USE Ramafemenina;
GO

-- Ejecutar: Database_Scripts/Update_CajaChica_Table.sql
```

### 2. Verificar la Tabla

```sql
SELECT * FROM CajaChica;
```

### 3. Datos de Prueba

La tabla ya incluye 3 registros de ejemplo:
- Juan Pérez - $500.00
- María García - $1,200.00
- Carlos Rodríguez - $350.00

## ? Validaciones

El sistema valida:
- ? Número de recibo obligatorio y mayor a 0
- ? Fecha obligatoria
- ? Nombre del beneficiario obligatorio
- ? Monto obligatorio y mayor a 0
- ? Concepto obligatorio

## ?? Mejores Prácticas

1. **Números de Recibo**
   - Usa una secuencia consecutiva
   - Ejemplo: 1001, 1002, 1003...

2. **Conceptos**
   - Sé específico y detallado
   - Incluye números de factura si aplica

3. **Cargos**
   - Usa nombres consistentes para cuentas
   - Facilita los reportes posteriores

## ?? Solución de Problemas

### Error: "No se puede insertar NULL en idrecibo"
**Solución**: Ejecutar `Update_CajaChica_Table.sql` para hacer `idrecibo` IDENTITY

### La búsqueda no funciona
**Solución**: Verificar que el evento `TextChanged` esté conectado en el XAML

### Los montos no se muestran con colores
**Solución**: Verificar que la propiedad `MontoColor` esté implementada en el modelo

## ?? Capturas de Pantalla

*(Las capturas se mostrarán en la interfaz de la aplicación)*

### Vista Principal
- Lista completa de desembolsos
- Barra de búsqueda activa
- Panel de resumen

### Formulario de Nuevo Desembolso
- Campos organizados
- Validaciones en tiempo real
- Botones de acción

## ?? Futuras Mejoras

- [ ] Exportar a Excel
- [ ] Filtros por rango de fechas
- [ ] Filtros por monto
- [ ] Reportes mensuales/anuales
- [ ] Gráficas de gastos
- [ ] Categorización automática

---

**Versión**: 1.0  
**Última actualización**: 2024  
**Desarrollado para**: Rama Femenina
