using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RamaFemenina.Models;

namespace RamaFemenina.Data
{
    public class RamaFemeninaContext : DbContext
    {
        public DbSet<Acceso> Accesos { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Donaciones> Donaciones { get; set; }
        public DbSet<Cheques> Cheques { get; set; }
        public DbSet<Clientes> Clientes { get; set; }
        public DbSet<Recibo> Recibos { get; set; }
        public DbSet<CajaChica> CajaChicas { get; set; }
        public DbSet<Factura> Facturas { get; set; }

        public RamaFemeninaContext(DbContextOptions<RamaFemeninaContext> options)
            : base(options)
        {
            // Optimizaciones de rendimiento en el constructor
            try
            {
                // Configurar timeout para operaciones complejas
                Database.SetCommandTimeout(TimeSpan.FromSeconds(60));
                
                // Configurar change tracking para mejor rendimiento
                ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
                ChangeTracker.AutoDetectChangesEnabled = false;
                ChangeTracker.LazyLoadingEnabled = false;
                
                System.Diagnostics.Debug.WriteLine($"[CONTEXT] DbContext creado con optimizaciones de rendimiento");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CONTEXT] Error al configurar optimizaciones: {ex.Message}");
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            if (!optionsBuilder.IsConfigured)
            {
                return;
            }

            // Optimizaciones avanzadas de rendimiento para Entity Framework
            optionsBuilder
                #if DEBUG
                .LogTo(message => 
                    {
                        // Filtrar solo errores y warnings importantes
                        if (message.Contains("Error") || message.Contains("Warning") || message.Contains("Failed"))
                        {
                            System.Diagnostics.Debug.WriteLine($"[EF] {message}");
                        }
                    },
                    Microsoft.Extensions.Logging.LogLevel.Warning)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                #else
                .LogTo(_ => { }, Microsoft.Extensions.Logging.LogLevel.None)
                #endif
                
                // Optimizaciones críticas de rendimiento
                .EnableServiceProviderCaching(true)
                
                // Configurar warnings para mejorar rendimiento
                .ConfigureWarnings(warnings => warnings
                    .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.RowLimitingOperationWithoutOrderByWarning)
                    .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.FirstWithoutOrderByAndFilterWarning)
                    .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración optimizada de la tabla Acceso
            modelBuilder.Entity<Acceso>(entity =>
            {
                entity.ToTable("acceso");
                entity.HasKey(e => e.IdUsuario);
                entity.Property(e => e.IdUsuario)
                    .HasColumnName("idusuario")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn()
                    .IsRequired();
                entity.Property(e => e.Usuario)
                    .HasColumnName("usuario")
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasDefaultValue(string.Empty);
                entity.Property(e => e.Contraseña)
                    .HasColumnName("contraseña")
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasDefaultValue(string.Empty);
                
                // Índice único optimizado para consultas de login
                entity.HasIndex(e => e.Usuario)
                    .IsUnique()
                    .HasDatabaseName("IX_Acceso_Usuario")
                    .HasFilter(null);
            });

            // Configuración optimizada de la tabla Pacientes
            modelBuilder.Entity<Paciente>(entity =>
            {
                entity.ToTable("Pacientes");
                entity.HasKey(e => e.idpaciente);
                entity.Property(e => e.idpaciente)
                    .HasColumnName("idpaciente")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn()
                    .IsRequired();
                    
                // Optimizaciones de columnas de texto con valores por defecto
                entity.Property(e => e.cedula).HasColumnName("cedula").IsRequired().HasMaxLength(50).IsUnicode(false).HasDefaultValue(string.Empty);
                entity.Property(e => e.nombre).HasColumnName("nombre").IsRequired().HasMaxLength(50).IsUnicode(true).HasDefaultValue(string.Empty);
                entity.Property(e => e.telefono).HasColumnName("telefono").HasMaxLength(50).IsUnicode(false).HasDefaultValue(string.Empty);
                entity.Property(e => e.celular).HasColumnName("celular").HasMaxLength(50).IsUnicode(false).HasDefaultValue(string.Empty);
                entity.Property(e => e.estado).HasColumnName("estado").IsRequired().HasMaxLength(50).IsUnicode(false).HasDefaultValue("Activo");
                entity.Property(e => e.nrecord).HasColumnName("nrecord").IsRequired().HasMaxLength(50).IsUnicode(false).HasDefaultValue(string.Empty);
                entity.Property(e => e.observaciones).HasColumnName("observaciones").HasMaxLength(300).IsUnicode(true).HasDefaultValue(string.Empty);
                entity.Property(e => e.sexo).HasColumnName("sexo").HasMaxLength(50).IsUnicode(false).HasDefaultValue(string.Empty);
                entity.Property(e => e.area).HasColumnName("area").HasMaxLength(50).IsUnicode(false).HasDefaultValue(string.Empty);
                
                // Índices optimizados para consultas frecuentes
                entity.HasIndex(e => e.cedula)
                    .IsUnique()
                    .HasDatabaseName("IX_Pacientes_Cedula");
                    
                entity.HasIndex(e => e.nombre)
                    .HasDatabaseName("IX_Pacientes_Nombre");
                    
                entity.HasIndex(e => e.area)
                    .HasDatabaseName("IX_Pacientes_Area");
                    
                entity.HasIndex(e => e.estado)
                    .HasDatabaseName("IX_Pacientes_Estado");
            });

            // Configuración optimizada de la tabla Donaciones
            modelBuilder.Entity<Donaciones>(entity =>
            {
                entity.ToTable("Donaciones");
                entity.HasKey(e => e.idDonacion);
                entity.Property(e => e.idDonacion)
                    .HasColumnName("Iddonacion")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                    
                // Configuración optimizada de columnas monetarias
                entity.Property(e => e.Fecha).HasColumnName("fecha").IsRequired().HasColumnType("datetime2(0)").HasDefaultValue(DateTime.Now);
                entity.Property(e => e.valor).HasColumnName("valor").HasColumnType("decimal(18,2)").HasDefaultValue(0.00m);
                entity.Property(e => e.total).HasColumnName("total").HasColumnType("decimal(18,2)").HasDefaultValue(0.00m);
                entity.Property(e => e.montoSolicitado)
                    .HasColumnName("montoSolicitado")
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0.00m);
                    
                entity.Property(e => e.idPaciente).HasColumnName("idpaciente").IsRequired();
                entity.Property(e => e.procedimiento).HasColumnName("procedimiento").HasMaxLength(50).IsUnicode(false).HasDefaultValue(string.Empty);
                entity.Property(e => e.observacion).HasColumnName("observacion").HasMaxLength(300).IsUnicode(true).HasDefaultValue(string.Empty);
                
                // Relación optimizada con Paciente
                entity.HasOne(d => d.Paciente)
                    .WithMany()
                    .HasForeignKey(d => d.idPaciente)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Donaciones_Paciente");
                
                // Índices optimizados para consultas frecuentes
                entity.HasIndex(e => e.idPaciente)
                    .HasDatabaseName("IX_Donaciones_Paciente");
                    
                entity.HasIndex(e => e.Fecha)
                    .HasDatabaseName("IX_Donaciones_Fecha");
                    
                entity.HasIndex(e => e.procedimiento)
                    .HasDatabaseName("IX_Donaciones_Procedimiento");
                
                // Ignorar propiedades calculadas para el mapeo
                entity.Ignore(e => e.FechaFormateada);
                entity.Ignore(e => e.EstadoTexto);
                entity.Ignore(e => e.EstadoColor);
                entity.Ignore(e => e.Diferencia);
                entity.Ignore(e => e.PorcentajeCompletado);
            });

