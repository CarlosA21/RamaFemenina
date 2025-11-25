# Guía de Uso - Crystal Reports Optimizado (.NET 8)

## ?? Descripción General

El servicio `CrystalReportService` ha sido optimizado para replicar la funcionalidad del código original de .NET Framework 3.5, pero adaptado a .NET 8 con mejoras de rendimiento y mantenibilidad.

### ? Características Principales

1. **Configuración Automática de BD**: Lee la conexión desde `appsettings.json` y la aplica automáticamente a todos los reportes
2. **Manejo de Parámetros**: Establece parámetros dinámicamente de forma segura
3. **Filtros RecordSelectionFormula**: Aplica filtros a los reportes principal y subreportes
4. **Soporte de Subreportes**: Configura automáticamente subreportes con sus propios filtros
5. **Exportación a PDF**: Exporta reportes a PDF y los abre automáticamente
6. **Gestión de Recursos**: Libera correctamente los recursos de Crystal Reports

---

## ?? Configuración Inicial

### 1. Cadena de Conexión en `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Ramafemenina;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

El servicio extrae automáticamente:
- **ServerName**: `localhost`
- **DatabaseName**: `Ramafemenina`
- **IntegratedSecurity**: `true` (si usa Trusted_Connection)
- **UserID/Password**: Si no usa autenticación de Windows

### 2. Registro de Servicios en `App.xaml.cs`

```csharp
services.AddSingleton<IConfiguration>(configuration);
services.AddDbContext<RamaFemeninaContext>(options => options.UseSqlServer(connectionString));
services.AddScoped<CrystalReportService>();
services.AddScoped<SimpleReportService>();
services.AddScoped<ReportManager>();
```

---

## ?? Ejemplos de Uso

### Opción 1: Reporte Simple (Sin Parámetros)

```csharp
// Ejemplo: Reporte de Pacientes Activos
var reportManager = serviceProvider.GetRequiredService<ReportManager>();
var pdfPath = await reportManager.GenerarReporteAreaAsync();
// El reporte se abre automáticamente
```

### Opción 2: Reporte con Filtro (RecordSelectionFormula)

```csharp
// Ejemplo: Donaciones de un paciente específico
var reportManager = serviceProvider.GetRequiredService<ReportManager>();
var pdfPath = await reportManager.GenerarReporteDonacionesPacienteAsync("001-1234567-8");
// Internamente aplica: {Pacientes.idpaciente} = '001-1234567-8'
```

### Opción 3: Reporte con Subreportes y Filtro por Año

```csharp
// Ejemplo: Reporte de Área por Año (con 2 subreportes)
var reportManager = serviceProvider.GetRequiredService<ReportManager>();
var pdfPath = await reportManager.GenerarReporteAreaPorAnioAsync(2024);

// Internamente configura:
// - Reporte principal: {View1.año} = 2024
// - Subreporte "grafica": {grafico_area.año} = 2024
// - Subreporte "genero": {View2genero.Expr1} = 2024
```

### Opción 4: Recibo con Parámetros Simples

```csharp
// Ejemplo: Recibo de Ingresos
var parametros = new ReciboParametros
{
    NumeroRecibo = 1234,
    Fecha = DateTime.Now,
    Nombre = "Juan Pérez",
    Cedula = "001-1234567-8",
    Monto = 5000.00m,
    MontoEnLetras = "Cinco mil pesos exactos",
    Concepto = "Donación voluntaria",
    NumeroCheque = "CH-12345"
};

var reportManager = serviceProvider.GetRequiredService<ReportManager>();
var pdfPath = await reportManager.GenerarReciboIngresosAsync(parametros);
```

### Opción 5: Recibo con Parámetros Condicionales (Checkboxes)

```csharp
// Ejemplo: Recibo de Ingreso Completo
var parametros = new ReciboCompletoParametros
{
    NumeroRecibo = 1235,
    Fecha = DateTime.Now,
    Nombre = "María López",
    Monto = 3500.00m,
    MontoEnLetras = "Tres mil quinientos pesos",
    Concepto = "Donación para tratamiento",
    Efectivo = false,
    Cheque = true,
    Transferencia = false,
    NumeroCheque = "CH-54321",
    Banco = "Banco Popular",
    NCF = "B0100000123"
};

var reportManager = serviceProvider.GetRequiredService<ReportManager>();
var pdfPath = await reportManager.GenerarReciboIngresoCompletoAsync(parametros);

// Internamente establece:
// - rec = " " (checkbox vacío)
// - cheq = "?" (checkbox marcado)
// - trans = " " (checkbox vacío)
```

