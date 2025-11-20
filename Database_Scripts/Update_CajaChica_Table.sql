-- Script para actualizar la tabla CajaChica y hacer idrecibo IDENTITY
USE Ramafemenina;
GO

PRINT '============================================================================';
PRINT 'ACTUALIZANDO TABLA CAJACHICA';
PRINT '============================================================================';
PRINT '';

-- Verificar si idrecibo ya es IDENTITY
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.CajaChica') 
    AND name = 'idrecibo' 
    AND is_identity = 1
)
BEGIN
    PRINT 'Convirtiendo idrecibo a IDENTITY...';
    
    -- Crear tabla temporal con la estructura correcta
    CREATE TABLE dbo.CajaChica_Temp (
        idrecibo INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        recibo INT NOT NULL,
        fecha DATETIME NOT NULL,
        nombre NVARCHAR(200) NOT NULL,
        monto MONEY NOT NULL,
        cargoa NVARCHAR(200) NULL,
        concepto NVARCHAR(500) NULL
    );
    
    -- Copiar datos existentes si los hay
    IF EXISTS (SELECT 1 FROM dbo.CajaChica)
    BEGIN
        SET IDENTITY_INSERT dbo.CajaChica_Temp ON;
        
        INSERT INTO dbo.CajaChica_Temp (idrecibo, recibo, fecha, nombre, monto, cargoa, concepto)
        SELECT idrecibo, recibo, fecha, nombre, monto, cargoa, concepto
        FROM dbo.CajaChica;
        
        SET IDENTITY_INSERT dbo.CajaChica_Temp OFF;
        
        PRINT 'Datos existentes copiados.';
    END
    
    -- Eliminar la tabla original
    DROP TABLE dbo.CajaChica;
    
    -- Renombrar la tabla temporal
    EXEC sp_rename 'dbo.CajaChica_Temp', 'CajaChica';
    
    PRINT '? idrecibo convertido a IDENTITY exitosamente.';
    PRINT '';
END
ELSE
BEGIN
    PRINT '? idrecibo ya es IDENTITY.';
    PRINT '';
END

-- Mostrar estructura actualizada
PRINT 'Estructura actualizada de CajaChica:';
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_identity AS IsIdentity,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.CajaChica')
ORDER BY c.column_id;
PRINT '';

PRINT '============================================================================';
PRINT 'TABLA CAJACHICA ACTUALIZADA EXITOSAMENTE';
PRINT '============================================================================';
GO
