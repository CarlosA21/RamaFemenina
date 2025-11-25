# ?? Ejemplos Prácticos - Crystal Reports por Tipo

## ?? Uso Rápido en Tus Páginas

### Inicialización en cualquier Page.xaml.cs

```csharp
using RamaFemenina.Services;
using Microsoft.Extensions.DependencyInjection;

public sealed partial class MiPage : Page
{
    private ReportManager? _reportManager;

    public MiPage()
    {
        InitializeComponent();
        InicializarServicios();
    }

    private async void InicializarServicios()
    {
        var app = (App)App.Current;
        _reportManager = await ReportManager.CreateAsync(app.Services);
    }
}
```

---

## ?? OPCIÓN 1: Reporte por Área

**Archivo**: `Areporte.rpt` o `area_report.rpt`  
**Descripción**: Lista pacientes agrupados por área geográfica  
**Parámetros**: Ninguno  
**Filtros**: Ninguno

### Código:
```csharp
private async void btnReporteArea_Click(object sender, RoutedEventArgs e)
{
    try
    {
        progressBar.IsActive = true;
        
        var pdfPath = await _reportManager.GenerarReporteAreaAsync();
        
        progressBar.IsActive = false;
        await MostrarMensaje($"? Reporte generado: {pdfPath}");
    }
    catch (Exception ex)
    {
        progressBar.IsActive = false;
        await MostrarError($"? Error: {ex.Message}");
    }
}
```

### Salida del Log:
```
[CRYSTAL] ??? Generando Reporte por Área ???
[CRYSTAL] ? Reporte cargado: Areporte.rpt
[CRYSTAL] Aplicando credenciales a 1 tablas...
[CRYSTAL]   ? Tabla: Pacientes
[CRYSTAL] ? PDF exportado: C:\Users\...\ReporteArea_20241215_143022.pdf
[CRYSTAL] ? Reporte mostrado exitosamente
```

---

## ?? OPCIÓN 2: Reporte de Fallecidas

**Archivo**: `Freporte.rpt` o `Reporte_fallecidas.rpt`  
**Descripción**: Lista de pacientes fallecidas  
**Parámetros**: Ninguno  
**Filtros**: Ninguno (el .rpt ya tiene el filtro interno)

### Código:
```csharp
private async void btnReporteFallecidas_Click(object sender, RoutedEventArgs e)
{
    try
    {
        progressBar.IsActive = true;
        
        var pdfPath = await _reportManager.GenerarReporteFallecidasAsync();
        
        progressBar.IsActive = false;
        await MostrarMensaje($"? Reporte generado: {pdfPath}");
    }
    catch (Exception ex)
    {
        progressBar.IsActive = false;
        await MostrarError($"? Error: {ex.Message}");
    }
}
```

---

## ?? OPCIÓN 3: Reporte de Donaciones por Paciente

**Archivo**: `ReporteD.rpt`  
**Descripción**: Histórico de donaciones de un paciente específico  
**Parámetros**: `idPaciente` (string)  
**Filtros**: `{Pacientes.idpaciente} = '001-1234567-8'`

### Código:
```csharp
private async void btnReporteDonaciones_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // Obtener paciente seleccionado
        if (dgPacientes.SelectedItem is not Pacientes paciente)
        {
            await MostrarError("Seleccione un paciente");
            return;
        }

        progressBar.IsActive = true;
        
        var pdfPath = await _reportManager.GenerarReporteDonacionesPacienteAsync(
            paciente.cedula  // ID del paciente
        );
        
        progressBar.IsActive = false;
        await MostrarMensaje($"? Reporte generado: {pdfPath}");
    }
    catch (Exception ex)
    {
        progressBar.IsActive = false;
        await MostrarError($"? Error: {ex.Message}");
    }
}
```

### Salida del Log:
```
[CRYSTAL] ??? Generando Reporte Donaciones - Paciente: 001-1234567-8 ???
[CRYSTAL] ? Reporte cargado: ReporteD.rpt
[CRYSTAL] Aplicando credenciales a 2 tablas...
[CRYSTAL]   ? Tabla: Pacientes
[CRYSTAL]   ? Tabla: Donaciones
[CRYSTAL] Filtro aplicado: {Pacientes.idpaciente} = '001-1234567-8'
[CRYSTAL] ? PDF exportado: C:\Users\...\ReporteDonaciones_001-1234567-8.pdf
```

---

## ?? OPCIÓN 4: Reporte de Pacientes Activas

