-- ============================================================================
-- SCRIPT DE MIGRACION COMPLETA - RAMA FEMENINA
-- Ejecutar este script para realizar la migración completa de la base de datos
-- ============================================================================
-- IMPORTANTE: Realizar backup de la base de datos antes de ejecutar este script
-- ============================================================================

USE Ramafemenina;
GO

SET NOCOUNT ON;
GO

PRINT '============================================================================';
PRINT 'INICIANDO MIGRACION DE BASE DE DATOS - RAMA FEMENINA';
PRINT '============================================================================';
PRINT '';
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '';

-- ============================================================================
-- PASO 1: VERIFICACION INICIAL
-- ============================================================================
PRINT '------------------------------------------------------------';
PRINT 'PASO 1: VERIFICACION INICIAL';
PRINT '------------------------------------------------------------';
PRINT '';

-- Verificar tablas existentes
DECLARE @tablasExistentes TABLE (NombreTabla NVARCHAR(100));

INSERT INTO @tablasExistentes (NombreTabla)
SELECT name FROM sys.tables 
WHERE name IN ('acceso', 'Pacientes', 'Donaciones', 'Cheques', 'Clientes', 'Recibo');

PRINT 'Tablas encontradas:';
SELECT NombreTabla FROM @tablasExistentes;
PRINT '';

-- ============================================================================
-- PASO 2: CREAR/ACTUALIZAR TABLA RECIBO
-- ============================================================================
PRINT '------------------------------------------------------------';
PRINT 'PASO 2: CREAR/ACTUALIZAR TABLA RECIBO';
PRINT '------------------------------------------------------------';
PRINT '';

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Recibo') AND type = 'U')
BEGIN
    PRINT 'Creando tabla Recibo...';
    
    CREATE TABLE dbo.Recibo (
        NumeroRecibo INT IDENTITY(1,1) NOT NULL,
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
        Banco NVARCHAR(100) NULL,
        CONSTRAINT PK_Recibo PRIMARY KEY (NumeroRecibo)
    );
    
    PRINT '? Tabla Recibo creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'Tabla Recibo ya existe, verificando columnas...';
    
    -- Agregar TipoRecibo si no existe
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recibo') AND name = 'TipoRecibo')
    BEGIN
        PRINT '  Agregando columna TipoRecibo...';
        ALTER TABLE dbo.Recibo ADD TipoRecibo NVARCHAR(20) NOT NULL DEFAULT 'Ingreso';
        PRINT '  ? Columna TipoRecibo agregada.';
    END
    ELSE
    BEGIN
        PRINT '  ? Columna TipoRecibo ya existe.';
    END
    
    -- Agregar Cedula si no existe
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recibo') AND name = 'Cedula')
    BEGIN
        PRINT '  Agregando columna Cedula...';
        ALTER TABLE dbo.Recibo ADD Cedula NVARCHAR(20) NULL;
        PRINT '  ? Columna Cedula agregada.';
    END
    ELSE
    BEGIN
        PRINT '  ? Columna Cedula ya existe.';
    END
    
    -- Agregar MontoEnLetras si no existe
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Recibo') AND name = 'MontoEnLetras')
    BEGIN
        PRINT '  Agregando columna MontoEnLetras...';
        ALTER TABLE dbo.Recibo ADD MontoEnLetras NVARCHAR(500) NULL;
        PRINT '  ? Columna MontoEnLetras agregada.';
    END
    ELSE
    BEGIN
        PRINT '  ? Columna MontoEnLetras ya existe.';
    END
    
    -- Verificar si NumeroRecibo es IDENTITY
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.Recibo') 
        AND name = 'NumeroRecibo' 
        AND is_identity = 1
    )
    BEGIN
        PRINT '  NumeroRecibo no es IDENTITY, convirtiendo...';
        
        -- Crear tabla temporal
        CREATE TABLE dbo.Recibo_Temp (
            NumeroRecibo INT IDENTITY(1,1) NOT NULL,
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
            Banco NVARCHAR(100) NULL,
            CONSTRAINT PK_Recibo_Temp PRIMARY KEY (NumeroRecibo)
        );
        
        -- Copiar datos existentes
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
        
        -- Reemplazar tabla original
        DROP TABLE dbo.Recibo;
        EXEC sp_rename 'dbo.Recibo_Temp', 'Recibo';
        
        PRINT '  ? NumeroRecibo convertido a IDENTITY.';
    END
    ELSE
    BEGIN
        PRINT '  ? NumeroRecibo ya es IDENTITY.';
    END
