using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Amparo_Tech_API.Models;
namespace Amparo_Tech_API.DTOs
{
    public class DoacaoCadastroDTO : IValidatableObject
    {
        [Required, StringLength(120)] 
        public string Titulo { get; set; }

        [Required, StringLength(1000)]
        public string Descricao { get; set; }

        [StringLength(200)]
        public string? Condicao { get; set; }

        [Required]
        public int IdCategoria { get; set; }

        // Agora guarda o ID da instituição atribuída
        public int? IdInstituicaoAtribuida { get; set; }

        // Referência ao arquivo enviado em /api/uploads/midia
        public string? MidiaId { get; set; }           // compat
        public List<string>? MidiaIds { get; set; }    // novo

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Mantido para futura validação contextual se necessário
            yield break;
        }

    }
}