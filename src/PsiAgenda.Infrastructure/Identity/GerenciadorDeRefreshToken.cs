using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PsiAgenda.Application.Auth;
using PsiAgenda.Application.Common;
using PsiAgenda.Domain.Common;
using PsiAgenda.Infrastructure.Persistencia;

namespace PsiAgenda.Infrastructure.Identity;

public class GerenciadorDeRefreshToken(
    AppDbContext db,
    IGeradorDeTokenOpaco tokens,
    IOptions<JwtOptions> opcoes,
    ILogger<GerenciadorDeRefreshToken> logger) : IGerenciadorDeRefreshToken
{
    private readonly JwtOptions _opcoes = opcoes.Value;

    public async Task<string> EmitirAsync(Guid usuarioId, Guid? familiaId = null, CancellationToken ct = default)
    {
        var token = tokens.Gerar();

        db.Set<RefreshToken>().Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            TokenHash = tokens.Hash(token),
            FamiliaId = familiaId ?? Guid.NewGuid(),
            CriadoEmUtc = DateTime.UtcNow,
            ExpiraEmUtc = DateTime.UtcNow.AddDays(_opcoes.RefreshExpiracaoEmDias),
        });

        await db.SaveChangesAsync(ct);
        return token;
    }

    public async Task<(Guid UsuarioId, string NovoToken)> RotacionarAsync(string token, CancellationToken ct = default)
    {
        var hash = tokens.Hash(token);
        var atual = await db.Set<RefreshToken>().AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new DomainException("Sessao invalida. Faca login novamente.");

        if (DateTime.UtcNow >= atual.ExpiraEmUtc)
            throw new DomainException("Sua sessao expirou. Faca login novamente.");

        // Consome o token em um UPDATE condicional: dois refreshes simultaneos com o mesmo token
        // nao podem os dois passar por uma checagem em memoria e gerar duas cadeias validas.
        var consumidos = await db.Set<RefreshToken>()
            .Where(t => t.Id == atual.Id && t.RevogadoEmUtc == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevogadoEmUtc, DateTime.UtcNow)
                .SetProperty(t => t.MotivoRevogacao, "rotacionado"), ct);

        // Ninguem consumiu agora => o token ja estava revogado. Alguem esta com uma copia.
        if (consumidos == 0)
        {
            logger.LogWarning("Reuso de refresh token detectado (familia {Familia}, usuario {Usuario})",
                atual.FamiliaId, atual.UsuarioId);

            await RevogarFamiliaAsync(atual.FamiliaId, "reuso detectado", ct);
            throw new DomainException("Sessao encerrada por seguranca. Faca login novamente.");
        }

        var novo = await EmitirAsync(atual.UsuarioId, atual.FamiliaId, ct);
        return (atual.UsuarioId, novo);
    }

    public async Task RevogarAsync(string token, CancellationToken ct = default)
    {
        var hash = tokens.Hash(token);
        var atual = await db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (atual is null || atual.RevogadoEmUtc is not null)
            return;

        // Logout derruba a cadeia toda, nao so o token atual.
        await RevogarFamiliaAsync(atual.FamiliaId, "logout", ct);
    }

    public async Task RevogarTudoDoUsuarioAsync(Guid usuarioId, string motivo, CancellationToken ct = default)
        => await db.Set<RefreshToken>()
            .Where(t => t.UsuarioId == usuarioId && t.RevogadoEmUtc == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevogadoEmUtc, DateTime.UtcNow)
                .SetProperty(t => t.MotivoRevogacao, motivo), ct);

    private async Task RevogarFamiliaAsync(Guid familiaId, string motivo, CancellationToken ct)
    {
        await db.Set<RefreshToken>()
            .Where(t => t.FamiliaId == familiaId && t.RevogadoEmUtc == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevogadoEmUtc, DateTime.UtcNow)
                .SetProperty(t => t.MotivoRevogacao, motivo), ct);
    }

}
