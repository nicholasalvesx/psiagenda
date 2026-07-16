using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PsiAgenda.Api.Video;
using PsiAgenda.Application.Common;
using PsiAgenda.Domain.Agendamentos;
using PsiAgenda.Domain.Common;

namespace PsiAgenda.Api.Controllers;

[ApiController]
[Route("api/video")]
[Authorize]
public class VideoController(
    IAgendamentoRepository agendamentos,
    IUsuarioAtual usuario,
    IOptions<TurnOptions> turn) : ControllerBase
{
    /// <summary>Libera a sala WebRTC do agendamento para quem tem direito e dentro da janela da consulta.</summary>
    [HttpGet("{agendamentoId:guid}/entrar")]
    public async Task<ActionResult<SalaResponse>> Entrar(Guid agendamentoId, CancellationToken ct)
    {
        var agendamento = await agendamentos.ObterPorIdAsync(agendamentoId, ct)
            ?? throw new DomainException("Consulta nao encontrada.");

        var papel = ResolverPapel(agendamento)
            ?? throw new UnauthorizedAccessException("Consulta nao encontrada.");

        if (agendamento.Modalidade != ModalidadeAtendimento.Online)
            throw new DomainException("Essa consulta e presencial.");
        if (!agendamento.PodeEntrarNaSala(DateTime.UtcNow))
            throw new DomainException("A sala abre 10 minutos antes do horario e fecha ao fim da consulta.");

        return Ok(new SalaResponse(
            agendamento.SalaVideoId!.Value,
            agendamento.InicioUtc,
            agendamento.FimUtc,
            papel,
            MontarIceServers()));
    }

    /// <summary>Null = o usuario nao participa dessa consulta.</summary>
    private string? ResolverPapel(Agendamento agendamento)
    {
        if (usuario.EhPsicologo && agendamento.PsicologoId == usuario.PsicologoId)
            return "psicologo";
        if (usuario.EhPaciente && agendamento.PacienteId == usuario.PacienteId)
            return "paciente";

        return null;
    }

    private IceServer[] MontarIceServers()
    {
        var opcoes = turn.Value;
        List<IceServer> servidores = [new IceServer(opcoes.StunUrls)];

        if (opcoes.TurnUrls.Length > 0)
            servidores.Add(new IceServer(opcoes.TurnUrls, opcoes.Usuario, opcoes.Senha));

        return [.. servidores];
    }
}
