namespace PsiAgenda.Domain.Agendamentos;

public interface IAgendamentoRepository
{
    Task<Agendamento?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Agendamento?> ObterPorSalaVideoAsync(Guid salaVideoId, CancellationToken ct = default);

    /// <summary>Agendamentos nao cancelados do psicologo que cruzam a janela informada.</summary>
    Task<IReadOnlyList<Agendamento>> ObterNaJanelaAsync(
        Guid psicologoId, DateTime inicioUtc, DateTime fimUtc, CancellationToken ct = default);

    Task AdicionarAsync(Agendamento agendamento, CancellationToken ct = default);
}
