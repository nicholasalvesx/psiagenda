namespace PsiAgenda.Application.Auth;

public record PedirRecuperacaoRequest(string Email);

public record RedefinirSenhaRequest(string Token, string NovaSenha);

public record TokenDeRecuperacaoInfo(Guid Id, Guid UsuarioId);

/// <summary>Persistencia dos tokens de "esqueci minha senha" (vive na Infrastructure, junto do login).</summary>
public interface IRepositorioDeRecuperacao
{
    Task CriarAsync(Guid usuarioId, string tokenHash, int validadeEmMinutos, CancellationToken ct = default);

    /// <summary>Null se nao existe, ja foi usado, foi invalidado ou expirou.</summary>
    Task<TokenDeRecuperacaoInfo?> ObterValidoAsync(string tokenHash, CancellationToken ct = default);

    Task ConsumirAsync(Guid id, CancellationToken ct = default);

    /// <summary>Deixa sem efeito todos os links em aberto do usuario.</summary>
    Task InvalidarEmAbertoAsync(Guid usuarioId, CancellationToken ct = default);
}
