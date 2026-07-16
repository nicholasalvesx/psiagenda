using PsiAgenda.Domain.Common;

namespace PsiAgenda.Domain.Pacientes;

/// <summary>
/// Convite para o paciente criar o login do portal. Sem ele, bastaria adivinhar o e-mail de
/// alguem ja cadastrado para tomar a conta antes do dono — o convite prova posse do e-mail.
/// </summary>
public class ConviteDeAcesso : AggregateRoot
{
    public const int ValidadeEmDias = 7;

    public Guid PacienteId { get; private set; }

    /// <summary>Hash do token. O valor cru so vai no link enviado por e-mail.</summary>
    public string TokenHash { get; private set; } = null!;

    public DateTime ExpiraEmUtc { get; private set; }
    public DateTime? UsadoEmUtc { get; private set; }
    public DateTime? CanceladoEmUtc { get; private set; }

    private ConviteDeAcesso() { } // EF

    public ConviteDeAcesso(Guid pacienteId, string tokenHash)
    {
        if (pacienteId == Guid.Empty)
            throw new DomainException("Convite exige um paciente.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Convite exige um token.");

        Id = Guid.NewGuid();
        PacienteId = pacienteId;
        TokenHash = tokenHash;
        ExpiraEmUtc = DateTime.UtcNow.AddDays(ValidadeEmDias);
    }

    public bool Valido(DateTime agoraUtc)
        => UsadoEmUtc is null && CanceladoEmUtc is null && agoraUtc < ExpiraEmUtc;

    public void Consumir()
    {
        if (UsadoEmUtc is not null)
            throw new DomainException("Este convite ja foi utilizado.");
        if (CanceladoEmUtc is not null)
            throw new DomainException("Este convite foi cancelado.");
        if (DateTime.UtcNow >= ExpiraEmUtc)
            throw new DomainException("Este convite expirou. Peca um novo ao seu psicologo.");

        UsadoEmUtc = DateTime.UtcNow;
        MarcarAtualizado();
    }

    /// <summary>Reenviar o convite invalida o anterior: so o link mais recente deve funcionar.</summary>
    public void Cancelar()
    {
        if (UsadoEmUtc is not null || CanceladoEmUtc is not null)
            return;

        CanceladoEmUtc = DateTime.UtcNow;
        MarcarAtualizado();
    }
}
