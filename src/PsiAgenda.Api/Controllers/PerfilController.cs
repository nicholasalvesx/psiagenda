using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsiAgenda.Api.Autenticacao;
using PsiAgenda.Application.Common;
using PsiAgenda.Application.Psicologos;

namespace PsiAgenda.Api.Controllers;

[ApiController]
[Route("api/perfil")]
[Authorize(Roles = RolesDoSistema.Psicologo)]
public class PerfilController(ServicoDePerfil servico, IUsuarioAtual usuario) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PerfilResponse>> Obter(CancellationToken ct)
        => Ok(await servico.ObterAsync(usuario.PsicologoIdObrigatorio(), ct));

    [HttpPut]
    public async Task<ActionResult<PerfilResponse>> Atualizar(AtualizarPerfilRequest req, CancellationToken ct)
        => Ok(await servico.AtualizarAsync(usuario.PsicologoIdObrigatorio(), req, ct));

    /// <summary>Libera o atendimento online declarando o cadastro no e-Psi (Res. CFP 11/2018).</summary>
    [HttpPost("epsi")]
    public async Task<ActionResult<PerfilResponse>> ConfirmarEPsi(CancellationToken ct)
        => Ok(await servico.ConfirmarEPsiAsync(usuario.PsicologoIdObrigatorio(), ct));

    [HttpPost("disponibilidades")]
    public async Task<ActionResult<PerfilResponse>> AdicionarDisponibilidade(
        DefinirDisponibilidadeRequest req, CancellationToken ct)
        => Ok(await servico.AdicionarDisponibilidadeAsync(usuario.PsicologoIdObrigatorio(), req, ct));

    [HttpDelete("disponibilidades/{id:guid}")]
    public async Task<ActionResult<PerfilResponse>> RemoverDisponibilidade(Guid id, CancellationToken ct)
        => Ok(await servico.RemoverDisponibilidadeAsync(usuario.PsicologoIdObrigatorio(), id, ct));
}
