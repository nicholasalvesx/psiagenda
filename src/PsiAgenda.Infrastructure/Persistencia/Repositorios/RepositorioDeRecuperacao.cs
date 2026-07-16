using Microsoft.EntityFrameworkCore;
using PsiAgenda.Application.Auth;
using PsiAgenda.Infrastructure.Identity;

namespace PsiAgenda.Infrastructure.Persistencia.Repositorios;

public class RepositorioDeRecuperacao(AppDbContext db) : IRepositorioDeRecuperacao
{
    public async Task CriarAsync(Guid usuarioId, string tokenHash, int validadeEmMinutos, CancellationToken ct = default)
    {
        db.TokensDeRecuperacao.Add(new TokenDeRecuperacao
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            TokenHash = tokenHash,
            CriadoEmUtc = DateTime.UtcNow,
            ExpiraEmUtc = DateTime.UtcNow.AddMinutes(validadeEmMinutos),
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<TokenDeRecuperacaoInfo?> ObterValidoAsync(string tokenHash, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;

        return await db.TokensDeRecuperacao
            .AsNoTracking()
            .Where(t => t.TokenHash == tokenHash
                        && t.UsadoEmUtc == null
                        && t.InvalidadoEmUtc == null
                        && t.ExpiraEmUtc > agora)
            .Select(t => new TokenDeRecuperacaoInfo(t.Id, t.UsuarioId))
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>UPDATE condicional: dois cliques no mesmo link nao podem redefinir a senha duas vezes.</summary>
    public async Task ConsumirAsync(Guid id, CancellationToken ct = default)
        => await db.TokensDeRecuperacao
            .Where(t => t.Id == id && t.UsadoEmUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEmUtc, DateTime.UtcNow), ct);

    public async Task InvalidarEmAbertoAsync(Guid usuarioId, CancellationToken ct = default)
        => await db.TokensDeRecuperacao
            .Where(t => t.UsuarioId == usuarioId && t.UsadoEmUtc == null && t.InvalidadoEmUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.InvalidadoEmUtc, DateTime.UtcNow), ct);
}