### Opción 6: Recibo de Desembolso (Caja Chica)

```csharp
// Ejemplo: Voucher de Desembolso
var parametros = new DesembolsoParametros
{
    NumeroRecibo = 45,
    Fecha = DateTime.Now,
    Nombre = "Proveedor ABC",
    Monto = 1500.00m,
    MontoEnLetras = "Un mil quinientos pesos",
    Concepto = "Compra de materiales de oficina",
    CargoA = "Gastos administrativos"
};

var reportManager = serviceProvider.GetRequiredService<ReportManager>();
var pdfPath = await reportManager.GenerarReciboDesembolsoAsync(parametros);
```

---

## ?? Uso Desde una Página XAML

### Ejemplo Completo en `ReportPage.xaml.cs`

```csharp
using Microsoft.UI.Xaml.Controls;
using RamaFemenina.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace RamaFemenina;

public sealed partial class ReportPage : Page
{
    private ReportManager? _reportManager;

    public ReportPage()
    {
        InitializeComponent();
        InitializeServices();
    }

    private async void InitializeServices()
    {
        try
        {
            var app = (App)App.Current;
            _reportManager = await ReportManager.CreateAsync(app.Services);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error inicializando servicios: {ex.Message}");
        }
    }

    private async void btnReporteArea_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_reportManager == null) return;

            btnGenerarReporte.IsEnabled = false;
            progressRing.IsActive = true;

            // Generar y mostrar reporte
            var pdfPath = await _reportManager.GenerarReporteAreaAsync();

            progressRing.IsActive = false;
            btnGenerarReporte.IsEnabled = true;

            txtStatus.Text = $"? Reporte generado: {pdfPath}";
        }
        catch (Exception ex)
        {
            progressRing.IsActive = false;
            btnGenerarReporte.IsEnabled = true;
            txtStatus.Text = $"? Error: {ex.Message}";
        }
    }
}
```

---

## ?? Mapeo de Opciones (Código Original ? Código Nuevo)

| Opción | Descripción | Código Original | Código Nuevo |
|--------|-------------|----------------|--------------|
| 1 | Reporte de área | `Areporte.rpt` | `GenerarReporteAreaAsync()` |
| 2 | Pacientes fallecidos | `Freporte.rpt` | `GenerarReporteFallecidasAsync()` |
| 3 | Donaciones por paciente | `ReporteD.rpt` | `GenerarReporteDonacionesPacienteAsync(id)` |
| 4 | Pacientes activas | `reporte_activas.rpt` | `GenerarReporteActivasAsync()` |
| 5 | Fallecidas detallado | `Reporte_fallecidas.rpt` | `GenerarReporteFallecidasDetalladoAsync()` |
| 6 | Área por año (con subreportes) | `area_report.rpt` | `GenerarReporteAreaPorAnioAsync(anio)` |
| 7 | Recibo de ingresos v1 | `ingrecibos.rpt` | `GenerarReciboIngresosAsync(params)` |
| 8 | Recibo de ingresos v2 | `reciboingreso.rpt` | `GenerarReciboIngresoCompletoAsync(params)` |
| 9 | Recibo de desembolso | `desembolso.rpt` | `GenerarReciboDesembolsoAsync(params)` |

---

## ?? Comparación: Código Original vs. Optimizado

### Código Original (.NET Framework 3.5)

```csharp
// Código viejo - Manual y verboso
Reporte report = new Reporte();
report.opcion = 8;
report.nombre = "Juan Pérez";
report.monto = "5000.00";
report.nletra = "Cinco mil pesos exactos";
report.concepto = "Donación voluntaria";
report.fecha = DateTime.Now.ToString("dd/MM/yyyy");
report.nrecibo = "001234";
report.banco = "Banco Popular";
report.cheque = "CH-12345";
report.factura = "B0100000123";
report.efe = false;
report.cheq = true;
report.trans = false;
report.ShowDialog();
```