            // Configuración optimizada de la tabla Cheques con manejo mejorado de NULL
            modelBuilder.Entity<Cheques>(entity =>
            {
                entity.ToTable("cheques");
                entity.HasKey(e => e.idCheque);
                entity.Property(e => e.idCheque)
                    .HasColumnName("idcheque")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                    
                entity.Property(e => e.monto).HasColumnName("monto").HasColumnType("decimal(18,2)").IsRequired().HasDefaultValue(0.00m);
                entity.Property(e => e.Fecha).HasColumnName("fecha").IsRequired().HasColumnType("datetime2(0)").HasDefaultValue(DateTime.Now);
                entity.Property(e => e.nombre).HasColumnName("nombre").IsRequired().HasMaxLength(200).IsUnicode(true).HasDefaultValue(string.Empty);
                
                // Configuración especial para concepto - permitir NULL pero con conversión
                entity.Property(e => e.concepto)
                    .HasColumnName("concepto")
                    .HasMaxLength(200)
                    .IsUnicode(true)
                    .IsRequired(false) // Permitir NULL en base de datos
                    .HasConversion(
                        v => string.IsNullOrEmpty(v) ? null : v,  // Al guardar: string vacío -> NULL
                        v => v ?? string.Empty                    // Al leer: NULL -> string vacío
                    );
                    
                entity.Property(e => e.numero).HasColumnName("numero").IsRequired().HasMaxLength(50).IsUnicode(false).HasDefaultValue(string.Empty);
                
                // Índices optimizados
                entity.HasIndex(e => e.numero)
                    .IsUnique()
                    .HasDatabaseName("IX_Cheques_Numero");
                    
                entity.HasIndex(e => e.Fecha)
                    .HasDatabaseName("IX_CHEQUES_Fecha");
                    
                entity.HasIndex(e => e.nombre)
                    .HasDatabaseName("IX_Cheques_Nombre");
                
                entity.Ignore(e => e.FechaFormateada);
                entity.Ignore(e => e.ConceptoSeguro);
            });

