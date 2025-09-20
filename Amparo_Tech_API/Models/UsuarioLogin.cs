using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Amparo_Tech_API.Models
{
    [Table("usuariologin")]
    public class Usuariologin
    {
        [Key]
        [Column("idUsuario")]
        public int IdUsuario { get; set; }

        [Column("nome")]
        public string Nome { get; set; }

        [Column("cpf")]
        public string Cpf { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("senha")]
        public string Senha { get; set; }

        [Column("tipoUsuario")]
        public TipoUsuarioEnum TipoUsuario { get; set; }

        [Column("dataCadastro")]
        public DateTime? DataCadastro { get; set; }

        [Column("ultimoLogin")]
        public DateTime? UltimoLogin { get; set; }

        [Column("telefone")]
        public string? Telefone { get; set; }

        [Column("idEndereco")]
        public int? IdEndereco { get; set; }

        [ForeignKey("IdEndereco")]
        public Endereco? Endereco { get; set; }
    }

    public enum TipoUsuarioEnum
    {
        Doador,
        Beneficiario
    }
}