**Archivo**: `reporte_activas.rpt`  
**Descripción**: Lista de pacientes activas (no fallecidas)  
**Parámetros**: Ninguno  
**Filtros**: Ninguno

### Código:
```csharp
private async void btnReporteActivas_Click(object sender, RoutedEventArgs e)
{
    try
    {
        progressBar.IsActive = true;
        
        var pdfPath = await _reportManager.GenerarReporteActivasAsync();
        
        progressBar.IsActive = false;
        await MostrarMensaje($"? Reporte generado: {pdfPath}");
    }
    catch (Exception ex)
    {
        progressBar.IsActive = false;
        await MostrarError($"? Error: {ex.Message}");
    }
}
```

---

## ?? OPCIÓN 6: Reporte por Área y Año (CON SUBREPORTES)

**Archivo**: `area_report.rpt` o `Areporte.rpt`  
**Descripción**: Casos por área en un año específico con gráficas  
**Parámetros**: `anio` (int)  
**Filtros**: 
- Reporte principal: `{View1.año} = 2024`
- Subreporte "grafica": `{grafico_area.año} = 2024`
- Subreporte "genero": `{View2genero.Expr1} = 2024`

### Código con ComboBox de Año:
```csharp
private async void btnReporteAreaAnio_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // Validar que se seleccionó un año
        if (cmbAnio.SelectedItem == null)
        {
            await MostrarError("Seleccione un año");
            return;
        }

        int anioSeleccionado = int.Parse(cmbAnio.SelectedItem.ToString());
        
        progressBar.IsActive = true;
        
        var pdfPath = await _reportManager.GenerarReporteAreaPorAnioAsync(anioSeleccionado);
        
        progressBar.IsActive = false;
        await MostrarMensaje($"? Reporte generado: {pdfPath}");
    }
    catch (Exception ex)
    {
        progressBar.IsActive = false;
        await MostrarError($"? Error: {ex.Message}");
    }
}

// Llenar ComboBox al cargar la página
private void Page_Loaded(object sender, RoutedEventArgs e)
{
    // Llenar años desde 2020 hasta año actual
    int anioActual = DateTime.Now.Year;
    for (int i = anioActual; i >= 2020; i--)
    {
        cmbAnio.Items.Add(i);
    }
    cmbAnio.SelectedIndex = 0; // Seleccionar año actual
}
```

### Salida del Log:
```
[CRYSTAL] ??? Generando Reporte por Área - Año 2024 ???
[CRYSTAL] ? Reporte cargado: area_report.rpt
[CRYSTAL] Aplicando credenciales a 3 tablas...
[CRYSTAL]   ? Tabla: View1
[CRYSTAL]   ? Tabla: grafico_area
[CRYSTAL]   ? Tabla: View2genero
[CRYSTAL] Aplicando credenciales a 2 subreportes...
[CRYSTAL] Filtro principal: {View1.año} = 2024
[CRYSTAL] ? Subreporte 'grafica' configurado
[CRYSTAL] ? Subreporte 'genero' configurado
[CRYSTAL] ? PDF exportado: C:\Users\...\ReporteArea_2024.pdf
```

---

## ?? OPCIÓN 7: Recibo de Ingresos (Versión Simple)

**Archivo**: `ingrecibos.rpt`  
**Descripción**: Recibo de donación simple  
**Parámetros**: 
- `cedula`, `cheque`, `concepto`, `enletra`, `fecha`, `monto`, `nombre`, `recibo`

### Código desde Módulo de Recibos:
```csharp
private async void btnGenerarRecibo_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // Validar campos
        if (string.IsNullOrWhiteSpace(txtNombre.Text))
        {
            await MostrarError("Ingrese el nombre");
            return;
        }

        if (!decimal.TryParse(txtMonto.Text, out decimal monto) || monto <= 0)
        {
            await MostrarError("Ingrese un monto válido");
            return;
        }

        // Crear parámetros
        var parametros = new ReciboParametros
        {
            NumeroRecibo = await ObtenerProximoNumeroRecibo(),
            Fecha = dpFecha.Date?.DateTime ?? DateTime.Now,
            Nombre = txtNombre.Text.Trim(),
            Cedula = txtCedula.Text.Trim(),
            Monto = monto,
            MontoEnLetras = ConvertirMontoALetras(monto),
            Concepto = txtConcepto.Text.Trim(),
            NumeroCheque = txtNumeroCheque.Text.Trim()
        };

        progressBar.IsActive = true;
        
        var pdfPath = await _reportManager.GenerarReciboIngresosAsync(parametros);
        
        progressBar.IsActive = false;
        
        await MostrarMensaje($"? Recibo #{parametros.NumeroRecibo:000000} generado");
        LimpiarFormulario();
    }
    catch (Exception ex)
    {
        progressBar.IsActive = false;
        await MostrarError($"? Error: {ex.Message}");
    }
}

// Método auxiliar para obtener próximo número
private async Task<int> ObtenerProximoNumeroRecibo()
{
    var app = (App)App.Current;
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
    
    var ultimoRecibo = await context.Recibos
        .OrderByDescending(r => r.NumeroRecibo)
        .FirstOrDefaultAsync();
    
    return (ultimoRecibo?.NumeroRecibo ?? 0) + 1;
}
```

