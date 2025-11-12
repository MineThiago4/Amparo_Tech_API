using Microsoft.EntityFrameworkCore;
using Amparo_Tech_API.Models;
namespace Amparo_Tech_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Usuariologin> usuariologin { get; set; }
        public DbSet<Endereco> endereco { get; set; }
        public DbSet<Categoria> categoria { get; set; }
        public DbSet<Instituicao> instituicao { get; set; }
        public DbSet<Administrador> administrador { get; set; }
        public DbSet<DoacaoItem> doacaoitem { get; set; }
        public DbSet<DoacaoMidia> doacaomidia { get; set; }
        public DbSet<Mensagem> mensagem { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuariologin>()
                .Property(u => u.TipoUsuario)
                .HasConversion<string>();

            modelBuilder.Entity<Usuariologin>()
                .HasOne(u => u.Endereco)
                .WithMany()
                .HasForeignKey(u => u.IdEndereco);

            modelBuilder.Entity<DoacaoItem>()
                .Property(d => d.Status)
                .HasConversion<string>();

            modelBuilder.Entity<DoacaoMidia>()
                .HasOne(dm => dm.DoacaoItem)
                .WithMany(di => di.Midias)
                .HasForeignKey(dm => dm.IdDoacaoItem)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}