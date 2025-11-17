using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace Amparo_Tech_API.Models
{
    [Table("instituicao")]
    public class Instituicao
    {
        [Key][Column("idInstituicao")] public int IdInstituicao { get; set; }
        [Required, Column("nome")]
        [StringLength(150)]
        public string Nome { get; set; }

        [Column("cnpj")]
        [StringLength(18)]
        public string? Cnpj { get; set; }

        [Required, Column("email")]
        [StringLength(200)]
        public string Email { get; set; }

        [JsonIgnore]
        [Column("senha")]
        [StringLength(200)]
        public string? Senha { get; set; }

        [Column("pessoaContato")]
        [StringLength(120)]
        public string? PessoaContato { get; set; }

        [Column("telefone")]
        [StringLength(30)]
        public string? Telefone { get; set; }

        [JsonIgnore]
        [Column("dataCadastro")]
        public DateTime? DataCadastro { get; set; }

        [JsonIgnore]
        [Column("ultimoLogin")]
        public DateTime? UltimoLogin { get; set; }

        [Column("idEndereco")]
        public int? IdEndereco { get; set; }

        [ForeignKey(nameof(IdEndereco))]
        public Endereco? Endereco { get; set; }
    }
}