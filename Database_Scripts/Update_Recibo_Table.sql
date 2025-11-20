-- Script to update the Recibo table with new columns for TipoRecibo and Cedula
-- Run this script on your Ramafemenina database

USE Ramafemenina;
GO

-- ============================================================================
-- UPDATE RECIBO TABLE - Add new columns and ensure proper schema
-- ============================================================================

PRINT 'Updating Recibo table schema...'

-- Check if table exists, if not create it
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Recibo') AND type = 'U')
BEGIN
    PRINT 'Creating Recibo table...'
    
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
    
    PRINT 'Recibo table created successfully.'
END
ELSE
BEGIN
    PRINT 'Recibo table exists, checking for missing columns...'
    
    -- Add TipoRecibo column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recibo') AND name = 'TipoRecibo')
    BEGIN
        PRINT 'Adding TipoRecibo column...'
        ALTER TABLE dbo.Recibo
        ADD TipoRecibo NVARCHAR(20) NOT NULL DEFAULT 'Ingreso';
        PRINT 'TipoRecibo column added.'
    END
    ELSE
    BEGIN
        PRINT 'TipoRecibo column already exists.'
    END

    -- Add Cedula column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recibo') AND name = 'Cedula')
    BEGIN
        PRINT 'Adding Cedula column...'
        ALTER TABLE dbo.Recibo
        ADD Cedula NVARCHAR(20) NULL;
        PRINT 'Cedula column added.'
    END
    ELSE
    BEGIN
        PRINT 'Cedula column already exists.'
    END

    -- Verify/update MontoEnLetras column
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recibo') AND name = 'MontoEnLetras')
    BEGIN
        PRINT 'Adding MontoEnLetras column...'
        ALTER TABLE dbo.Recibo
        ADD MontoEnLetras NVARCHAR(500) NULL;
        PRINT 'MontoEnLetras column added.'
    END
    ELSE
    BEGIN
        PRINT 'MontoEnLetras column already exists.'
    END

    -- Ensure NumeroRecibo is IDENTITY if not already
    IF NOT EXISTS (
        SELECT 1 
        FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.Recibo') 
        AND name = 'NumeroRecibo' 
        AND is_identity = 1
    )
    BEGIN
        PRINT 'Converting NumeroRecibo to IDENTITY column...'
        
        -- Create temporary table with correct schema
        CREATE TABLE dbo.Recibo_Temp (
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

        -- Copy existing data if any
        IF EXISTS (SELECT 1 FROM dbo.Recibo)
        BEGIN
            SET IDENTITY_INSERT dbo.Recibo_Temp ON;
            
            INSERT INTO dbo.Recibo_Temp (
                NumeroRecibo, TipoRecibo, Fecha, RecibimosDe, Cedula, Monto, 
                MontoEnLetras, Concepto, EsEfectivo, EsTransferencia, EsCheque, 
                NumeroFacturaNCF, NumeroCheque, Banco
            )
            SELECT 
                ISNULL(NumeroRecibo, ROW_NUMBER() OVER (ORDER BY Fecha)),
                ISNULL(TipoRecibo, 'Ingreso'),
                Fecha, 
                RecibimosDe, 
                ISNULL(Cedula, ''),
                ISNULL(Monto, 0),
                MontoEnLetras,
                Concepto,
                ISNULL(EsEfectivo, 0),
                ISNULL(EsTransferencia, 0),
                ISNULL(EsCheque, 0),
                NumeroFacturaNCF,
                NumeroCheque,
                Banco
            FROM dbo.Recibo;
            
            SET IDENTITY_INSERT dbo.Recibo_Temp OFF;
        END

        -- Drop original table
        DROP TABLE dbo.Recibo;
        
        -- Rename temp table
        EXEC sp_rename 'dbo.Recibo_Temp', 'Recibo';
        
        PRINT 'NumeroRecibo is now an IDENTITY column.'
    END
    ELSE
    BEGIN
        PRINT 'NumeroRecibo is already an IDENTITY column.'
    END
END
GO

-- Verify the table structure
PRINT ''
PRINT 'Verifying Recibo table structure...'
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
GO

-- Display current data count
PRINT ''
PRINT 'Current data in Recibo table:'
SELECT COUNT(*) AS TotalRecibos FROM dbo.Recibo;
GO

PRINT ''
PRINT 'Recibo table update completed successfully!'
GO
