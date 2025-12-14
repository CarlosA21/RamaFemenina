using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RamaFemenina.Data;
using RamaFemenina.Models;

namespace RamaFemenina.Services
{
    public class DataCacheService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, object> _cache = new();
        private readonly ConcurrentDictionary<string, DateTime> _cacheTimestamps = new();
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public DataCacheService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private bool IsCacheValid(string key)
        {
            return _cacheTimestamps.TryGetValue(key, out var timestamp) &&
                   DateTime.UtcNow - timestamp < _cacheExpiration;
        }

        private void SetCache<T>(string key, T data)
        {
            _cache[key] = data;
            _cacheTimestamps[key] = DateTime.UtcNow;
        }

        public void InvalidateCache(string pattern = null)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                _cache.Clear();
                _cacheTimestamps.Clear();
                return;
            }

            var keysToRemove = _cache.Keys.Where(k => k.Contains(pattern)).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
                _cacheTimestamps.TryRemove(key, out _);
            }
        }

        public async Task<IEnumerable<T>> GetCachedDataAsync<T>(
            string cacheKey,
            Func<Task<IEnumerable<T>>> dataProvider,
            CancellationToken cancellationToken = default)
        {
            if (IsCacheValid(cacheKey) && _cache.TryGetValue(cacheKey, out var cachedData))
            {
                return (IEnumerable<T>)cachedData;
            }

            var data = await dataProvider();
            SetCache(cacheKey, data);
            return data;
        }

        // Métodos específicos optimizados para cada entidad
        public async Task<IEnumerable<Paciente>> GetPacientesPaginatedAsync(
            int page = 1, 
            int pageSize = 50, 
            string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"pacientes_{page}_{pageSize}_{searchTerm}";
            
            return await GetCachedDataAsync<Paciente>(cacheKey, async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                
                var query = context.Pacientes.AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Búsqueda optimizada por todos los campos relevantes
                    query = query.Where(p => 
                        EF.Functions.Like(p.nombre, $"%{searchTerm}%") ||
                        EF.Functions.Like(p.cedula, $"%{searchTerm}%") ||
                        EF.Functions.Like(p.nrecord, $"%{searchTerm}%") ||
                        EF.Functions.Like(p.telefono, $"%{searchTerm}%") ||
                        EF.Functions.Like(p.celular, $"%{searchTerm}%") ||
                        EF.Functions.Like(p.estado, $"%{searchTerm}%") ||
                        EF.Functions.Like(p.area, $"%{searchTerm}%") ||
                        EF.Functions.Like(p.sexo, $"%{searchTerm}%") ||
                        EF.Functions.Like(p.observaciones, $"%{searchTerm}%"));
                }

                var pacientes = await query
                    .OrderBy(p => p.nombre)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                // Crear nuevas instancias desconectadas completamente
                return pacientes.Select(p => new Paciente
                {
                    idpaciente = p.idpaciente,
                    cedula = p.cedula,
                    nombre = p.nombre,
                    telefono = p.telefono,
                    celular = p.celular,
                    estado = p.estado,
                    nrecord = p.nrecord,
                    observaciones = p.observaciones,
                    sexo = p.sexo,
                    area = p.area
                }).ToList();
            }, cancellationToken);
        }

        public async Task<int> GetPacientesTotalCountAsync(
            string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            // No usar caché para el conteo de pacientes para evitar que TotalPages quede obsoleto
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();

            var query = context.Pacientes.AsNoTracking();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Búsqueda optimizada por todos los campos relevantes
                query = query.Where(p =>
                    EF.Functions.Like(p.nombre, $"%{searchTerm}%") ||
                    EF.Functions.Like(p.cedula, $"%{searchTerm}%") ||
                    EF.Functions.Like(p.nrecord, $"%{searchTerm}%") ||
                    EF.Functions.Like(p.telefono, $"%{searchTerm}%") ||
                    EF.Functions.Like(p.celular, $"%{searchTerm}%") ||
                    EF.Functions.Like(p.estado, $"%{searchTerm}%") ||
                    EF.Functions.Like(p.area, $"%{searchTerm}%") ||
                    EF.Functions.Like(p.sexo, $"%{searchTerm}%") ||
                    EF.Functions.Like(p.observaciones, $"%{searchTerm}%"));
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<Donaciones>> GetDonacionesPaginatedAsync(
            int page = 1, 
            int pageSize = 50, 
            string searchTerm = "",
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"donaciones_{page}_{pageSize}_{searchTerm}_{fechaInicio}_{fechaFin}";
            
            System.Diagnostics.Debug.WriteLine($"[CACHE] GetDonacionesPaginatedAsync - Iniciando carga. Page: {page}, PageSize: {pageSize}, SearchTerm: '{searchTerm}'");
            
            return await GetCachedDataAsync<Donaciones>(cacheKey, async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[CACHE] Cache miss o expirado para: {cacheKey}");
                    
                    using var scope = _serviceProvider.CreateScope();
                    using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                    
                    System.Diagnostics.Debug.WriteLine($"[CACHE] DbContext creado correctamente");
                    
                    // Usar una consulta proyectada en lugar de Include para evitar proxies de EF
                    var query = from d in context.Donaciones.AsNoTracking()
                                join p in context.Pacientes.AsNoTracking() on d.idPaciente equals p.idpaciente into pacienteGroup
                                from paciente in pacienteGroup.DefaultIfEmpty()
                                select new { Donacion = d, Paciente = paciente };

                    System.Diagnostics.Debug.WriteLine($"[CACHE] Query base creada");

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        System.Diagnostics.Debug.WriteLine($"[CACHE] Aplicando filtro de búsqueda: '{searchTerm}'");
                        query = query.Where(x => 
                            EF.Functions.Like(x.Donacion.procedimiento, $"%{searchTerm}%") ||
                            EF.Functions.Like(x.Donacion.observacion, $"%{searchTerm}%") ||
                            x.Donacion.montoSolicitado.ToString().Contains(searchTerm) ||
                            x.Donacion.total.ToString().Contains(searchTerm) ||
                            x.Donacion.valor.ToString().Contains(searchTerm) ||
                            (x.Paciente != null && (
                                EF.Functions.Like(x.Paciente.nombre, $"%{searchTerm}%") ||
                                EF.Functions.Like(x.Paciente.cedula, $"%{searchTerm}%") ||
                                EF.Functions.Like(x.Paciente.nrecord, $"%{searchTerm}%")
                            )));
                    }

                    if (fechaInicio.HasValue)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CACHE] Aplicando filtro fecha inicio: {fechaInicio.Value}");
                        query = query.Where(x => x.Donacion.Fecha >= fechaInicio.Value);
                    }

                    if (fechaFin.HasValue)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CACHE] Aplicando filtro fecha fin: {fechaFin.Value}");
                        query = query.Where(x => x.Donacion.Fecha <= fechaFin.Value);
                    }

                    System.Diagnostics.Debug.WriteLine($"[CACHE] Ejecutando query contra la base de datos...");
                    
                    // Materializar y crear instancias desconectadas
                    var resultado = await query
                        .OrderByDescending(x => x.Donacion.Fecha)
                        .ThenByDescending(x => x.Donacion.idDonacion)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync(cancellationToken);

                    System.Diagnostics.Debug.WriteLine($"[CACHE] Query ejecutada. Registros obtenidos: {resultado.Count}");

                    // Crear nuevas instancias completamente desconectadas de EF
                    var donacionesDesconectadas = resultado.Select(x => new Donaciones
                    {
                        idDonacion = x.Donacion.idDonacion,
                        Fecha = x.Donacion.Fecha,
                        valor = x.Donacion.valor,
                        total = x.Donacion.total,
                        idPaciente = x.Donacion.idPaciente,
                        procedimiento = x.Donacion.procedimiento,
                        observacion = x.Donacion.observacion,
                        montoSolicitado = x.Donacion.montoSolicitado,
                        Paciente = x.Paciente != null ? new Paciente
                        {
                            idpaciente = x.Paciente.idpaciente,
                            cedula = x.Paciente.cedula ?? string.Empty,
                            nombre = x.Paciente.nombre ?? string.Empty,
                            telefono = x.Paciente.telefono ?? string.Empty,
                            celular = x.Paciente.celular ?? string.Empty,
                            estado = x.Paciente.estado ?? string.Empty,
                            nrecord = x.Paciente.nrecord ?? string.Empty,
                            observaciones = x.Paciente.observaciones ?? string.Empty,
                            sexo = x.Paciente.sexo ?? string.Empty,
                            area = x.Paciente.area ?? string.Empty
                        } : null
                    }).ToList();

                    System.Diagnostics.Debug.WriteLine($"[CACHE] Instancias desconectadas creadas: {donacionesDesconectadas.Count}");
                    
                    // Verificar que las instancias estén completamente desconectadas
                    foreach (var d in donacionesDesconectadas)
                    {
                        if (d.Paciente != null)
                        {
                            var nombrePaciente = d.Paciente.nombre; // Forzar acceso
                            System.Diagnostics.Debug.WriteLine($"[CACHE] Donación {d.idDonacion} - Paciente: {nombrePaciente}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[CACHE] Donación {d.idDonacion} - Sin paciente asociado");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[CACHE] GetDonacionesPaginatedAsync - Completado exitosamente");
                    return donacionesDesconectadas;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CACHE] ERROR en GetDonacionesPaginatedAsync: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[CACHE] StackTrace: {ex.StackTrace}");
                    // Evitar que un fallo de DB cierre la app: devolver lista vacía
                    return new List<Donaciones>();
                }
            }, cancellationToken);
        }

        public async Task<int> GetDonacionesTotalCountAsync(
            string searchTerm = "",
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"donaciones_count_{searchTerm}_{fechaInicio}_{fechaFin}";
            
            var result = await GetCachedDataAsync<int>(cacheKey, async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                
                IQueryable<Donaciones> query = context.Donaciones.AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Búsqueda optimizada - evitar Include para count
                    query = query.Where(d => 
                        EF.Functions.Like(d.procedimiento, $"%{searchTerm}%") ||
                        EF.Functions.Like(d.observacion, $"%{searchTerm}%") ||
                        d.montoSolicitado.ToString().Contains(searchTerm) ||
                        d.total.ToString().Contains(searchTerm) ||
                        d.valor.ToString().Contains(searchTerm) ||
                        context.Pacientes.Any(p => p.idpaciente == d.idPaciente && (
                            EF.Functions.Like(p.nombre, $"%{searchTerm}%") ||
                            EF.Functions.Like(p.cedula, $"%{searchTerm}%") ||
                            EF.Functions.Like(p.nrecord, $"%{searchTerm}%")
                        )));
                }

                if (fechaInicio.HasValue)
                {
                    query = query.Where(d => d.Fecha >= fechaInicio.Value);
                }

                if (fechaFin.HasValue)
                {
                    query = query.Where(d => d.Fecha <= fechaFin.Value);
                }

                return new[] { await query.CountAsync(cancellationToken) };
            }, cancellationToken);

            return result.First();
        }

        public async Task<IEnumerable<Clientes>> GetClientesPaginatedAsync(
            int page = 1, 
            int pageSize = 50, 
            string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"clientes_{page}_{pageSize}_{searchTerm}";
            
            return await GetCachedDataAsync<Clientes>(cacheKey, async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                
                var query = context.Clientes.AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Búsqueda optimizada por todos los campos
                    query = query.Where(c => 
                        EF.Functions.Like(c.nombre, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.telefono, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.rnc, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.direccion, $"%{searchTerm}%"));
                }

                return await query
                    .OrderBy(c => c.nombre)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }, cancellationToken);
        }

        public async Task<int> GetClientesTotalCountAsync(
            string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"clientes_count_{searchTerm}";
            
            var result = await GetCachedDataAsync<int>(cacheKey, async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                
                var query = context.Clientes.AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Búsqueda optimizada por todos los campos
                    query = query.Where(c => 
                        EF.Functions.Like(c.nombre, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.telefono, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.rnc, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.direccion, $"%{searchTerm}%"));
                }

                return new[] { await query.CountAsync(cancellationToken) };
            }, cancellationToken);

            return result.First();
        }

        public async Task<IEnumerable<Cheques>> GetChequesPaginatedAsync(
            int page = 1, 
            int pageSize = 50, 
            string searchTerm = "",
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"cheques_{page}_{pageSize}_{searchTerm}_{fechaInicio}_{fechaFin}";
            
            return await GetCachedDataAsync<Cheques>(cacheKey, async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                
                var query = context.Cheques.AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Búsqueda optimizada incluyendo monto y número de cheque
                    query = query.Where(c => 
                        EF.Functions.Like(c.nombre, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.numero, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.concepto, $"%{searchTerm}%") ||
                        c.monto.ToString().Contains(searchTerm));
                }

                if (fechaInicio.HasValue)
                {
                    query = query.Where(c => c.Fecha >= fechaInicio.Value);
                }

                if (fechaFin.HasValue)
                {
                    query = query.Where(c => c.Fecha <= fechaFin.Value);
                }

                return await query
                    .OrderByDescending(c => c.Fecha)
                    .ThenByDescending(c => c.idCheque)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }, cancellationToken);
        }

        public async Task<int> GetChequesTotalCountAsync(
            string searchTerm = "",
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"cheques_count_{searchTerm}_{fechaInicio}_{fechaFin}";
            
            var result = await GetCachedDataAsync<int>(cacheKey, async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                
                var query = context.Cheques.AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Búsqueda optimizada incluyendo monto y número de cheque
                    query = query.Where(c => 
                        EF.Functions.Like(c.nombre, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.numero, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.concepto, $"%{searchTerm}%") ||
                        c.monto.ToString().Contains(searchTerm));
                }

                if (fechaInicio.HasValue)
                {
                    query = query.Where(c => c.Fecha >= fechaInicio.Value);
                }

                if (fechaFin.HasValue)
                {
                    query = query.Where(c => c.Fecha <= fechaFin.Value);
                }

                return new[] { await query.CountAsync(cancellationToken) };
            }, cancellationToken);

            return result.First();
        }

        public async Task<IEnumerable<CajaChica>> GetCajaChicaPaginatedAsync(
            int page = 1, 
            int pageSize = 50, 
            string searchTerm = "",
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"cajachica_{page}_{pageSize}_{searchTerm}_{fechaInicio}_{fechaFin}";
            
            return await GetCachedDataAsync<CajaChica>(cacheKey, async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                
                var query = context.CajaChicas.AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Búsqueda optimizada incluyendo número de recibo y monto
                    query = query.Where(c => 
                        c.NumeroRecibo.ToString().Contains(searchTerm) ||
                        EF.Functions.Like(c.PagadoA, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.ConCargoA, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.Concepto, $"%{searchTerm}%") ||
                        c.Monto.ToString().Contains(searchTerm));
                }

                if (fechaInicio.HasValue)
                {
                    query = query.Where(c => c.Fecha >= fechaInicio.Value);
                }

                if (fechaFin.HasValue)
                {
                    query = query.Where(c => c.Fecha <= fechaFin.Value);
                }

                return await query
                    .OrderByDescending(c => c.Fecha)
                    .ThenByDescending(c => c.NumeroRecibo)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }, cancellationToken);
        }

        public async Task<int> GetCajaChicaTotalCountAsync(
            string searchTerm = "",
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"cajachica_count_{searchTerm}_{fechaInicio}_{fechaFin}";
            
            var result = await GetCachedDataAsync<int>(cacheKey, async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                
                var query = context.CajaChicas.AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Búsqueda optimizada incluyendo número de recibo y monto
                    query = query.Where(c => 
                        c.NumeroRecibo.ToString().Contains(searchTerm) ||
                        EF.Functions.Like(c.PagadoA, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.ConCargoA, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.Concepto, $"%{searchTerm}%") ||
                        c.Monto.ToString().Contains(searchTerm));
                }

                if (fechaInicio.HasValue)
                {
                    query = query.Where(c => c.Fecha >= fechaInicio.Value);
                }

                if (fechaFin.HasValue)
                {
                    query = query.Where(c => c.Fecha <= fechaFin.Value);
                }

                return new[] { await query.CountAsync(cancellationToken) };
            }, cancellationToken);

            return result.First();
        }

        public async Task<DonacionesStatsDto> GetDonacionesStatsAsync(CancellationToken cancellationToken = default)
        {
            var cacheKey = "donaciones_stats";
            
            var result = await GetCachedDataAsync<DonacionesStatsDto>(cacheKey, async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                
                var stats = await context.Donaciones
                    .AsNoTracking()
                    .GroupBy(d => 1)
                    .Select(g => new DonacionesStatsDto
                    {
                        TotalDonaciones = g.Count(),
                        TotalSolicitado = g.Sum(d => d.montoSolicitado),
                        TotalDonado = g.Sum(d => d.total),
                        PromedioSolicitado = g.Average(d => d.montoSolicitado),
                        PromedioDonado = g.Average(d => d.total)
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                return new[] { stats ?? new DonacionesStatsDto() };
            }, cancellationToken);

            return result.First();
        }

        public async Task<IEnumerable<Recibo>> GetRecibosPaginatedAsync(
            int page = 1, 
            int pageSize = 50, 
            string searchTerm = "",
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"recibos_{page}_{pageSize}_{searchTerm}_{fechaInicio}_{fechaFin}";
            
            return await GetCachedDataAsync<Recibo>(cacheKey, async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                
                var query = context.Recibos.AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Búsqueda por campos mapeados de la base de datos
                    query = query.Where(r => 
                        r.NumeroRecibo.ToString().Contains(searchTerm) ||
                        EF.Functions.Like(r.RecibimosDe, $"%{searchTerm}%") ||
                        EF.Functions.Like(r.Concepto, $"%{searchTerm}%") ||
                        EF.Functions.Like(r.Cedula, $"%{searchTerm}%") ||
                        EF.Functions.Like(r.Banco, $"%{searchTerm}%") ||
                        EF.Functions.Like(r.NumeroFacturaNCF, $"%{searchTerm}%") ||
                        r.Monto.ToString().Contains(searchTerm));
                }

                if (fechaInicio.HasValue)
                {
                    query = query.Where(r => r.Fecha >= fechaInicio.Value);
                }

                if (fechaFin.HasValue)
                {
                    query = query.Where(r => r.Fecha <= fechaFin.Value);
                }

                return await query
                    .OrderByDescending(r => r.Fecha)
                    .ThenByDescending(r => r.NumeroRecibo)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }, cancellationToken);
        }

        public async Task<int> GetRecibosTotalCountAsync(
            string searchTerm = "",
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"recibos_count_{searchTerm}_{fechaInicio}_{fechaFin}";
            
            var result = await GetCachedDataAsync<int>(cacheKey, async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RamaFemeninaContext>();
                
                var query = context.Recibos.AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Búsqueda por campos mapeados de la base de datos
                    query = query.Where(r => 
                        r.NumeroRecibo.ToString().Contains(searchTerm) ||
                        EF.Functions.Like(r.RecibimosDe, $"%{searchTerm}%") ||
                        EF.Functions.Like(r.Concepto, $"%{searchTerm}%") ||
                        EF.Functions.Like(r.Cedula, $"%{searchTerm}%") ||
                        EF.Functions.Like(r.Banco, $"%{searchTerm}%") ||
                        EF.Functions.Like(r.NumeroFacturaNCF, $"%{searchTerm}%") ||
                        r.Monto.ToString().Contains(searchTerm));
                }

                if (fechaInicio.HasValue)
                {
                    query = query.Where(r => r.Fecha >= fechaInicio.Value);
                }

                if (fechaFin.HasValue)
                {
                    query = query.Where(r => r.Fecha <= fechaFin.Value);
                }

                return new[] { await query.CountAsync(cancellationToken) };
            }, cancellationToken);

            return result.First();
        }
    }

    public class DonacionesStatsDto
    {
        public int TotalDonaciones { get; set; }
        public decimal TotalSolicitado { get; set; }
        public decimal TotalDonado { get; set; }
        public decimal PromedioSolicitado { get; set; }
        public decimal PromedioDonado { get; set; }
        public decimal Diferencia => TotalSolicitado - TotalDonado;
        public decimal PorcentajeCompletado => TotalSolicitado > 0 ? (TotalDonado / TotalSolicitado) * 100 : 0;
    }
}