### Salida del Log:
```
[CRYSTAL] ??? Generando Recibo Ingresos #001234 ???
[CRYSTAL] ? Reporte cargado: ingrecibos.rpt
[CRYSTAL] Aplicando credenciales a 0 tablas...
[CRYSTAL]   ? Parámetro 'cedula' = '001-1234567-8'
[CRYSTAL]   ? Parámetro 'cheque' = 'CH-12345'
[CRYSTAL]   ? Parámetro 'concepto' = 'Donación voluntaria'
[CRYSTAL]   ? Parámetro 'enletra' = 'Cinco mil pesos exactos'
[CRYSTAL]   ? Parámetro 'fecha' = '15/12/2024'
[CRYSTAL]   ? Parámetro 'monto' = '5000.00'
[CRYSTAL]   ? Parámetro 'nombre' = 'Juan Pérez'
[CRYSTAL]   ? Parámetro 'recibo' = '001234'
[CRYSTAL] ? PDF exportado: C:\Users\...\ReciboIngresos_001234.pdf
```

---

## ?? OPCIÓN 8: Recibo de Ingreso Completo (Con Forma de Pago)

**Archivo**: `reciboingreso.rpt`  
**Descripción**: Recibo completo con checkboxes de forma de pago  
**Parámetros**: 
- `numero`, `concepto`, `enletra`, `fecha`, `monto`, `nombre`, `recibo`, `banco`, `ncf`
- `rec` (? o espacio), `cheq` (? o espacio), `trans` (? o espacio)

### Código con CheckBoxes:
```csharp
private async void btnGenerarReciboCompleto_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // Validar al menos una forma de pago seleccionada
        if (!chkEfectivo.IsChecked == true && 
            !chkCheque.IsChecked == true && 
            !chkTransferencia.IsChecked == true)
        {
            await MostrarError("Seleccione al menos una forma de pago");
            return;
        }

        if (!decimal.TryParse(txtMonto.Text, out decimal monto) || monto <= 0)
        {
            await MostrarError("Ingrese un monto válido");
            return;
        }

        // Crear parámetros completos
        var parametros = new ReciboCompletoParametros
        {
            NumeroRecibo = await ObtenerProximoNumeroRecibo(),
            Fecha = dpFecha.Date?.DateTime ?? DateTime.Now,
            Nombre = txtNombre.Text.Trim(),
            Monto = monto,
            MontoEnLetras = ConvertirMontoALetras(monto),
            Concepto = txtConcepto.Text.Trim(),
            
            // Formas de pago
            Efectivo = chkEfectivo.IsChecked == true,
            Cheque = chkCheque.IsChecked == true,
            Transferencia = chkTransferencia.IsChecked == true,
            
            // Datos adicionales
            NumeroCheque = txtNumeroCheque.Text.Trim(),
            Banco = txtBanco.Text.Trim(),
            NCF = txtNCF.Text.Trim()
        };

        progressBar.IsActive = true;
        
        var pdfPath = await _reportManager.GenerarReciboIngresoCompletoAsync(parametros);
        
        progressBar.IsActive = false;
        
        await MostrarMensaje($"? Recibo #{parametros.NumeroRecibo:000000} generado");
        LimpiarFormulario();
    }
    catch (Exception ex)
    {
        progressBar.IsActive = false;
        await MostrarError($"? Error: {ex.Message}");
    }
}

// Evento para habilitar/deshabilitar campos según forma de pago
private void chkCheque_Checked(object sender, RoutedEventArgs e)
{
    txtNumeroCheque.IsEnabled = chkCheque.IsChecked == true;
    txtBanco.IsEnabled = chkCheque.IsChecked == true;
}
```