            // Configuración optimizada de la tabla Clientes
            modelBuilder.Entity<Clientes>(entity =>
            {
                entity.ToTable("clientes");
                entity.HasKey(e => e.idCliente);
                entity.Property(e => e.idCliente)
                    .HasColumnName("idcliente")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                    
                entity.Property(e => e.nombre).HasColumnName("nombre").IsRequired().HasMaxLength(150).IsUnicode(true).HasDefaultValue(string.Empty);
                entity.Property(e => e.telefono).HasColumnName("telefono").IsRequired().HasMaxLength(50).IsUnicode(false).HasDefaultValue(string.Empty);
                entity.Property(e => e.direccion).HasColumnName("direccion").IsRequired().HasMaxLength(200).IsUnicode(true).HasDefaultValue(string.Empty);
                entity.Property(e => e.rnc).HasColumnName("rnc").HasMaxLength(50).IsUnicode(false).HasDefaultValue(string.Empty);
                
                // Índices optimizados
                entity.HasIndex(e => e.nombre)
                    .HasDatabaseName("IX_Clientes_Nombre");
                    
                entity.HasIndex(e => e.rnc)
                    .HasDatabaseName("IX_Clientes_RNC");
            });

            // Configuración optimizada de la tabla CajaChica
            modelBuilder.Entity<CajaChica>(entity =>
            {
                entity.ToTable("cajachica");
                entity.HasKey(e => e.IdRecibo);
                entity.Property(e => e.IdRecibo)
                    .HasColumnName("idrecibo")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                    
                entity.Property(e => e.NumeroRecibo).HasColumnName("recibo").IsRequired().HasDefaultValue(0);
                entity.Property(e => e.Fecha).HasColumnName("fecha").IsRequired().HasColumnType("datetime2(0)").HasDefaultValue(DateTime.Now);
                entity.Property(e => e.PagadoA).HasColumnName("nombre").IsRequired().HasMaxLength(50).IsUnicode(true).HasDefaultValue(string.Empty);
                entity.Property(e => e.Monto).HasColumnName("monto").HasColumnType("decimal(18,2)").IsRequired().HasDefaultValue(0.00m);
                entity.Property(e => e.ConCargoA).HasColumnName("cargoa").IsRequired().HasMaxLength(50).IsUnicode(false).HasDefaultValue(string.Empty);
                entity.Property(e => e.Concepto).HasColumnName("concepto").IsRequired().HasMaxLength(100).IsUnicode(true).HasDefaultValue(string.Empty);
                
                // Índices optimizados
                entity.HasIndex(e => e.Fecha)
                    .HasDatabaseName("IX_CajaChica_Fecha");
                    
                entity.HasIndex(e => e.PagadoA)
                    .HasDatabaseName("IX_CajaChica_PagadoA");
                
                // Ignorar propiedades calculadas
                entity.Ignore(e => e.FechaFormateada);
                entity.Ignore(e => e.MontoFormateado);
                entity.Ignore(e => e.MontoColor);
            });