END
PRINT '';

-- ============================================================================
-- PASO 3: VERIFICAR OTRAS TABLAS (OPCIONAL)
-- ============================================================================
PRINT '------------------------------------------------------------';
PRINT 'PASO 3: VERIFICAR COLUMNAS IDENTITY EN OTRAS TABLAS';
PRINT '------------------------------------------------------------';
PRINT '';

-- Verificar Donaciones.Iddonacion
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Donaciones') AND type = 'U')
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.Donaciones') 
        AND name = 'Iddonacion' 
        AND is_identity = 1
    )
    BEGIN
        PRINT 'ADVERTENCIA: Donaciones.Iddonacion no es IDENTITY.';
        PRINT 'Ejecutar Fix_Identity_Columns.sql para corregir.';
    END
    ELSE
    BEGIN
        PRINT '? Donaciones.Iddonacion es IDENTITY.';
    END
END
PRINT '';

-- Verificar Cheques.idCheque
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Cheques') AND type = 'U')
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.Cheques') 
        AND name = 'idCheque' 
        AND is_identity = 1
    )
    BEGIN
        PRINT 'ADVERTENCIA: Cheques.idCheque no es IDENTITY.';
        PRINT 'Ejecutar Fix_Identity_Columns.sql para corregir.';
    END
    ELSE
    BEGIN
        PRINT '? Cheques.idCheque es IDENTITY.';
    END
END
PRINT '';

-- Verificar Clientes.idCliente
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Clientes') AND type = 'U')
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.Clientes') 
        AND name = 'idCliente' 
        AND is_identity = 1
    )
    BEGIN
        PRINT 'ADVERTENCIA: Clientes.idCliente no es IDENTITY.';
        PRINT 'Ejecutar Fix_Identity_Columns.sql para corregir.';
    END
    ELSE
    BEGIN
        PRINT '? Clientes.idCliente es IDENTITY.';
    END
END
PRINT '';

-- ============================================================================
-- PASO 4: VERIFICACION FINAL
-- ============================================================================
PRINT '------------------------------------------------------------';
PRINT 'PASO 4: VERIFICACION FINAL';
PRINT '------------------------------------------------------------';
PRINT '';

-- Mostrar estructura de la tabla Recibo
PRINT 'Estructura de la tabla Recibo:';
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
PRINT '';

-- Contar registros
DECLARE @totalRecibos INT;
SELECT @totalRecibos = COUNT(*) FROM dbo.Recibo;
PRINT 'Total de recibos en la tabla: ' + CAST(@totalRecibos AS VARCHAR);
PRINT '';

-- ============================================================================
-- RESULTADO FINAL
-- ============================================================================
PRINT '============================================================================';
PRINT 'MIGRACION COMPLETADA EXITOSAMENTE';
PRINT '============================================================================';
PRINT '';
PRINT 'Siguiente paso:';
PRINT '1. Verificar que la aplicación puede conectarse a la base de datos';
PRINT '2. Probar la funcionalidad de crear, editar y eliminar recibos';
PRINT '3. Verificar que los recibos existentes se muestran correctamente';
PRINT '';
PRINT 'Si hay problemas con columnas IDENTITY en otras tablas,';
PRINT 'ejecutar el script: Fix_Identity_Columns.sql';
PRINT '';
PRINT '============================================================================';

SET NOCOUNT OFF;
GO
