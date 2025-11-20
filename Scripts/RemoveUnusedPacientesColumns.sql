-- Script para eliminar columnas no utilizadas de la tabla Pacientes

USE Ramafemenina;
GO

PRINT '=== Modificando tabla Pacientes ===';
PRINT '';

-- Verificar columnas actuales
PRINT 'Columnas actuales:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Pacientes'
ORDER BY ORDINAL_POSITION;
GO

-- Eliminar columna estado
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Pacientes' AND COLUMN_NAME = 'estado')
BEGIN
    ALTER TABLE Pacientes DROP COLUMN estado;
    PRINT '? Columna "estado" eliminada';
END
ELSE
BEGIN
    PRINT '?? Columna "estado" no existe';
END
GO

-- Eliminar columna idpaciente
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Pacientes' AND COLUMN_NAME = 'idpaciente')
BEGIN
    ALTER TABLE Pacientes DROP COLUMN idpaciente;
    PRINT '? Columna "idpaciente" eliminada';
END
ELSE
BEGIN
    PRINT '?? Columna "idpaciente" no existe';
END
GO

-- Verificar estructura final
PRINT '';
PRINT '=== Estructura final de la tabla Pacientes ===';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Pacientes'
ORDER BY ORDINAL_POSITION;
GO

PRINT '';
PRINT '? Tabla Pacientes modificada exitosamente';
GO
