using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Amparo_Tech_API.DTOs
{
    public class DoacaoAtualizacaoDTO
    {
        [StringLength(120)]
        public string? Titulo { get; set; }

        [StringLength(1000)]
        public string? Descricao { get; set; }

        [StringLength(200)]
        public string? Condicao { get; set; }

        public int? IdCategoria { get; set; }

        public int? IdInstituicaoAtribuida { get; set; }

        // Atualização de mídias
        public bool? SubstituirMidias { get; set; }
        public string? MidiaId { get; set; }           // compat
        public List<string>? MidiaIds { get; set; }    // novo
    }
}
