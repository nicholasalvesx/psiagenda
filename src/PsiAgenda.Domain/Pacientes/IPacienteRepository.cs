namespace PsiAgenda.Domain.Pacientes;

public interface IPacienteRepository
{
    /// <summary>Sempre filtrado por psicologo: paciente de outro tenant nunca deve ser alcancavel.</summary>
    Task<Paciente?> ObterPorIdAsync(Guid id, Guid psicologoId, CancellationToken ct = default);
    Task<Paciente?> ObterPorUsuarioIdAsync(Guid usuarioId, CancellationToken ct = default);

    /// <summary>
    /// Sem filtro de tenant. Usar SOMENTE no fluxo de convite, onde quem autoriza e o token
    /// do link — nao ha usuario logado ainda para se extrair o tenant.
    /// </summary>
    Task<Paciente?> ObterPorIdSemTenantAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExisteComEmailAsync(string email, Guid psicologoId, CancellationToken ct = default);
    Task AdicionarAsync(Paciente paciente, CancellationToken ct = default);
}
