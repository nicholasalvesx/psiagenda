using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsiAgenda.Api.Autenticacao;
using PsiAgenda.Application.Common;
using PsiAgenda.Application.Pacientes;

namespace PsiAgenda.Api.Controllers;

[ApiController]
[Route("api/pacientes")]
[Authorize(Roles = RolesDoSistema.Psicologo)]
public class PacientesController(
    ServicoDePacientes servico,
    ServicoDeConvites convites,
    IPacienteQueries queries,
    IUsuarioAtual usuario) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PacienteResumo>>> Listar(
        [FromQuery] string? busca, [FromQuery] bool apenasAtivos = true, CancellationToken ct = default)
        => Ok(await queries.ListarAsync(usuario.PsicologoIdObrigatorio(), busca, apenasAtivos, ct));

    /// <summary>Cadastra e ja dispara o convite de acesso ao portal.</summary>
    [HttpPost]
    public async Task<IActionResult> Cadastrar(CadastrarPacienteRequest req, CancellationToken ct)
    {
        var (paciente, conviteEnviado) = await servico.CadastrarAsync(usuario.PsicologoIdObrigatorio(), req, ct);

        // conviteEnviado=false significa paciente salvo mas e-mail falhou: a tela avisa e oferece reenvio.
        return CreatedAtAction(nameof(Listar), new { }, new { paciente.Id, conviteEnviado });
    }

    [HttpPost("{id:guid}/convite")]
    public async Task<IActionResult> ReenviarConvite(Guid id, CancellationToken ct)
    {
        await convites.EmitirAsync(usuario.PsicologoIdObrigatorio(), id, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, AtualizarPacienteRequest req, CancellationToken ct)
    {
        await servico.AtualizarAsync(usuario.PsicologoIdObrigatorio(), id, req, ct);
        return NoContent();
    }

    /// <summary>Desativa (nao apaga): historico de consultas precisa continuar existindo.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id, CancellationToken ct)
    {
        await servico.DesativarAsync(usuario.PsicologoIdObrigatorio(), id, ct);
        return NoContent();
    }
}
