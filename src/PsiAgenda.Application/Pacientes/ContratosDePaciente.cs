namespace PsiAgenda.Application.Pacientes;

public record CadastrarPacienteRequest(string NomeCompleto, string Email, string? Telefone, DateOnly? DataNascimento);

public record AtualizarPacienteRequest(string NomeCompleto, string? Telefone, DateOnly? DataNascimento);

public record PacienteResumo(
    Guid Id,
    string NomeCompleto,
    string Email,
    string? Telefone,
    DateOnly? DataNascimento,
    bool PossuiAcessoAoPortal,
    bool Ativo,
    DateTime? ProximaConsultaUtc);

public interface IPacienteQueries
{
    Task<IReadOnlyList<PacienteResumo>> ListarAsync(
        Guid psicologoId, string? busca, bool apenasAtivos, CancellationToken ct = default);
}
