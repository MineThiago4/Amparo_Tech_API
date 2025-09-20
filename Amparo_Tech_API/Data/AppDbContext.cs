using Microsoft.EntityFrameworkCore;
using Amparo_Tech_API.Models;

namespace Amparo_Tech_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Esta propriedade DbSet representa a tabela Itens no banco de dados.
        // Você usará esta propriedade para fazer consultas e operações CRUD.
        public DbSet<Usuariologin> usuariologin { get; set; }
        public DbSet<Endereco> endereco { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Mapeia a propriedade TipoUsuario como uma string
            modelBuilder.Entity<Usuariologin>()
                .Property(u => u.TipoUsuario)
                .HasConversion<string>();

            // Configura a chave estrangeira
            modelBuilder.Entity<Usuariologin>()
                .HasOne(u => u.Endereco)
                .WithMany()
                .HasForeignKey(u => u.IdEndereco);
        }
    }
}