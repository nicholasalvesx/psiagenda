namespace PsiAgenda.Domain.Psicologos;

public interface IPsicologoRepository
{
    Task<Psicologo?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Psicologo?> ObterPorUsuarioIdAsync(Guid usuarioId, CancellationToken ct = default);
    Task<bool> ExisteComCrpAsync(string crp, CancellationToken ct = default);
    Task AdicionarAsync(Psicologo psicologo, CancellationToken ct = default);
}
