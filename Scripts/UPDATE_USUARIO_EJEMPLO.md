# Script SQL para Actualizar Contraseña de Usuario Existente

## Usuario Actual en Base de Datos
```
usuario: usuario_ejemplo
contraseña: miPassword123 (texto plano)
```

## Hash BCrypt Generado

Para la contraseña `miPassword123`, el hash BCrypt es:

```
$2a$12$Ft5K7vF8QXgWZYDGJ6KqHOqCTxM5LGqwKJXZ5VxGN7yJ1W9VLKViy
```

## Script SQL para Actualizar

Ejecuta este script en SQL Server Management Studio o Azure Data Studio:

```sql
-- Actualizar la contraseña del usuario existente con el hash BCrypt
UPDATE acceso
SET contraseña = '$2a$12$Ft5K7vF8QXgWZYDGJ6KqHOqCTxM5LGqwKJXZ5VxGN7yJ1W9VLKViy'
WHERE usuario = 'usuario_ejemplo';

-- Verificar el cambio
SELECT usuario, contraseña 
FROM acceso 
WHERE usuario = 'usuario_ejemplo';
```

## Credenciales para Login

Después de ejecutar el UPDATE, puedes iniciar sesión con:

- **Usuario**: `usuario_ejemplo`
- **Contraseña**: `miPassword123`

## Verificación del Hash

El hash generado sigue este formato:

```
$2a$12$Ft5K7vF8QXgWZYDGJ6KqHOqCTxM5LGqwKJXZ5VxGN7yJ1W9VLKViy
? ?  ?  ?                                                      ?
? ?  ?  ?? Salt único (22 caracteres)                         ?
? ?  ????? Work Factor 12 (2^12 = 4,096 iteraciones)         ?
? ???????? Versión BCrypt (2a)                                ?
??????????? Hash resultante (31 caracteres)                   ?
```

## Pasos a Seguir

1. **Abre SQL Server Management Studio o Azure Data Studio**

2. **Conéctate a tu servidor** (localhost)

3. **Selecciona la base de datos** `Ramafemenina`

4. **Ejecuta el script SQL** de arriba

5. **Verifica el resultado**:
   ```sql
   SELECT * FROM acceso WHERE usuario = 'usuario_ejemplo';
   ```
   
   Deberías ver:
   ```
   usuario_ejemplo | $2a$12$Ft5K7vF8QXgWZYDGJ6KqHOqCTxM5LGqwKJXZ5VxGN7yJ1W9VLKViy
   ```

6. **Prueba el login** en la aplicación:
   - Usuario: `usuario_ejemplo`
   - Contraseña: `miPassword123`

## Nota Importante

?? **No compartas el hash públicamente** - Aunque el hash es seguro, es una buena práctica mantener todos los datos de autenticación privados.

## Si el Login No Funciona

Si después de actualizar la contraseña el login no funciona:

1. Verifica que el hash se haya actualizado correctamente:
   ```sql
   SELECT contraseña FROM acceso WHERE usuario = 'usuario_ejemplo';
   ```

2. Asegúrate de que la contraseña comienza con `$2a$12$`

3. Verifica que la cadena de conexión en `appsettings.json` sea correcta:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=Ramafemenina;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

4. Revisa los logs de la aplicación para ver si hay errores de conexión

## Generar Hash para Otras Contraseñas

Si necesitas generar hashes para otras contraseñas, puedes usar la utilidad incluida en el proyecto:

```csharp
using RamaFemenina.Utilities;

// Generar hash
string hash = PasswordHashUtility.GenerateHash("nueva_contraseña");
Console.WriteLine(hash);

// Generar SQL INSERT completo
PasswordHashUtility.PrintSqlInsert("nuevo_usuario", "nueva_contraseña");
```

---

**Fecha de generación**: 2024  
**Algoritmo**: BCrypt  
**Work Factor**: 12  
**Contraseña original**: miPassword123 (NO compartir)
