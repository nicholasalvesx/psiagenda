using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsiAgenda.Api.Autenticacao;
using PsiAgenda.Application.Agendamentos;
using PsiAgenda.Application.Common;

namespace PsiAgenda.Api.Controllers;

/// <summary>Painel do paciente: so as proprias consultas, e o cancelamento respeita a janela de 24h.</summary>
[ApiController]
[Route("api/minhas-consultas")]
[Authorize(Roles = RolesDoSistema.Paciente)]
public class MinhasConsultasController(
    ServicoDeAgendamento servico,
    IAgendaQueries queries,
    IUsuarioAtual usuario) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgendamentoResumo>>> Listar(CancellationToken ct)
        => Ok(await queries.ObterConsultasDoPacienteAsync(usuario.PacienteIdObrigatorio(), ct));

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancelarRequest req, CancellationToken ct)
    {
        await servico.CancelarPeloPacienteAsync(usuario.PacienteIdObrigatorio(), id, req.Motivo, ct);
        return NoContent();
    }
}
