-- Script para modificar la tabla acceso y agregar IDENTITY a idusuario

USE Ramafemenina;
GO

-- Paso 1: Crear tabla temporal con la estructura correcta
CREATE TABLE acceso_temp (
    idusuario INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
    usuario NVARCHAR(50) NOT NULL UNIQUE,
    contraseña NVARCHAR(100) NOT NULL
);
GO

-- Paso 2: Copiar datos existentes (si los hay)
SET IDENTITY_INSERT acceso_temp ON;
INSERT INTO acceso_temp (idusuario, usuario, contraseña)
SELECT 
    ROW_NUMBER() OVER (ORDER BY usuario) as idusuario,
    usuario, 
    contraseña
FROM acceso
WHERE usuario IS NOT NULL AND contraseña IS NOT NULL;
SET IDENTITY_INSERT acceso_temp OFF;
GO

-- Paso 3: Eliminar tabla original
DROP TABLE acceso;
GO

-- Paso 4: Renombrar tabla temporal
EXEC sp_rename 'acceso_temp', 'acceso';
GO

-- Verificar la estructura
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, 
       COLUMNPROPERTY(OBJECT_ID('acceso'), COLUMN_NAME, 'IsIdentity') as IS_IDENTITY
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'acceso';
GO

PRINT 'Tabla acceso modificada exitosamente';
PRINT 'La columna idusuario ahora es IDENTITY';
GO