            // Configuración optimizada de la tabla Recibo (tabla real: inrecibo)
            modelBuilder.Entity<Recibo>(entity =>
            {
                entity.ToTable("inrecibo");
                entity.HasKey(e => e.IdRecibo);
                entity.Property(e => e.IdRecibo)
                    .HasColumnName("idrecibo")
                    .ValueGeneratedNever(); // No es identity en la BD, se asigna manualmente (MAX+1)
                
                entity.Property(e => e.NumeroRecibo)
                    .HasColumnName("nrecibo")
                    .ValueGeneratedNever(); // Se asigna manualmente (MAX+1)
                    
                entity.Property(e => e.Fecha)
                    .HasColumnName("fecha")
                    .IsRequired()
                    .HasColumnType("datetime2(0)")
                    .HasDefaultValue(DateTime.Now);
                    
                entity.Property(e => e.RecibimosDe)
                    .HasColumnName("nombre")
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(true)
                    .HasDefaultValue(string.Empty);
                    
                entity.Property(e => e.Cedula)
                    .HasColumnName("cheque")
                    .HasMaxLength(50)
                    .IsUnicode(false);
                    
                entity.Property(e => e.Monto)
                    .HasColumnName("monto")
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0.00m);
                    
                entity.Property(e => e.Concepto)
                    .HasColumnName("concepto")
                    .HasMaxLength(200)
                    .IsUnicode(true)
                    .HasDefaultValue(string.Empty);
                    
                entity.Property(e => e.EsEfectivo)
                    .HasColumnName("efect")
                    .HasDefaultValue(false);
                    
                entity.Property(e => e.EsTransferencia)
                    .HasColumnName("trans")
                    .HasDefaultValue(false);
                    
                entity.Property(e => e.EsCheque)
                    .HasColumnName("cheq")
                    .HasDefaultValue(false);
                    
                entity.Property(e => e.NumeroFacturaNCF)
                    .HasColumnName("factura")
                    .HasMaxLength(100)
                    .IsUnicode(false);
                    
                entity.Property(e => e.Banco)
                    .HasColumnName("banco")
                    .HasMaxLength(100)
                    .IsUnicode(true);
                
                entity.HasIndex(e => e.Fecha).HasDatabaseName("IX_InRecibo_Fecha");
                entity.HasIndex(e => e.NumeroRecibo).HasDatabaseName("IX_InRecibo_NRecibo");
                entity.HasIndex(e => e.RecibimosDe).HasDatabaseName("IX_InRecibo_Nombre");
                
                entity.Ignore(e => e.MontoEnLetras);
                entity.Ignore(e => e.NumeroCheque);
                entity.Ignore(e => e.FechaFormateada);
                entity.Ignore(e => e.TipoPago);
                entity.Ignore(e => e.TipoPagoColor);
                entity.Ignore(e => e.DetallesPago);
                entity.Ignore(e => e.TipoRecibo);
                entity.Ignore(e => e.TipoReciboColor);
                entity.Ignore(e => e.TipoReciboIcono);
            });

