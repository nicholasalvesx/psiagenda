using PsiAgenda.Domain.Agendamentos;

namespace PsiAgenda.Application.Agendamentos;

public record AgendarConsultaRequest(Guid PacienteId, DateTime InicioUtc, int? DuracaoMinutos, ModalidadeAtendimento Modalidade, string? Motivo);

public record ReagendarRequest(DateTime NovoInicioUtc);

public record CancelarRequest(string? Motivo);

public record AgendamentoResumo(
    Guid Id,
    Guid PacienteId,
    string PacienteNome,
    DateTime InicioUtc,
    DateTime FimUtc,
    int DuracaoMinutos,
    string Status,
    string Modalidade,
    Guid? SalaVideoId);

/// <summary>Leitura da agenda via Dapper: e tela de grade, nao precisa de tracking do EF.</summary>
public interface IAgendaQueries
{
    Task<IReadOnlyList<AgendamentoResumo>> ObterAgendaDoPsicologoAsync(
        Guid psicologoId, DateTime deUtc, DateTime ateUtc, CancellationToken ct = default);

    Task<IReadOnlyList<AgendamentoResumo>> ObterConsultasDoPacienteAsync(
        Guid pacienteId, CancellationToken ct = default);
}
