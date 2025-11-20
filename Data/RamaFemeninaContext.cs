using System;
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

        public RamaFemeninaContext(DbContextOptions<RamaFemeninaContext> options)
            : base(options)
        {
            // Log para debug
            try
            {
                var connectionString = Database.GetConnectionString();
                System.Diagnostics.Debug.WriteLine($"[CONTEXT] DbContext creado");
                System.Diagnostics.Debug.WriteLine($"[CONTEXT] Connection String: {connectionString}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CONTEXT] Error al obtener connection string: {ex.Message}");
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            // Habilitar logging detallado para debug
            optionsBuilder.LogTo(message => 
                System.Diagnostics.Debug.WriteLine($"[EF] {message}"),
                Microsoft.Extensions.Logging.LogLevel.Information);
            
            // Habilitar logging de datos sensibles (solo para debug)
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la tabla Acceso
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
                    .HasMaxLength(50);
                entity.Property(e => e.Contraseña)
                    .HasColumnName("contraseña")
                    .IsRequired()
                    .HasMaxLength(100);
                
                // Índice único para el usuario
                entity.HasIndex(e => e.Usuario).IsUnique();
            });

            // Configuración de la tabla Paciente
            modelBuilder.Entity<Paciente>(entity =>
            {
                entity.ToTable("Pacientes");
                entity.HasKey(e => e.cedula);
                entity.Property(e => e.cedula).HasColumnName("cedula").IsRequired().HasMaxLength(50);
                entity.Property(e => e.nombre).HasColumnName("nombre").IsRequired().HasMaxLength(50);
                entity.Property(e => e.telefono).HasColumnName("telefono").HasMaxLength(50);
                entity.Property(e => e.celular).HasColumnName("celular").HasMaxLength(50);
                entity.Property(e => e.nrecord).HasColumnName("nrecord").IsRequired().HasMaxLength(50);
                entity.Property(e => e.observaciones).HasColumnName("observaciones").HasMaxLength(300);
                entity.Property(e => e.sexo).HasColumnName("sexo").HasMaxLength(50);
                entity.Property(e => e.area).HasColumnName("area").HasMaxLength(50);
            });

            // Configuración de la tabla Donaciones
            modelBuilder.Entity<Donaciones>(entity =>
            {
                entity.ToTable("Donaciones");
                entity.HasKey(e => e.idDonacion);
                entity.Property(e => e.idDonacion)
                    .HasColumnName("Iddonacion")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                entity.Property(e => e.Fecha).HasColumnName("Fecha").IsRequired();
                entity.Property(e => e.valor).HasColumnName("valor").HasColumnType("decimal(18,2)");
                entity.Property(e => e.total).HasColumnName("total").HasColumnType("decimal(18,2)");
                entity.Property(e => e.idPaciente).HasColumnName("idPaciente").IsRequired();
                entity.Property(e => e.procedimiento).HasColumnName("procedimiento");
                entity.Property(e => e.observacion).HasColumnName("observacion");
                entity.Property(e => e.montoSolicitado).HasColumnName("montoSolicitado").HasColumnType("decimal(18,2)");
                
                // Relación con Paciente
                entity.HasOne<Paciente>()
                    .WithMany()
                    .HasForeignKey(e => e.idPaciente)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Ignorar propiedades calculadas
                entity.Ignore(e => e.FechaFormateada);
                entity.Ignore(e => e.EstadoTexto);
                entity.Ignore(e => e.EstadoColor);
                entity.Ignore(e => e.Diferencia);
                entity.Ignore(e => e.PorcentajeCompletado);
            });

            // Configuración de la tabla Cheques
            modelBuilder.Entity<Cheques>(entity =>
            {
                entity.ToTable("Cheques");
                entity.HasKey(e => e.idCheque);
                entity.Property(e => e.idCheque)
                    .HasColumnName("idCheque")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                entity.Property(e => e.monto).HasColumnName("monto").HasColumnType("decimal(18,2)");
                entity.Property(e => e.Fecha).HasColumnName("Fecha").IsRequired();
                entity.Property(e => e.nombre).HasColumnName("nombre");
                entity.Property(e => e.concepto).HasColumnName("concepto");
                entity.Property(e => e.numero).HasColumnName("numero");
                
                // Ignorar propiedades calculadas
                entity.Ignore(e => e.FechaFormateada);
            });

            // Configuración de la tabla Clientes
            modelBuilder.Entity<Clientes>(entity =>
            {
                entity.ToTable("Clientes");
                entity.HasKey(e => e.idCliente);
                entity.Property(e => e.idCliente)
                    .HasColumnName("idCliente")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                entity.Property(e => e.nombre).HasColumnName("nombre").IsRequired();
                entity.Property(e => e.telefono).HasColumnName("telefono");
                entity.Property(e => e.direccion).HasColumnName("direccion");
                entity.Property(e => e.rnc).HasColumnName("rnc");
            });

            // Configuración de la tabla Recibo
            modelBuilder.Entity<Recibo>(entity =>
            {
                entity.ToTable("Recibo");
                entity.HasKey(e => e.NumeroRecibo);
                entity.Property(e => e.NumeroRecibo)
                    .HasColumnName("NumeroRecibo")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                entity.Property(e => e.TipoRecibo).HasColumnName("TipoRecibo").IsRequired();
                entity.Property(e => e.Fecha).HasColumnName("Fecha").IsRequired();
                entity.Property(e => e.RecibimosDe).HasColumnName("RecibimosDe").IsRequired();
                entity.Property(e => e.Cedula).HasColumnName("Cedula");
                entity.Property(e => e.Monto).HasColumnName("Monto").HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontoEnLetras).HasColumnName("MontoEnLetras");
                entity.Property(e => e.Concepto).HasColumnName("Concepto");
                entity.Property(e => e.EsEfectivo).HasColumnName("EsEfectivo");
                entity.Property(e => e.EsTransferencia).HasColumnName("EsTransferencia");
                entity.Property(e => e.EsCheque).HasColumnName("EsCheque");
                entity.Property(e => e.NumeroFacturaNCF).HasColumnName("NumeroFacturaNCF");
                entity.Property(e => e.NumeroCheque).HasColumnName("NumeroCheque");
                entity.Property(e => e.Banco).HasColumnName("Banco");
                
                // Ignorar propiedades calculadas
                entity.Ignore(e => e.FechaFormateada);
                entity.Ignore(e => e.TipoPago);
                entity.Ignore(e => e.TipoPagoColor);
                entity.Ignore(e => e.DetallesPago);
                entity.Ignore(e => e.TipoReciboColor);
                entity.Ignore(e => e.TipoReciboIcono);
            });

            // Configuración de la tabla CajaChica
            modelBuilder.Entity<CajaChica>(entity =>
            {
                entity.ToTable("CajaChica");
                entity.HasKey(e => e.IdRecibo);
                entity.Property(e => e.IdRecibo)
                    .HasColumnName("idrecibo")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                entity.Property(e => e.NumeroRecibo).HasColumnName("recibo");
                entity.Property(e => e.Fecha).HasColumnName("fecha").IsRequired();
                entity.Property(e => e.PagadoA).HasColumnName("nombre").IsRequired();
                entity.Property(e => e.Monto).HasColumnName("monto").HasColumnType("money");
                entity.Property(e => e.ConCargoA).HasColumnName("cargoa");
                entity.Property(e => e.Concepto).HasColumnName("concepto");
                
                // Ignorar propiedades calculadas
                entity.Ignore(e => e.FechaFormateada);
                entity.Ignore(e => e.MontoFormateado);
                entity.Ignore(e => e.MontoColor);
            });

            System.Diagnostics.Debug.WriteLine($"[CONTEXT] Modelo configurado correctamente");
            System.Diagnostics.Debug.WriteLine($"[CONTEXT] Entidades configuradas: Acceso, Paciente, Donaciones, Cheques, Clientes, Recibo, CajaChica");
        }
    }
}
