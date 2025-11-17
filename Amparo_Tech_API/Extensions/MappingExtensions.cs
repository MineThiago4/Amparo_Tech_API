using Amparo_Tech_API.DTOs;
using Amparo_Tech_API.Models;

namespace Amparo_Tech_API.Extensions
{
 public static class MappingExtensions
 {
 public static EnderecoDTO? ToDTO(this Endereco? e)
 {
 if (e == null) return null;
 return new EnderecoDTO
 {
 IdEndereco = e.IdEndereco,
 Cep = e.Cep,
 Logradouro = e.Logradouro,
 Numero = e.Numero,
 Complemento = e.Complemento,
 Cidade = e.Cidade,
 Estado = e.Estado,
 InformacoesAdicionais = e.InformacoesAdicionais
 };
 }

 public static InstituicaoViewDTO ToViewDTO(this Instituicao i)
 {
 return new InstituicaoViewDTO
 {
 IdInstituicao = i.IdInstituicao,
 Nome = i.Nome,
 Email = i.Email,
 Cnpj = i.Cnpj,
 Telefone = i.Telefone,
 PessoaContato = i.PessoaContato,
 Endereco = i.Endereco.ToDTO()
 };
 }

 public static InstituicaoAdminDTO ToAdminDTO(this Instituicao i)
 {
 return new InstituicaoAdminDTO
 {
 IdInstituicao = i.IdInstituicao,
 Nome = i.Nome,
 Email = i.Email,
 Cnpj = i.Cnpj,
 Telefone = i.Telefone,
 PessoaContato = i.PessoaContato,
 Endereco = i.Endereco.ToDTO(),
 DataCadastro = i.DataCadastro,
 UltimoLogin = i.UltimoLogin
 };
 }

 public static DoacaoMidiaDTO ToDTO(this DoacaoMidia m)
 {
 if (m == null) return null;
 return new DoacaoMidiaDTO
 {
 IdDoacaoMidia = m.IdDoacaoMidia,
 MidiaId = m.MidiaId,
 Tipo = m.Tipo,
 Ordem = m.Ordem
 };
 }

 public static DoacaoViewDTO ToViewDTO(this DoacaoItem d, object? instituicaoDto = null, List<DoacaoMidiaDTO>? midias = null)
 {
 return new DoacaoViewDTO
 {
 IdDoacaoItem = d.IdDoacaoItem,
 IdDoador = d.IdDoador,
 IdCategoria = d.IdCategoria,
 Titulo = d.Titulo,
 Descricao = d.Descricao,
 Condicao = d.Condicao,
 MidiaId = d.MidiaId,
 Midias = midias,
 IdInstituicaoAtribuida = d.IdInstituicaoAtribuida,
 InstituicaoAtribuida = instituicaoDto,
 RequeridoPor = d.RequeridoPor,
 DataDoacao = d.DataDoacao,
 Status = (int)d.Status
 };
 }
 }
}
