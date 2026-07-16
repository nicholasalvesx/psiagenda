namespace PsiAgenda.Infrastructure.Identity;

/// <summary>
/// Sessao de longa duracao. Fica na Infrastructure de proposito: e detalhe de autenticacao,
/// nao regra de negocio do consultorio.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }

    /// <summary>Hash SHA-256 do token. O valor cru so existe no cookie do cliente.</summary>
    public string TokenHash { get; set; } = null!;

    /// <summary>
    /// Agrupa a cadeia de rotacoes de um mesmo login. Se um token ja usado reaparecer,
    /// a familia inteira e revogada — e o sinal classico de token roubado.
    /// </summary>
    public Guid FamiliaId { get; set; }

    public DateTime CriadoEmUtc { get; set; }
    public DateTime ExpiraEmUtc { get; set; }
    public DateTime? RevogadoEmUtc { get; set; }
    public string? MotivoRevogacao { get; set; }

    public bool Ativo => RevogadoEmUtc is null && DateTime.UtcNow < ExpiraEmUtc;
}
