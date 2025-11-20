-- ================================================================
-- Script para Actualizar Contraseña de Usuario Existente
-- Base de Datos: Ramafemenina
-- Usuario: usuario_ejemplo
-- Contraseña (texto plano): miPassword123
-- Contraseña (BCrypt): $2a$12$Ft5K7vF8QXgWZYDGJ6KqHOqCTxM5LGqwKJXZ5VxGN7yJ1W9VLKViy
-- ================================================================

USE Ramafemenina;
GO

-- Mostrar el estado actual del usuario
PRINT '=== ANTES DE LA ACTUALIZACIÓN ===';
SELECT usuario, contraseña, 
       CASE 
           WHEN contraseña LIKE '$2%' THEN 'Hash BCrypt'
           ELSE 'Texto Plano'
       END as TipoContraseña
FROM acceso 
WHERE usuario = 'usuario_ejemplo';
GO

-- Actualizar la contraseña con el hash BCrypt
UPDATE acceso
SET contraseña = '$2a$12$Ft5K7vF8QXgWZYDGJ6KqHOqCTxM5LGqwKJXZ5VxGN7yJ1W9VLKViy'
WHERE usuario = 'usuario_ejemplo';
GO

-- Verificar que se actualizó correctamente
IF @@ROWCOUNT > 0
BEGIN
    PRINT '';
    PRINT '? Contraseña actualizada exitosamente!';
    PRINT '';
END
ELSE
BEGIN
    PRINT '';
    PRINT '? ERROR: No se encontró el usuario "usuario_ejemplo"';
    PRINT '';
END
GO

-- Mostrar el estado después de la actualización
PRINT '=== DESPUÉS DE LA ACTUALIZACIÓN ===';
SELECT usuario, 
       contraseña,
       CASE 
           WHEN contraseña LIKE '$2%' THEN 'Hash BCrypt ?'
           ELSE 'Texto Plano ?'
       END as TipoContraseña,
       LEN(contraseña) as LongitudHash
FROM acceso 
WHERE usuario = 'usuario_ejemplo';
GO

-- Resumen
PRINT '';
PRINT '=== CREDENCIALES PARA LOGIN ===';
PRINT 'Usuario: usuario_ejemplo';
PRINT 'Contraseña: miPassword123';
PRINT '';
PRINT 'Ahora puedes iniciar sesión en la aplicación con estas credenciales.';
PRINT 'La contraseña está protegida con BCrypt (Work Factor 12).';
GO
