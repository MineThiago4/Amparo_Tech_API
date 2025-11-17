using System;
using System.Collections.Generic;

namespace Amparo_Tech_API.DTOs
{
    public class DoacaoViewDTO
    {
        public int IdDoacaoItem { get; set; }
        public int IdDoador { get; set; }
        public int IdCategoria { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public string? Condicao { get; set; }
        public string? MidiaId { get; set; }
        public List<DoacaoMidiaDTO>? Midias { get; set; }
        public int? IdInstituicaoAtribuida { get; set; }
        public object? InstituicaoAtribuida { get; set; }
        public int? RequeridoPor { get; set; }
        public DateTime DataDoacao { get; set; }
        public int Status { get; set; }
    }
}
