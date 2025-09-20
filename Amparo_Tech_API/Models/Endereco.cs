using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Amparo_Tech_API.Models
{
    [Table("endereco")]
    public class Endereco
    {
        [Key]
        [Column("idEndereco")]
        public int IdEndereco { get; set; }

        [Column("cep")]
        public string Cep { get; set; }

        [Column("logradouro")]
        public string Logradouro { get; set; }

        [Column("numero")]
        public string Numero { get; set; }

        [Column("complemento")]
        public string Complemento { get; set; }

        [Column("cidade")]
        public string Cidade { get; set; }

        [Column("estado")]
        public string Estado { get; set; }

        [Column("informacoesAdicionais")]
        public string? InformacoesAdicionais { get; set; }
    }
}