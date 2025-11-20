-- Verification Script: Check if identity columns are properly configured
-- Run this script to verify your database schema is correct

USE Ramafemenina;
GO

PRINT '========================================================================';
PRINT 'IDENTITY COLUMN VERIFICATION REPORT';
PRINT 'Database: Ramafemenina';
PRINT 'Generated: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '========================================================================';
PRINT '';

-- Check Donaciones table
PRINT '1. DONACIONES TABLE';
PRINT '-------------------';
IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Donaciones') 
    AND name = 'Iddonacion' 
    AND is_identity = 1
)
BEGIN
    SELECT 
        'Column Name' = name,
        'Data Type' = TYPE_NAME(user_type_id),
        'Is Identity' = CASE WHEN is_identity = 1 THEN 'YES ?' ELSE 'NO ?' END,
        'Seed Value' = CAST(IDENT_SEED('dbo.Donaciones') AS INT),
        'Increment' = CAST(IDENT_INCR('dbo.Donaciones') AS INT),
        'Current Value' = IDENT_CURRENT('dbo.Donaciones')
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Donaciones') 
    AND name = 'Iddonacion';
    
    PRINT 'Status: ? IDENTITY column is properly configured';
END
ELSE
BEGIN
    PRINT 'Status: ? WARNING - Iddonacion is NOT an IDENTITY column!';
    PRINT 'Action Required: Run Fix_Identity_Columns.sql script';
END
PRINT '';

-- Check Cheques table
PRINT '2. CHEQUES TABLE';
PRINT '----------------';
IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Cheques') 
    AND name = 'idCheque' 
    AND is_identity = 1
)
BEGIN
    SELECT 
        'Column Name' = name,
        'Data Type' = TYPE_NAME(user_type_id),
        'Is Identity' = CASE WHEN is_identity = 1 THEN 'YES ?' ELSE 'NO ?' END,
        'Seed Value' = CAST(IDENT_SEED('dbo.Cheques') AS INT),
        'Increment' = CAST(IDENT_INCR('dbo.Cheques') AS INT),
        'Current Value' = IDENT_CURRENT('dbo.Cheques')
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Cheques') 
    AND name = 'idCheque';
    
    PRINT 'Status: ? IDENTITY column is properly configured';
END
ELSE
BEGIN
    PRINT 'Status: ? WARNING - idCheque is NOT an IDENTITY column!';
    PRINT 'Action Required: Run Fix_Identity_Columns.sql script';
END
PRINT '';

-- Check Clientes table
PRINT '3. CLIENTES TABLE';
PRINT '-----------------';
IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Clientes') 
    AND name = 'idCliente' 
    AND is_identity = 1
)
BEGIN
    SELECT 
        'Column Name' = name,
        'Data Type' = TYPE_NAME(user_type_id),
        'Is Identity' = CASE WHEN is_identity = 1 THEN 'YES ?' ELSE 'NO ?' END,
        'Seed Value' = CAST(IDENT_SEED('dbo.Clientes') AS INT),
        'Increment' = CAST(IDENT_INCR('dbo.Clientes') AS INT),
        'Current Value' = IDENT_CURRENT('dbo.Clientes')
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Clientes') 
    AND name = 'idCliente';
    
    PRINT 'Status: ? IDENTITY column is properly configured';
END
ELSE
BEGIN
    PRINT 'Status: ? WARNING - idCliente is NOT an IDENTITY column!';
    PRINT 'Action Required: Run Fix_Identity_Columns.sql script';
END
PRINT '';

-- Check Recibo table
PRINT '4. RECIBO TABLE';
PRINT '---------------';
IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Recibo') 
    AND name = 'NumeroRecibo' 
    AND is_identity = 1
)
BEGIN
    SELECT 
        'Column Name' = name,
        'Data Type' = TYPE_NAME(user_type_id),
        'Is Identity' = CASE WHEN is_identity = 1 THEN 'YES ?' ELSE 'NO ?' END,
        'Seed Value' = CAST(IDENT_SEED('dbo.Recibo') AS INT),
        'Increment' = CAST(IDENT_INCR('dbo.Recibo') AS INT),
        'Current Value' = IDENT_CURRENT('dbo.Recibo')
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Recibo') 
    AND name = 'NumeroRecibo';
    
    PRINT 'Status: ? IDENTITY column is properly configured';
END
ELSE
BEGIN
    PRINT 'Status: ? WARNING - NumeroRecibo is NOT an IDENTITY column!';
    PRINT 'Action Required: Run Fix_Identity_Columns.sql script';
END
PRINT '';

-- Check acceso table
PRINT '5. ACCESO TABLE';
PRINT '---------------';
IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.acceso') 
    AND name = 'idusuario' 
    AND is_identity = 1
)
BEGIN
    SELECT 
        'Column Name' = name,
        'Data Type' = TYPE_NAME(user_type_id),
        'Is Identity' = CASE WHEN is_identity = 1 THEN 'YES ?' ELSE 'NO ?' END,
        'Seed Value' = CAST(IDENT_SEED('dbo.acceso') AS INT),
        'Increment' = CAST(IDENT_INCR('dbo.acceso') AS INT),
        'Current Value' = IDENT_CURRENT('dbo.acceso')
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.acceso') 
    AND name = 'idusuario';
    
    PRINT 'Status: ? IDENTITY column is properly configured';
END
ELSE
BEGIN
    PRINT 'Status: ? WARNING - idusuario is NOT an IDENTITY column!';
    PRINT 'Action Required: Run Fix_Identity_Columns.sql script';
END
PRINT '';

-- Summary
PRINT '========================================================================';
PRINT 'SUMMARY';
PRINT '========================================================================';

DECLARE @IssueCount INT = 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Donaciones') AND name = 'Iddonacion' AND is_identity = 1)
    SET @IssueCount = @IssueCount + 1;
    
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Cheques') AND name = 'idCheque' AND is_identity = 1)
    SET @IssueCount = @IssueCount + 1;
    
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Clientes') AND name = 'idCliente' AND is_identity = 1)
    SET @IssueCount = @IssueCount + 1;
    
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recibo') AND name = 'NumeroRecibo' AND is_identity = 1)
    SET @IssueCount = @IssueCount + 1;
    
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.acceso') AND name = 'idusuario' AND is_identity = 1)
    SET @IssueCount = @IssueCount + 1;

IF @IssueCount = 0
BEGIN
    PRINT '? All identity columns are properly configured!';
    PRINT 'Your database is ready to use.';
END
ELSE
BEGIN
    PRINT '? Found ' + CAST(@IssueCount AS VARCHAR) + ' issue(s) with identity columns.';
    PRINT 'Please run the Fix_Identity_Columns.sql script to resolve these issues.';
END

PRINT '';
PRINT '========================================================================';
GO
