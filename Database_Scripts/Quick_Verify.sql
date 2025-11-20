-- Script de Verificación Rápida Post-Corrección
-- Ejecuta este script para verificar que todo está OK

USE Ramafemenina;
GO

PRINT '========================================================================';
PRINT 'VERIFICACIÓN RÁPIDA DE COLUMNAS IDENTITY';
PRINT '========================================================================';
PRINT '';

-- Verificación simple
SELECT 
    'Tabla' = OBJECT_NAME(object_id),
    'Columna' = name,
    'Es Identity' = CASE WHEN is_identity = 1 THEN 'SÍ ?' ELSE 'NO ?' END,
    'Tipo' = TYPE_NAME(user_type_id),
    'Valor Actual' = IDENT_CURRENT(OBJECT_NAME(object_id))
FROM sys.columns 
WHERE 
    (object_id = OBJECT_ID('dbo.Donaciones') AND name = 'Iddonacion')
    OR (object_id = OBJECT_ID('dbo.Cheques') AND name = 'idCheque')
    OR (object_id = OBJECT_ID('dbo.Clientes') AND name = 'idCliente')
    OR (object_id = OBJECT_ID('dbo.inrecibo') AND name = 'nrecibo')
    OR (object_id = OBJECT_ID('dbo.acceso') AND name = 'idusuario')
ORDER BY OBJECT_NAME(object_id);

PRINT '';
PRINT '========================================================================';
PRINT 'Si todas las columnas muestran "SÍ ?", ¡todo está perfecto!';
PRINT '========================================================================';
GO