### Código Nuevo (.NET 8 Optimizado)

```csharp
// Código nuevo - Moderno, tipado y async
var parametros = new ReciboCompletoParametros
{
    NumeroRecibo = 1234,
    Fecha = DateTime.Now,
    Nombre = "Juan Pérez",
    Monto = 5000.00m,
    MontoEnLetras = "Cinco mil pesos exactos",
    Concepto = "Donación voluntaria",
    Banco = "Banco Popular",
    NumeroCheque = "CH-12345",
    NCF = "B0100000123",
    Efectivo = false,
    Cheque = true,
    Transferencia = false
};

var pdfPath = await reportManager.GenerarReciboIngresoCompletoAsync(parametros);
// Se abre automáticamente en el visor PDF predeterminado
```

**Ventajas del código nuevo:**
- ? Fuertemente tipado (detecta errores en tiempo de compilación)
- ? Asíncrono (no bloquea la UI)
- ? Inyección de dependencias (fácil de testear)
- ? Configuración centralizada (appsettings.json)
- ? Manejo automático de recursos
- ? Logging integrado (Debug.WriteLine)

---

## ??? Métodos Internos Clave (Cómo Funciona)

### 1. Configuración de Conexión
```csharp
private ConnectionInfo ConfigurarConexionDesdeAppSettings()
{
    // Lee appsettings.json
    var connectionString = _configuration.GetConnectionString("DefaultConnection");
    var builder = new SqlConnectionStringBuilder(connectionString);
    
    // Crea ConnectionInfo
    return new ConnectionInfo
    {
        ServerName = builder.DataSource,
        DatabaseName = builder.InitialCatalog,
        IntegratedSecurity = builder.IntegratedSecurity,
        UserID = builder.UserID,
        Password = builder.Password
    };
}
```

### 2. Aplicación de Credenciales (Método Clave)
```csharp
private void AplicarConexionAReporte(ReportDocument reporte)
{
    // Reporte principal
    foreach (Table tabla in reporte.Database.Tables)
    {
        var logInfo = tabla.LogOnInfo;
        logInfo.ConnectionInfo = _connectionInfo;
        tabla.ApplyLogOnInfo(logInfo);
    }
    
    // Subreportes (recursivo)
    foreach (ReportDocument subreporte in reporte.Subreports)
    {
        AplicarConexionAReporte(subreporte);
    }
}
```

### 3. Establecimiento de Parámetros (Seguro)
```csharp
private void EstablecerParametroSeguro(ReportDocument reporte, string nombreParametro, string valor)
{
    try
    {
        if (reporte.ParameterFields[nombreParametro] != null)
        {
            reporte.SetParameterValue(nombreParametro, valor ?? string.Empty);
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Parámetro '{nombreParametro}' no existe: {ex.Message}");
    }
}
```

### 4. Exportación y Visualización
```csharp
private string MostrarReporte(ReportDocument reporte, string nombreBase)
{
    // Exportar a PDF temporal
    var pdfFile = Path.Combine(Path.GetTempPath(), $"{nombreBase}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    reporte.ExportToDisk(ExportFormatType.PortableDocFormat, pdfFile);
    
    // Abrir con visor predeterminado
    Process.Start(new ProcessStartInfo { FileName = pdfFile, UseShellExecute = true });
    
    // Liberar recursos
    reporte.Close();
    reporte.Dispose();
    
    return pdfFile;
}
```

---

## ?? Solución de Problemas

### Error: "Connection String no encontrada"
```
? Solución: Verifica que appsettings.json esté copiado al directorio de salida
```

### Error: "Reporte no encontrado"
```
? Solución: Asegúrate de que los archivos .rpt estén en la carpeta Reportes/
```

### Error: "No se puede conectar a la base de datos"
```
? Solución: Verifica la cadena de conexión en appsettings.json
? Verifica que SQL Server esté corriendo
```

### Error: "Parámetro no existe"
```
? Solución: El método EstablecerParametroSeguro ya maneja esto automáticamente
? Revisa el log de Debug para ver qué parámetros no existen
```

