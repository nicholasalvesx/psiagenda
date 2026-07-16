using PsiAgenda.Domain.Common;

namespace PsiAgenda.Domain.Pacientes;

/// <summary>
/// Paciente sempre pertence a um psicologo (PsicologoId = tenant). O UsuarioId e opcional:
/// o psicologo pode cadastrar alguem que ainda nao criou login no portal.
/// </summary>
public class Paciente : AggregateRoot
{
    public Guid PsicologoId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string NomeCompleto { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string? Telefone { get; private set; }
    public DateOnly? DataNascimento { get; private set; }
    public bool Ativo { get; private set; } = true;

    private Paciente() { } // EF

    public Paciente(Guid psicologoId, string nomeCompleto, Email email, string? telefone = null, DateOnly? dataNascimento = null)
    {
        if (psicologoId == Guid.Empty)
            throw new DomainException("Paciente precisa estar vinculado a um psicologo.");
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new DomainException("Nome do paciente e obrigatorio.");
        if (dataNascimento is not null && dataNascimento > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DomainException("Data de nascimento nao pode estar no futuro.");

        Id = Guid.NewGuid();
        PsicologoId = psicologoId;
        NomeCompleto = nomeCompleto.Trim();
        Email = email;
        Telefone = telefone?.Trim();
        DataNascimento = dataNascimento;
    }

    public void VincularUsuario(Guid usuarioId)
    {
        if (UsuarioId is not null)
            throw new DomainException("Paciente ja possui um usuario vinculado.");

        UsuarioId = usuarioId;
        MarcarAtualizado();
    }

    public void AtualizarDados(string nomeCompleto, string? telefone, DateOnly? dataNascimento)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new DomainException("Nome do paciente e obrigatorio.");

        NomeCompleto = nomeCompleto.Trim();
        Telefone = telefone?.Trim();
        DataNascimento = dataNascimento;
        MarcarAtualizado();
    }

    public void Desativar()
    {
        Ativo = false;
        MarcarAtualizado();
    }
}
