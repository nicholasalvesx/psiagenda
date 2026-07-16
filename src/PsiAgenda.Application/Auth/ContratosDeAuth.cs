namespace PsiAgenda.Application.Auth;

public record RegistrarPsicologoRequest(string NomeCompleto, string Email, string Senha, string Crp);

/// <summary>Sem e-mail: quem define de quem e a conta e o token do convite.</summary>
public record RegistrarPacienteRequest(string Convite, string Senha, string? NomeCompleto, string? Telefone, DateOnly? DataNascimento);

public record LoginRequest(string Email, string Senha);

public record TokenResponse(string AccessToken, DateTime ExpiraEmUtc, string Role, string NomeExibicao);

/// <summary>
/// O access token vai no corpo (o JS precisa dele); o refresh vai em cookie httpOnly,
/// fora do alcance de qualquer script — e o que limita o estrago de um XSS.
/// </summary>
public record ParDeTokens(TokenResponse Acesso, string RefreshToken);

public interface IGerenciadorDeRefreshToken
{
    /// <summary>Emite um token novo. Passar familiaId continua a cadeia de uma sessao existente.</summary>
    Task<string> EmitirAsync(Guid usuarioId, Guid? familiaId = null, CancellationToken ct = default);

    /// <summary>Consome o token e devolve outro. Lanca se estiver invalido, expirado ou reutilizado.</summary>
    Task<(Guid UsuarioId, string NovoToken)> RotacionarAsync(string token, CancellationToken ct = default);

    Task RevogarAsync(string token, CancellationToken ct = default);

    /// <summary>Derruba todas as sessoes do usuario. Usado ao trocar a senha.</summary>
    Task RevogarTudoDoUsuarioAsync(Guid usuarioId, string motivo, CancellationToken ct = default);
}

/// <summary>Operacoes de identidade (ASP.NET Identity vive na Infrastructure).</summary>
public interface IGerenciadorDeUsuarios
{
    Task<Guid> CriarUsuarioAsync(string email, string senha, string role, CancellationToken ct = default);
    Task<Guid?> ValidarCredenciaisAsync(string email, string senha, CancellationToken ct = default);
    Task<string?> ObterRoleAsync(Guid usuarioId, CancellationToken ct = default);
    Task<string?> ObterEmailAsync(Guid usuarioId, CancellationToken ct = default);
    Task<Guid?> ObterIdPorEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailJaCadastradoAsync(string email, CancellationToken ct = default);
    Task RemoverUsuarioAsync(Guid usuarioId, CancellationToken ct = default);

    /// <summary>Aplica a politica de senha e troca o hash. Nao mexe em sessoes — quem faz isso e o chamador.</summary>
    Task RedefinirSenhaAsync(Guid usuarioId, string novaSenha, CancellationToken ct = default);
}

public interface IGeradorDeToken
{
    TokenResponse Gerar(Guid usuarioId, string email, string role, string nomeExibicao, Guid? psicologoId, Guid? pacienteId);
}
