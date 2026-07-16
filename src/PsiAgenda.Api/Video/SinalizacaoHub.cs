using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PsiAgenda.Application.Common;
using PsiAgenda.Domain.Agendamentos;

namespace PsiAgenda.Api.Video;

/// <summary>
/// Signaling do WebRTC: troca SDP/ICE entre os dois participantes. O audio e video nunca passam
/// por aqui — vao P2P (ou via TURN). O hub so aproxima os dois lados.
/// </summary>
[Authorize]
public class SinalizacaoHub(
    IAgendamentoRepository agendamentos,
    IUsuarioAtual usuario,
    ILogger<SinalizacaoHub> logger) : Hub
{
    /// <summary>
    /// Entra na sala da consulta. A permissao e checada AQUI, no servidor: confiar no salaId
    /// que o cliente manda deixaria qualquer autenticado escutar a sessao alheia.
    /// </summary>
    public async Task EntrarNaSala(Guid salaVideoId)
    {
        var agendamento = await agendamentos.ObterPorSalaVideoAsync(salaVideoId, Context.ConnectionAborted);

        if (agendamento is null || !EhParticipante(agendamento) || !agendamento.PodeEntrarNaSala(DateTime.UtcNow))
        {
            logger.LogWarning("Entrada negada na sala {Sala} para o usuario {Usuario}", salaVideoId, usuario.UsuarioId);
            throw new HubException("Voce nao pode entrar nessa sala agora.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, NomeDoGrupo(salaVideoId));
        await Clients.OthersInGroup(NomeDoGrupo(salaVideoId)).SendAsync("ParticipanteEntrou", Context.ConnectionId);
    }

    public async Task SairDaSala(Guid salaVideoId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, NomeDoGrupo(salaVideoId));
        await Clients.OthersInGroup(NomeDoGrupo(salaVideoId)).SendAsync("ParticipanteSaiu", Context.ConnectionId);
    }

    /// <summary>Repassa offer/answer/candidate para o outro lado, sem interpretar o conteudo.</summary>
    public async Task EnviarSinal(Guid salaVideoId, string tipo, string payload)
    {
        var agendamento = await agendamentos.ObterPorSalaVideoAsync(salaVideoId, Context.ConnectionAborted);

        // Revalida a cada sinal: a janela da consulta pode ter fechado no meio da conexao.
        if (agendamento is null || !EhParticipante(agendamento) || !agendamento.PodeEntrarNaSala(DateTime.UtcNow))
            throw new HubException("Sessao encerrada.");

        await Clients.OthersInGroup(NomeDoGrupo(salaVideoId))
            .SendAsync("SinalRecebido", Context.ConnectionId, tipo, payload);
    }

    private bool EhParticipante(Agendamento agendamento)
        => (usuario.EhPsicologo && agendamento.PsicologoId == usuario.PsicologoId)
           || (usuario.EhPaciente && agendamento.PacienteId == usuario.PacienteId);

    private static string NomeDoGrupo(Guid salaVideoId) => $"sala:{salaVideoId}";
}