            // Configuración de la tabla Factura (ESTRUCTURA REAL DE BD)
            modelBuilder.Entity<Factura>(entity =>
            {
                entity.ToTable("factura");

                // PRIMARY KEY CORRECTA - IdFactura es la PK Identity
                entity.HasKey(e => e.IdFactura);

                // idfactura - PK Identity principal (la única columna Identity)
                entity.Property(e => e.IdFactura)
                    .HasColumnName("idfactura")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn()
                    .IsRequired();

                // nofactura - mismo valor que idfactura (se asigna en código)
                entity.Property(e => e.NoFactura)
                    .HasColumnName("nofactura")
                    .IsRequired();

                entity.Property(e => e.Fecha)
                    .HasColumnName("fecha")
                    .HasColumnType("datetime")
                    .IsRequired();

                entity.Property(e => e.IdCliente)
                    .HasColumnName("idcliente");

                entity.Property(e => e.Exento)
                    .HasColumnName("exento")
                    .HasColumnType("money")
                    .HasDefaultValue(0.00m)
                    .IsRequired();

                entity.Property(e => e.Gravado)
                    .HasColumnName("gravado")
                    .HasColumnType("money")
                    .HasDefaultValue(0.00m)
                    .IsRequired();

                entity.Property(e => e.Itbis)
                    .HasColumnName("itbis")
                    .HasColumnType("money")
                    .HasDefaultValue(0.00m)
                    .IsRequired();

                entity.Property(e => e.APagar)
                    .HasColumnName("apagar")
                    .HasColumnType("money")
                    .HasDefaultValue(0.00m)
                    .IsRequired();

                entity.Property(e => e.EsCredito)
                    .HasColumnName("cred")
                    .HasDefaultValue(false)
                    .IsRequired();

                entity.Property(e => e.EsEfectivo)
                    .HasColumnName("efec")
                    .HasDefaultValue(false)
                    .IsRequired();

                entity.Property(e => e.EsCheque)
                    .HasColumnName("cheq")
                    .HasDefaultValue(false)
                    .IsRequired();

                entity.Property(e => e.NumeroCheque)
                    .HasColumnName("cheque")
                    .HasMaxLength(50);

                entity.Property(e => e.Banco)
                    .HasColumnName("banco")
                    .HasMaxLength(50);

                entity.Property(e => e.FechaPago)
                    .HasColumnName("fechapago")
                    .HasColumnType("datetime");

                entity.Property(e => e.Pago)
                    .HasColumnName("pago")
                    .HasColumnType("money")
                    .HasDefaultValue(0.00m);

                entity.Property(e => e.NCFNumerico)
                    .HasColumnName("ncf");

                entity.Property(e => e.Cambio)
                    .HasColumnName("cambio")
                    .HasColumnType("money")
                    .HasDefaultValue(0.00m);

                entity.Property(e => e.TCFNumerico)
                    .HasColumnName("tcf");

                entity.Property(e => e.NulaTexto)
                    .HasColumnName("nula")
                    .HasMaxLength(2)
                    .HasDefaultValue("NO");

                entity.Property(e => e.FechaVencimientoTexto)
                    .HasColumnName("fechav2")
                    .HasMaxLength(10);

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.IdCliente)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índices (basados en estructura real)
                entity.HasIndex(e => e.Fecha).HasDatabaseName("IX_Factura_Fecha");
                entity.HasIndex(e => e.IdCliente).HasDatabaseName("IX_Factura_Cliente");
                entity.HasIndex(e => e.NulaTexto).HasDatabaseName("IX_Factura_Nula");
                
                // Ignorar TODAS las propiedades calculadas
                entity.Ignore(e => e.Numero);
                entity.Ignore(e => e.Total);
                entity.Ignore(e => e.Observaciones);
                entity.Ignore(e => e.Estado);
                entity.Ignore(e => e.EsNula);
                entity.Ignore(e => e.NCF);
                entity.Ignore(e => e.TCF);
                entity.Ignore(e => e.FechaVencimiento);
                entity.Ignore(e => e.FechaFormateada);
                entity.Ignore(e => e.NombreCliente);
                entity.Ignore(e => e.EstadoPago);
                entity.Ignore(e => e.EstadoPagoColor);
                entity.Ignore(e => e.EstadoPagoIcono);
                entity.Ignore(e => e.TipoPago);
                entity.Ignore(e => e.TipoPagoColor);
                entity.Ignore(e => e.Pendiente);
                entity.Ignore(e => e.PorcentajePagado);
                entity.Ignore(e => e.PorcentajePagadoDouble);
                entity.Ignore(e => e.DetallesPago);
                entity.Ignore(e => e.MontoFormateado);
                entity.Ignore(e => e.PagoFormateado);
                entity.Ignore(e => e.PendienteFormateado);
                entity.Ignore(e => e.NumeroFacturaFormateado);
                entity.Ignore(e => e.FechaPagoFormateada);
                entity.Ignore(e => e.FechaVencimientoFormateada);
                entity.Ignore(e => e.EstadoTexto);
            });

            System.Diagnostics.Debug.WriteLine($"[CONTEXT] Modelo configurado correctamente con optimizaciones y manejo de NULL para DonacionesDB");
            System.Diagnostics.Debug.WriteLine($"[CONTEXT] Entidades configuradas con índices optimizados y valores por defecto: Acceso, Paciente, Donaciones, Cheques, Clientes, CajaChica, Recibo (inrecibo), Factura");
        }
    }
}