### Salida del Log:
```
[CRYSTAL] ??? Generando Recibo Completo #001235 ???
[CRYSTAL] ? Reporte cargado: reciboingreso.rpt
[CRYSTAL]   ? Parámetro 'numero' = 'CH-54321'
[CRYSTAL]   ? Parámetro 'concepto' = 'Donación para tratamiento'
[CRYSTAL]   ? Parámetro 'enletra' = 'Tres mil quinientos pesos'
[CRYSTAL]   ? Parámetro 'fecha' = '15/12/2024'
[CRYSTAL]   ? Parámetro 'monto' = '3500.00'
[CRYSTAL]   ? Parámetro 'nombre' = 'María López'
[CRYSTAL]   ? Parámetro 'recibo' = '001235'
[CRYSTAL]   ? Parámetro 'banco' = 'Banco Popular'
[CRYSTAL]   ? Parámetro 'ncf' = 'B0100000123'
[CRYSTAL]   ? Parámetro 'rec' = ' '
[CRYSTAL]   ? Parámetro 'cheq' = '?'
[CRYSTAL]   ? Parámetro 'trans' = ' '
[CRYSTAL] Forma pago: Efectivo=False, Cheque=True, Trans=False
[CRYSTAL] ? PDF exportado: C:\Users\...\ReciboCompleto_001235.pdf
```

---

## ?? OPCIÓN 9: Recibo de Desembolso (Caja Chica)

**Archivo**: `desembolso.rpt`  
**Descripción**: Comprobante de desembolso de caja chica  
**Parámetros**: 
- `concepto`, `enletra`, `fecha`, `monto`, `nombre`, `recibo`, `cargoa`

### Código desde Módulo Caja Chica:
```csharp
private async void btnGenerarDesembolso_Click(object sender, RoutedEventArgs e)
{
    try
    {
        if (string.IsNullOrWhiteSpace(txtPagadoA.Text))
        {
            await MostrarError("Ingrese a quién se paga");
            return;
        }

        if (!decimal.TryParse(txtMonto.Text, out decimal monto) || monto <= 0)
        {
            await MostrarError("Ingrese un monto válido");
            return;
        }

        // Crear parámetros
        var parametros = new DesembolsoParametros
        {
            NumeroRecibo = await ObtenerProximoNumeroDesembolso(),
            Fecha = dpFecha.Date?.DateTime ?? DateTime.Now,
            Nombre = txtPagadoA.Text.Trim(),
            Monto = monto,
            MontoEnLetras = ConvertirMontoALetras(monto),
            Concepto = txtConcepto.Text.Trim(),
            CargoA = cmbCargoA.SelectedItem?.ToString() ?? "Gastos generales"
        };

        progressBar.IsActive = true;
        
        var pdfPath = await _reportManager.GenerarReciboDesembolsoAsync(parametros);
        
        progressBar.IsActive = false;
        
        await MostrarMensaje($"? Desembolso #{parametros.NumeroRecibo:000000} generado");
        LimpiarFormulario();
    }
    catch (Exception ex)
    {
        progressBar.IsActive = false;
        await MostrarError($"? Error: {ex.Message}");
    }
}

// Llenar ComboBox de categorías de gasto
private void Page_Loaded(object sender, RoutedEventArgs e)
{
    cmbCargoA.Items.Add("Gastos administrativos");
    cmbCargoA.Items.Add("Gastos de operación");
    cmbCargoA.Items.Add("Gastos de mantenimiento");
    cmbCargoA.Items.Add("Gastos de personal");
    cmbCargoA.Items.Add("Gastos varios");
    cmbCargoA.SelectedIndex = 0;
}
```

### Salida del Log:
```
[CRYSTAL] ??? Generando Recibo Desembolso #000045 ???
[CRYSTAL] ? Reporte cargado: desembolso.rpt
[CRYSTAL]   ? Parámetro 'concepto' = 'Compra de materiales de oficina'
[CRYSTAL]   ? Parámetro 'enletra' = 'Un mil quinientos pesos'
[CRYSTAL]   ? Parámetro 'fecha' = '15/12/2024'
[CRYSTAL]   ? Parámetro 'monto' = '1500.00'
[CRYSTAL]   ? Parámetro 'nombre' = 'Proveedor ABC'
[CRYSTAL]   ? Parámetro 'recibo' = '000045'
[CRYSTAL]   ? Parámetro 'cargoa' = 'Gastos administrativos'
[CRYSTAL] ? PDF exportado: C:\Users\...\ReciboDesembolso_000045.pdf
```

