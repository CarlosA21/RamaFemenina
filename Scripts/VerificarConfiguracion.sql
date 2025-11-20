-- ================================================================
-- Script de Verificación de Configuración
-- Base de Datos: Ramafemenina
-- ================================================================

USE Ramafemenina;
GO

PRINT '=== VERIFICACIÓN DE CONFIGURACIÓN ===';
PRINT '';

-- 1. Verificar base de datos
PRINT '1. Base de Datos:';
PRINT '   Nombre: ' + DB_NAME();
PRINT '   Servidor: ' + @@SERVERNAME;
PRINT '   Usuario: ' + SUSER_SNAME();
PRINT '';

-- 2. Verificar tabla acceso
PRINT '2. Tabla acceso:';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'acceso')
BEGIN
    PRINT '   ? Tabla existe';
    
    -- Contar registros
    DECLARE @TotalUsuarios INT;
    SELECT @TotalUsuarios = COUNT(*) FROM acceso;
    PRINT '   Total de usuarios: ' + CAST(@TotalUsuarios AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '   ? Tabla NO existe';
END
PRINT '';

-- 3. Verificar usuarios
PRINT '3. Usuarios en tabla acceso:';
SELECT 
    usuario as Usuario,
    CASE 
        WHEN contraseña LIKE '$2a$12$%' THEN '? BCrypt Hash'
        WHEN contraseña LIKE '$2%' THEN '?? BCrypt (versión diferente)'
        ELSE '? Texto Plano (INSEGURO)'
    END as TipoHash,
    LEN(contraseña) as Longitud,
    LEFT(contraseña, 30) + '...' as HashParcial
FROM acceso;
GO

PRINT '';
PRINT '=== VERIFICACIÓN DE CONECTIVIDAD ===';

-- Test de consulta simple
SELECT 
    'Test de Conexión' as Prueba,
    'OK' as Estado,
    GETDATE() as FechaHora;
GO

PRINT '';
PRINT '=== INSTRUCCIONES ===';
PRINT 'Si ve este mensaje, la conexión funciona correctamente.';
PRINT '';
PRINT 'Credenciales para login:';
PRINT '  Usuario: usuario_ejemplo';
PRINT '  Contraseña: miPassword123';
PRINT '';
PRINT 'Connection String recomendada:';
PRINT '  Server=localhost;Database=Ramafemenina;Trusted_Connection=True;TrustServerCertificate=True;';
GO
