# Página de Recibos - RamaFemenina

## Características Implementadas

### Interfaz Moderna
- ? Diseño consistente con el resto de la aplicación
- ? Estilo WinUI 3 con tema oscuro/claro
- ? Diseño responsivo y profesional

### Funcionalidades CRUD
- ? **Crear** nuevos recibos de ingreso
- ? **Editar** recibos existentes
- ? **Eliminar** recibos con confirmación
- ? **Visualizar** lista completa de recibos

### Características de Búsqueda y Filtrado
- ? Búsqueda en tiempo real por:
  - Número de recibo
  - Nombre del beneficiario
  - Concepto del pago
  - Número de cheque
  - Monto
- ? Filtrado por tipo de pago:
  - Efectivo
  - Transferencia
  - Cheque
- ? Filtrado por fecha (estructura lista para implementación)

### Características de Ordenamiento
- ? Ordenar por:
  - Fecha (Reciente/Antigua)
  - Monto (Mayor/Menor)
  - Número de recibo

### Tipos de Pago Soportados
1. **Efectivo** - Pago en efectivo
2. **Transferencia** - Con campo para NCF/Número de factura
3. **Cheque** - Con campos para número de cheque y banco

### Panel de Resumen
- Total de recibos filtrados
- Total en efectivo
- Total en transferencias
- Total en cheques

### Características del Formulario
- Validación de campos obligatorios
- Cálculo automático de totales
- Campos condicionales según tipo de pago
- Interfaz intuitiva y fácil de usar

## Modelo de Datos

```csharp
public class Recibo
{
    public int NumeroRecibo { get; set; }
    public DateTime Fecha { get; set; }
    public string RecibimosDe { get; set; }
    public decimal Monto { get; set; }
    public string MontoEnLetras { get; set; }
    public string Concepto { get; set; }
    
    // Tipo de pago
    public bool EsEfectivo { get; set; }
    public bool EsTransferencia { get; set; }
    public bool EsCheque { get; set; }
    
    // Datos adicionales
    public string NumeroFacturaNCF { get; set; }
    public string NumeroCheque { get; set; }
    public string Banco { get; set; }
}
```

## Próximas Mejoras Sugeridas

1. **Impresión de Recibos**
   - Generar PDF del recibo
   - Plantilla personalizable
   - Vista previa antes de imprimir

2. **Filtrado por Fecha**
   - Implementar filtros de fecha personalizados
   - Selector de rango de fechas

3. **Exportación**
   - Exportar a Excel
   - Exportar a PDF
   - Generar reportes estadísticos

4. **Integración con Base de Datos**
   - Conectar con base de datos real
   - Persistencia de datos

5. **Numeración Automática**
   - Secuencia automática de números de recibo
   - Configuración de prefijos

## Navegación

La página de Recibos se accede desde el menú principal de navegación en `HomeWindow` seleccionando la opción "Recibos".
