using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsiAgenda.Api.Autenticacao;
using PsiAgenda.Application.Agendamentos;
using PsiAgenda.Application.Common;

namespace PsiAgenda.Api.Controllers;

/// <summary>Painel do psicologo: enxerga e opera a agenda inteira do proprio tenant.</summary>
[ApiController]
[Route("api/agenda")]
[Authorize(Roles = RolesDoSistema.Psicologo)]
public class AgendaController(
    ServicoDeAgendamento servico,
    IAgendaQueries queries,
    IUsuarioAtual usuario) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgendamentoResumo>>> Listar(
        [FromQuery] DateTime deUtc, [FromQuery] DateTime ateUtc, CancellationToken ct)
    {
        if (ateUtc <= deUtc)
            return BadRequest(new { detail = "'ateUtc' deve ser maior que 'deUtc'." });
        if ((ateUtc - deUtc).TotalDays > 62)
            return BadRequest(new { detail = "Janela maxima de consulta e de 62 dias." });

        return Ok(await queries.ObterAgendaDoPsicologoAsync(usuario.PsicologoIdObrigatorio(), deUtc, ateUtc, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Agendar(AgendarConsultaRequest req, CancellationToken ct)
    {
        var agendamento = await servico.AgendarAsync(usuario.PsicologoIdObrigatorio(), req, ct);
        return Created($"/api/agenda/{agendamento.Id}", new { agendamento.Id, agendamento.SalaVideoId });
    }

    [HttpPut("{id:guid}/reagendar")]
    public async Task<IActionResult> Reagendar(Guid id, ReagendarRequest req, CancellationToken ct)
    {
        await servico.ReagendarAsync(usuario.PsicologoIdObrigatorio(), id, req.NovoInicioUtc, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid id, CancellationToken ct)
    {
        await servico.ConfirmarAsync(usuario.PsicologoIdObrigatorio(), id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/concluir")]
    public async Task<IActionResult> Concluir(Guid id, CancellationToken ct)
    {
        await servico.ConcluirAsync(usuario.PsicologoIdObrigatorio(), id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/falta")]
    public async Task<IActionResult> RegistrarFalta(Guid id, CancellationToken ct)
    {
        await servico.RegistrarFaltaAsync(usuario.PsicologoIdObrigatorio(), id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancelarRequest req, CancellationToken ct)
    {
        await servico.CancelarPeloPsicologoAsync(usuario.PsicologoIdObrigatorio(), id, req.Motivo, ct);
        return NoContent();
    }
}
