-- Script para crear la tabla CajaChica en la base de datos Ramafemenina
-- Ejecutar este script en SQL Server Management Studio

USE Ramafemenina;
GO

PRINT '============================================================================';
PRINT 'CREANDO TABLA CAJACHICA';
PRINT '============================================================================';
PRINT '';

-- Verificar si la tabla existe
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.CajaChica') AND type = 'U')
BEGIN
    PRINT 'Creando tabla CajaChica...';
    
    CREATE TABLE dbo.CajaChica (
        NumeroRecibo INT IDENTITY(1,1) NOT NULL,
        Fecha DATETIME NOT NULL,
        PagadoA NVARCHAR(200) NOT NULL,
        Monto DECIMAL(18,2) NOT NULL DEFAULT 0,
        ConCargoA NVARCHAR(200) NULL,
        Concepto NVARCHAR(500) NULL,
        CONSTRAINT PK_CajaChica PRIMARY KEY (NumeroRecibo)
    );
    
    PRINT '? Tabla CajaChica creada exitosamente.';
    PRINT '';
    
    -- Insertar datos de prueba
    PRINT 'Insertando datos de prueba...';
    
    INSERT INTO CajaChica (Fecha, PagadoA, Monto, ConCargoA, Concepto)
    VALUES 
        (GETDATE(), 'Juan Pérez', 500.00, 'Gastos Administrativos', 'Compra de material de oficina'),
        (DATEADD(day, -1, GETDATE()), 'María García', 1200.00, 'Mantenimiento', 'Reparación de equipo de aire acondicionado'),
        (DATEADD(day, -3, GETDATE()), 'Carlos Rodríguez', 350.00, 'Transporte', 'Gasolina para vehículo institucional');
    
    PRINT '? Datos de prueba insertados.';
    PRINT '';
END
ELSE
BEGIN
    PRINT 'La tabla CajaChica ya existe.';
    PRINT '';
END

-- Verificar estructura
PRINT 'Estructura de la tabla CajaChica:';
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.CajaChica')
ORDER BY c.column_id;
PRINT '';

-- Mostrar registros
DECLARE @totalRegistros INT;
SELECT @totalRegistros = COUNT(*) FROM dbo.CajaChica;
PRINT 'Total de registros en CajaChica: ' + CAST(@totalRegistros AS VARCHAR);
PRINT '';

PRINT '============================================================================';
PRINT 'TABLA CAJACHICA CREADA EXITOSAMENTE';
PRINT '============================================================================';
PRINT '';
PRINT 'La tabla está lista para usarse en la aplicación.';
PRINT '';

GO