---

## ??? Métodos Auxiliares Comunes

### Convertir Monto a Letras
```csharp
private string ConvertirMontoALetras(decimal monto)
{
    // Implementación simplificada (puedes usar una biblioteca externa)
    if (monto == 0) return "Cero pesos";
    
    int parteEntera = (int)monto;
    int centavos = (int)((monto - parteEntera) * 100);
    
    string textoEntera = NumeroALetras(parteEntera);
    string textoCentavos = centavos > 0 ? $" con {centavos}/100" : "";
    
    return $"{textoEntera} pesos{textoCentavos}";
}

// Implementación básica (reemplazar con librería robusta)
private string NumeroALetras(int numero)
{
    // Simplificado para el ejemplo
    if (numero < 10) return new[] { "cero", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve" }[numero];
    // ... implementación completa
    return numero.ToString();
}
```

### Mostrar Mensajes
```csharp
private async Task MostrarMensaje(string mensaje)
{
    var dialog = new ContentDialog
    {
        Title = "Información",
        Content = mensaje,
        CloseButtonText = "OK",
        XamlRoot = this.XamlRoot
    };
    await dialog.ShowAsync();
}

private async Task MostrarError(string mensaje)
{
    var dialog = new ContentDialog
    {
        Title = "Error",
        Content = mensaje,
        CloseButtonText = "OK",
        XamlRoot = this.XamlRoot
    };
    await dialog.ShowAsync();
}
```

### Limpiar Formulario
```csharp
private void LimpiarFormulario()
{
    txtNombre.Text = string.Empty;
    txtCedula.Text = string.Empty;
    txtMonto.Text = string.Empty;
    txtConcepto.Text = string.Empty;
    txtNumeroCheque.Text = string.Empty;
    txtBanco.Text = string.Empty;
    txtNCF.Text = string.Empty;
    
    chkEfectivo.IsChecked = false;
    chkCheque.IsChecked = false;
    chkTransferencia.IsChecked = false;
    
    dpFecha.Date = DateTime.Now;
}
```

---

## ?? Ejemplo de XAML para Formulario de Recibo

```xml
<StackPanel Spacing="16" Padding="20">
    <TextBlock Text="Generar Recibo de Ingreso" 
               Style="{StaticResource TitleTextBlockStyle}"/>
    
    <TextBox x:Name="txtNombre" 
             Header="Nombre completo" 
             PlaceholderText="Juan Pérez"/>
    
    <TextBox x:Name="txtCedula" 
             Header="Cédula" 
             PlaceholderText="001-1234567-8"/>
    
    <TextBox x:Name="txtMonto" 
             Header="Monto" 
             PlaceholderText="0.00"
             InputScope="Number"/>
    
    <TextBox x:Name="txtConcepto" 
             Header="Concepto" 
             PlaceholderText="Donación voluntaria"/>
    
    <CalendarDatePicker x:Name="dpFecha" 
                        Header="Fecha" 
                        Date="{x:Bind DateTime.Now, Mode=OneWay}"/>
    
    <TextBlock Text="Forma de pago" 
               Style="{StaticResource SubtitleTextBlockStyle}"/>
    
    <CheckBox x:Name="chkEfectivo" 
              Content="Efectivo"/>
    
    <CheckBox x:Name="chkCheque" 
              Content="Cheque"
              Checked="chkCheque_Checked"
              Unchecked="chkCheque_Unchecked"/>
    
    <TextBox x:Name="txtNumeroCheque" 
             Header="Número de cheque" 
             PlaceholderText="CH-12345"
             IsEnabled="False"/>
    
    <TextBox x:Name="txtBanco" 
             Header="Banco" 
             PlaceholderText="Banco Popular"
             IsEnabled="False"/>
    
    <CheckBox x:Name="chkTransferencia" 
              Content="Transferencia"/>
    
    <ProgressRing x:Name="progressBar" 
                  IsActive="False"/>
    
    <Button x:Name="btnGenerarRecibo" 
            Content="Generar Recibo" 
            Click="btnGenerarReciboCompleto_Click"
            Style="{StaticResource AccentButtonStyle}"/>
</StackPanel>
```

---

**¡Listo para usar! Copia y pega estos ejemplos en tus páginas y adapta según necesites.**
