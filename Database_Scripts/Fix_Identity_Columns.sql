-- Script to fix identity columns in the database
-- Run this script on your Ramafemenina database

USE Ramafemenina;
GO

-- ============================================================================
-- FIX DONACIONES TABLE - Make Iddonacion an IDENTITY column
-- ============================================================================

-- Check if the column is already an identity column
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Donaciones') 
    AND name = 'Iddonacion' 
    AND is_identity = 1
)
BEGIN
    PRINT 'Fixing Donaciones.Iddonacion to be an IDENTITY column...'
    
    -- Step 1: Create a temporary table with the correct schema
    CREATE TABLE dbo.Donaciones_Temp (
        Iddonacion INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Fecha DATETIME NOT NULL,
        valor DECIMAL(18,2) NULL,
        total DECIMAL(18,2) NULL,
        idPaciente NVARCHAR(50) NOT NULL,
        procedimiento NVARCHAR(MAX) NULL,
        observacion NVARCHAR(MAX) NULL,
        montoSolicitado DECIMAL(18,2) NULL
    );

    -- Step 2: Copy existing data (if any) to the temp table
    IF EXISTS (SELECT 1 FROM dbo.Donaciones)
    BEGIN
        SET IDENTITY_INSERT dbo.Donaciones_Temp ON;
        
        INSERT INTO dbo.Donaciones_Temp (Iddonacion, Fecha, valor, total, idPaciente, procedimiento, observacion, montoSolicitado)
        SELECT Iddonacion, Fecha, valor, total, idPaciente, procedimiento, observacion, montoSolicitado
        FROM dbo.Donaciones;
        
        SET IDENTITY_INSERT dbo.Donaciones_Temp OFF;
    END

    -- Step 3: Drop the original table
    DROP TABLE dbo.Donaciones;

    -- Step 4: Rename the temp table to the original name
    EXEC sp_rename 'dbo.Donaciones_Temp', 'Donaciones';

    -- Step 5: Re-create the foreign key constraint to Pacientes
    ALTER TABLE dbo.Donaciones
    ADD CONSTRAINT FK_Donaciones_Pacientes 
    FOREIGN KEY (idPaciente) REFERENCES dbo.Pacientes(cedula) ON DELETE CASCADE;

    PRINT 'Donaciones.Iddonacion is now an IDENTITY column.'
END
ELSE
BEGIN
    PRINT 'Donaciones.Iddonacion is already an IDENTITY column.'
END
GO

-- ============================================================================
-- FIX CHEQUES TABLE - Ensure idCheque is an IDENTITY column
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Cheques') 
    AND name = 'idCheque' 
    AND is_identity = 1
)
BEGIN
    PRINT 'Fixing Cheques.idCheque to be an IDENTITY column...'
    
    CREATE TABLE dbo.Cheques_Temp (
        idCheque INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        monto DECIMAL(18,2) NULL,
        Fecha DATETIME NOT NULL,
        nombre NVARCHAR(MAX) NULL,
        concepto NVARCHAR(MAX) NULL,
        numero NVARCHAR(MAX) NULL
    );

    IF EXISTS (SELECT 1 FROM dbo.Cheques)
    BEGIN
        SET IDENTITY_INSERT dbo.Cheques_Temp ON;
        
        INSERT INTO dbo.Cheques_Temp (idCheque, monto, Fecha, nombre, concepto, numero)
        SELECT idCheque, monto, Fecha, nombre, concepto, numero
        FROM dbo.Cheques;
        
        SET IDENTITY_INSERT dbo.Cheques_Temp OFF;
    END

    DROP TABLE dbo.Cheques;
    EXEC sp_rename 'dbo.Cheques_Temp', 'Cheques';

    PRINT 'Cheques.idCheque is now an IDENTITY column.'
END
ELSE
BEGIN
    PRINT 'Cheques.idCheque is already an IDENTITY column.'
END
GO

-- ============================================================================
-- FIX CLIENTES TABLE - Ensure idCliente is an IDENTITY column
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Clientes') 
    AND name = 'idCliente' 
    AND is_identity = 1
)
BEGIN
    PRINT 'Fixing Clientes.idCliente to be an IDENTITY column...'
    
    CREATE TABLE dbo.Clientes_Temp (
        idCliente INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        nombre NVARCHAR(MAX) NOT NULL,
        telefono NVARCHAR(MAX) NULL,
        direccion NVARCHAR(MAX) NULL,
        rnc NVARCHAR(MAX) NULL
    );

    IF EXISTS (SELECT 1 FROM dbo.Clientes)
    BEGIN
        SET IDENTITY_INSERT dbo.Clientes_Temp ON;
        
        INSERT INTO dbo.Clientes_Temp (idCliente, nombre, telefono, direccion, rnc)
        SELECT idCliente, nombre, telefono, direccion, rnc
        FROM dbo.Clientes;
        
        SET IDENTITY_INSERT dbo.Clientes_Temp OFF;
    END

    DROP TABLE dbo.Clientes;
    EXEC sp_rename 'dbo.Clientes_Temp', 'Clientes';

    PRINT 'Clientes.idCliente is now an IDENTITY column.'
END
ELSE
BEGIN
    PRINT 'Clientes.idCliente is already an IDENTITY column.'
END
GO

-- ============================================================================
-- FIX RECIBO TABLE - Ensure NumeroRecibo is an IDENTITY column
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Recibo') 
    AND name = 'NumeroRecibo' 
    AND is_identity = 1
)
BEGIN
    PRINT 'Fixing Recibo.NumeroRecibo to be an IDENTITY column...'
    
    CREATE TABLE dbo.Recibo_Temp (
        NumeroRecibo INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Fecha DATETIME NOT NULL,
        RecibimosDe NVARCHAR(MAX) NOT NULL,
        Monto DECIMAL(18,2) NULL,
        MontoEnLetras NVARCHAR(MAX) NULL,
        Concepto NVARCHAR(MAX) NULL,
        EsEfectivo BIT NULL,
        EsTransferencia BIT NULL,
        EsCheque BIT NULL,
        NumeroFacturaNCF NVARCHAR(MAX) NULL,
        NumeroCheque NVARCHAR(MAX) NULL,
        Banco NVARCHAR(MAX) NULL
    );

    IF EXISTS (SELECT 1 FROM dbo.Recibo)
    BEGIN
        SET IDENTITY_INSERT dbo.Recibo_Temp ON;
        
        INSERT INTO dbo.Recibo_Temp (NumeroRecibo, Fecha, RecibimosDe, Monto, MontoEnLetras, Concepto, 
                                     EsEfectivo, EsTransferencia, EsCheque, NumeroFacturaNCF, NumeroCheque, Banco)
        SELECT NumeroRecibo, Fecha, RecibimosDe, Monto, MontoEnLetras, Concepto, 
               EsEfectivo, EsTransferencia, EsCheque, NumeroFacturaNCF, NumeroCheque, Banco
        FROM dbo.Recibo;
        
        SET IDENTITY_INSERT dbo.Recibo_Temp OFF;
    END

    DROP TABLE dbo.Recibo;
    EXEC sp_rename 'dbo.Recibo_Temp', 'Recibo';

    PRINT 'Recibo.NumeroRecibo is now an IDENTITY column.'
END
ELSE
BEGIN
    PRINT 'Recibo.NumeroRecibo is already an IDENTITY column.'
END
GO

PRINT 'All identity columns have been fixed successfully!'
GO
