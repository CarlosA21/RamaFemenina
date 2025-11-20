-- Script to verify all tables are correctly configured in Ramafemenina database
-- Run this script to check the database schema

USE Ramafemenina;
GO

PRINT '=========================================='
PRINT 'VERIFICACION DE BASE DE DATOS RAMAFEMENINA'
PRINT '=========================================='
PRINT ''

-- ============================================================================
-- CHECK ACCESO TABLE
-- ============================================================================
PRINT '1. Verificando tabla ACCESO...'
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.acceso') AND type = 'U')
BEGIN
    PRINT '   ? Tabla acceso existe'
    
    SELECT 
        c.name AS ColumnName,
        t.name AS DataType,
        c.max_length AS MaxLength,
        c.is_nullable AS IsNullable,
        c.is_identity AS IsIdentity
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.acceso')
    ORDER BY c.column_id;
    
    SELECT COUNT(*) AS TotalUsuarios FROM dbo.acceso;
END
ELSE
BEGIN
    PRINT '   ? Tabla acceso NO EXISTE'
END
PRINT ''

-- ============================================================================
-- CHECK PACIENTES TABLE
-- ============================================================================
PRINT '2. Verificando tabla PACIENTES...'
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Pacientes') AND type = 'U')
BEGIN
    PRINT '   ? Tabla Pacientes existe'
    
    SELECT 
        c.name AS ColumnName,
        t.name AS DataType,
        c.max_length AS MaxLength,
        c.is_nullable AS IsNullable,
        c.is_identity AS IsIdentity
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Pacientes')
    ORDER BY c.column_id;
    
    SELECT COUNT(*) AS TotalPacientes FROM dbo.Pacientes;
END
ELSE
BEGIN
    PRINT '   ? Tabla Pacientes NO EXISTE'
END
PRINT ''

-- ============================================================================
-- CHECK DONACIONES TABLE
-- ============================================================================
PRINT '3. Verificando tabla DONACIONES...'
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Donaciones') AND type = 'U')
BEGIN
    PRINT '   ? Tabla Donaciones existe'
    
    SELECT 
        c.name AS ColumnName,
        t.name AS DataType,
        c.max_length AS MaxLength,
        c.is_nullable AS IsNullable,
        c.is_identity AS IsIdentity
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Donaciones')
    ORDER BY c.column_id;
    
    SELECT COUNT(*) AS TotalDonaciones FROM dbo.Donaciones;
    
    -- Check foreign key
    IF EXISTS (
        SELECT 1 FROM sys.foreign_keys 
        WHERE parent_object_id = OBJECT_ID('dbo.Donaciones')
        AND referenced_object_id = OBJECT_ID('dbo.Pacientes')
    )
    BEGIN
        PRINT '   ? Foreign Key a Pacientes existe'
    END
    ELSE
    BEGIN
        PRINT '   ? Foreign Key a Pacientes NO EXISTE'
    END
END
ELSE
BEGIN
    PRINT '   ? Tabla Donaciones NO EXISTE'
END
PRINT ''

-- ============================================================================
-- CHECK CHEQUES TABLE
-- ============================================================================
PRINT '4. Verificando tabla CHEQUES...'
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Cheques') AND type = 'U')
BEGIN
    PRINT '   ? Tabla Cheques existe'
    
    SELECT 
        c.name AS ColumnName,
        t.name AS DataType,
        c.max_length AS MaxLength,
        c.is_nullable AS IsNullable,
        c.is_identity AS IsIdentity
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Cheques')
    ORDER BY c.column_id;
    
    SELECT COUNT(*) AS TotalCheques FROM dbo.Cheques;
END
ELSE
BEGIN
    PRINT '   ? Tabla Cheques NO EXISTE'
END
PRINT ''

-- ============================================================================
-- CHECK CLIENTES TABLE
-- ============================================================================
PRINT '5. Verificando tabla CLIENTES...'
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Clientes') AND type = 'U')
BEGIN
    PRINT '   ? Tabla Clientes existe'
    
    SELECT 
        c.name AS ColumnName,
        t.name AS DataType,
        c.max_length AS MaxLength,
        c.is_nullable AS IsNullable,
        c.is_identity AS IsIdentity
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Clientes')
    ORDER BY c.column_id;
    
    SELECT COUNT(*) AS TotalClientes FROM dbo.Clientes;
END
ELSE
BEGIN
    PRINT '   ? Tabla Clientes NO EXISTE'
END
PRINT ''

-- ============================================================================
-- CHECK RECIBO TABLE
-- ============================================================================
PRINT '6. Verificando tabla RECIBO...'
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Recibo') AND type = 'U')
BEGIN
    PRINT '   ? Tabla Recibo existe'
    
    SELECT 
        c.name AS ColumnName,
        t.name AS DataType,
        c.max_length AS MaxLength,
        c.is_nullable AS IsNullable,
        c.is_identity AS IsIdentity
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Recibo')
    ORDER BY c.column_id;
    
    SELECT COUNT(*) AS TotalRecibos FROM dbo.Recibo;
    
    -- Check if TipoRecibo and Cedula columns exist
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recibo') AND name = 'TipoRecibo')
    BEGIN
        PRINT '   ? Columna TipoRecibo existe'
    END
    ELSE
    BEGIN
        PRINT '   ? Columna TipoRecibo NO EXISTE - Ejecutar Update_Recibo_Table.sql'
    END
    
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recibo') AND name = 'Cedula')
    BEGIN
        PRINT '   ? Columna Cedula existe'
    END
    ELSE
    BEGIN
        PRINT '   ? Columna Cedula NO EXISTE - Ejecutar Update_Recibo_Table.sql'
    END
    
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recibo') AND name = 'MontoEnLetras')
    BEGIN
        PRINT '   ? Columna MontoEnLetras existe'
    END
    ELSE
    BEGIN
        PRINT '   ? Columna MontoEnLetras NO EXISTE - Ejecutar Update_Recibo_Table.sql'
    END
END
ELSE
BEGIN
    PRINT '   ? Tabla Recibo NO EXISTE - Ejecutar Update_Recibo_Table.sql'
END
PRINT ''

-- ============================================================================
-- SUMMARY
-- ============================================================================
PRINT '=========================================='
PRINT 'RESUMEN DE VERIFICACION'
PRINT '=========================================='

DECLARE @totalTablas INT = 0;
DECLARE @tablasExistentes INT = 0;

SET @totalTablas = 6;

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.acceso') AND type = 'U')
    SET @tablasExistentes = @tablasExistentes + 1;

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Pacientes') AND type = 'U')
    SET @tablasExistentes = @tablasExistentes + 1;

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Donaciones') AND type = 'U')
    SET @tablasExistentes = @tablasExistentes + 1;

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Cheques') AND type = 'U')
    SET @tablasExistentes = @tablasExistentes + 1;

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Clientes') AND type = 'U')
    SET @tablasExistentes = @tablasExistentes + 1;

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Recibo') AND type = 'U')
    SET @tablasExistentes = @tablasExistentes + 1;

PRINT 'Tablas encontradas: ' + CAST(@tablasExistentes AS VARCHAR) + ' de ' + CAST(@totalTablas AS VARCHAR);

IF @tablasExistentes = @totalTablas
BEGIN
    PRINT '? Todas las tablas están creadas'
END
ELSE
BEGIN
    PRINT '? Faltan tablas por crear'
END

PRINT ''
PRINT 'Verificacion completada.'
GO