### El reporte no muestra datos
```
? Solución: 
   1. Verifica las credenciales de conexión
   2. Revisa los filtros RecordSelectionFormula
   3. Asegúrate de que las tablas/vistas existan en la BD
```

---

## ?? Mejoras Implementadas vs. Código Original

| Aspecto | Código Original | Código Optimizado |
|---------|----------------|-------------------|
| **Configuración BD** | Hardcodeada en código | Desde appsettings.json |
| **Manejo de Errores** | Excepciones sin manejar | Try-catch con logging |
| **Async/Await** | Bloqueo de UI | Asíncrono |
| **Tipos** | Propiedades públicas sueltas | Clases tipadas (DTO) |
| **Logging** | Ninguno | Debug.WriteLine detallado |
| **Recursos** | Manual (propenso a leaks) | Using statements y Dispose() |
| **Testeable** | Difícil (dependencias acopladas) | Fácil (DI) |
| **Parámetros** | SetParameterValue sin validación | EstablecerParametroSeguro() |
| **Subreportes** | Manual | Automático con recursión |

---

## ?? Código Equivalente al Original

```csharp
// ????????????????????????????????????????????????????????????????????
// EJEMPLO COMPLETO: Cómo el código viejo se traduce al nuevo
// ????????????????????????????????????????????????????????????????????

// ??? CÓDIGO ORIGINAL (.NET Framework 3.5) ???
private void button_Click(object sender, EventArgs e)
{
    Reporte report = new Reporte();
    report.opcion = 6;  // Reporte área por año
    report.anio = 2024;
    report.ShowDialog();
}

// Dentro de Reporte.cs:
private void Reporte_Load(object sender, EventArgs e)
{
    conexinfo = new ConnectionInfo();
    conexinfo.ServerName = "10.0.0.6\\sqlexpress";
    conexinfo.DatabaseName = "DonacionesDB";
    conexinfo.UserID = "admindb";
    conexinfo.Password = "admin123";
    
    reporte6 = new area_report();
    reporte6.RecordSelectionFormula = "{View1.año} = " + anio;
    
    ReportDocument subrep = reporte6.OpenSubreport("grafica");
    subrep.RecordSelectionFormula = "{grafico_area.año} = " + anio;
    
    ReportDocument subrep2 = reporte6.OpenSubreport("genero");
    subrep2.RecordSelectionFormula = "{View2genero.Expr1} = " + anio;
    
    mostrar(reporte6, conexinfo);
}

private void mostrar(ReportClass reporte, ConnectionInfo conexinfo)
{
    foreach (Table tabla in reporte.Database.Tables)
    {
        var loginfo = tabla.LogOnInfo;
        loginfo.ConnectionInfo = conexinfo;
        tabla.ApplyLogOnInfo(loginfo);
    }
    crystalReportViewer1.ReportSource = reporte;
}

// ??? CÓDIGO NUEVO (.NET 8 Optimizado) ???
private async void btnGenerarReporte_Click(object sender, RoutedEventArgs e)
{
    var reportManager = serviceProvider.GetRequiredService<ReportManager>();
    var pdfPath = await reportManager.GenerarReporteAreaPorAnioAsync(2024);
    // ? LISTO! Todo lo demás se hace automáticamente
}

// El servicio internamente hace EXACTAMENTE lo mismo pero mejor:
// 1. Lee credenciales desde appsettings.json (no hardcodeadas)
// 2. Aplica filtro al reporte principal
// 3. Configura ambos subreportes con sus filtros
// 4. Aplica conexión a todas las tablas recursivamente
// 5. Exporta a PDF y lo abre automáticamente
// 6. Libera recursos correctamente
```

---

## ?? Referencias

- **Código Original**: Sistema .NET Framework 3.5 con Crystal Reports
- **Documentación Crystal Reports**: [SAP Crystal Reports Developer Guide](https://help.sap.com/docs/SAP_CRYSTAL_REPORTS)
- **Connection String Builder**: [SqlConnectionStringBuilder](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnectionstringbuilder)

---

**Autor**: Sistema de migración automatizada
**Fecha**: 2024
**Versión**: 1.0 (.NET 8 + Crystal Reports 13.0.4003)
