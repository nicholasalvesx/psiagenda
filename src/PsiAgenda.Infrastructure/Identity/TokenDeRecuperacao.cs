namespace PsiAgenda.Infrastructure.Identity;

/// <summary>
/// Token do link de "esqueci minha senha". Fica na Infrastructure junto do login, e nao no dominio:
/// vale para psicologo e paciente, e nao tem nada a ver com as regras do consultorio.
/// </summary>
public class TokenDeRecuperacao
{
    public const int ValidadeEmMinutos = 30;

    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }

    /// <summary>Hash SHA-256. O valor cru so existe no e-mail do usuario.</summary>
    public string TokenHash { get; set; } = null!;

    public DateTime CriadoEmUtc { get; set; }
    public DateTime ExpiraEmUtc { get; set; }
    public DateTime? UsadoEmUtc { get; set; }
    public DateTime? InvalidadoEmUtc { get; set; }
}
