-- Script para eliminar restricciones y columnas no utilizadas de la tabla Pacientes

USE Ramafemenina;
GO

PRINT '=== Modificando tabla Pacientes ===';
PRINT '';

-- Paso 1: Eliminar foreign key en tabla Donaciones
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Donaciones_Pacientes1')
BEGIN
    ALTER TABLE Donaciones DROP CONSTRAINT FK_Donaciones_Pacientes1;
    PRINT '? Foreign key FK_Donaciones_Pacientes1 eliminada';
END
GO

-- Paso 2: Eliminar primary key de idpaciente
IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_Pacientes_1')
BEGIN
    ALTER TABLE Pacientes DROP CONSTRAINT PK_Pacientes_1;
    PRINT '? Primary key PK_Pacientes_1 eliminada';
END
GO

-- Paso 3: Crear nueva primary key en cedula
IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('Pacientes') AND type = 'PK')
BEGIN
    ALTER TABLE Pacientes ADD CONSTRAINT PK_Pacientes PRIMARY KEY (cedula);
    PRINT '? Nueva primary key en cedula creada';
END
GO

-- Paso 4: Eliminar columna idpaciente
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Pacientes' AND COLUMN_NAME = 'idpaciente')
BEGIN
    ALTER TABLE Pacientes DROP COLUMN idpaciente;
    PRINT '? Columna idpaciente eliminada';
END
GO

-- Paso 5: Crear foreign key en Donaciones apuntando a cedula
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Donaciones_Pacientes_Cedula')
BEGIN
    -- Primero verificar que la columna idPaciente en Donaciones sea del tipo correcto
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Donaciones' AND COLUMN_NAME = 'idPaciente')
    BEGIN
        ALTER TABLE Donaciones 
        ADD CONSTRAINT FK_Donaciones_Pacientes_Cedula 
        FOREIGN KEY (idPaciente) REFERENCES Pacientes(cedula)
        ON DELETE CASCADE;
        PRINT '? Nueva foreign key FK_Donaciones_Pacientes_Cedula creada';
    END
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

-- Verificar primary key
PRINT '';
PRINT '=== Primary Key ===';
SELECT name, type_desc 
FROM sys.key_constraints 
WHERE parent_object_id = OBJECT_ID('Pacientes') AND type = 'PK';
GO

-- Verificar foreign keys
PRINT '';
PRINT '=== Foreign Keys en Donaciones ===';
SELECT name, delete_referential_action_desc
FROM sys.foreign_keys 
WHERE parent_object_id = OBJECT_ID('Donaciones');
GO

PRINT '';
PRINT '? Tabla Pacientes modificada exitosamente';
PRINT 'Ahora cedula es la clave primaria';
GO
