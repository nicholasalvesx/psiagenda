namespace PsiAgenda.Domain.Pacientes;

public interface IConviteRepository
{
    Task<ConviteDeAcesso?> ObterPorTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Convites em aberto do paciente, para cancelar antes de emitir um novo.</summary>
    Task<IReadOnlyList<ConviteDeAcesso>> ObterEmAbertoAsync(Guid pacienteId, CancellationToken ct = default);

    Task AdicionarAsync(ConviteDeAcesso convite, CancellationToken ct = default);
}
