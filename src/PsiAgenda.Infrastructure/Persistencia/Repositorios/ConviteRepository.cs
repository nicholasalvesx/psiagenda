using Microsoft.EntityFrameworkCore;
using PsiAgenda.Domain.Pacientes;

namespace PsiAgenda.Infrastructure.Persistencia.Repositorios;

public class ConviteRepository(AppDbContext db) : IConviteRepository
{
    public Task<ConviteDeAcesso?> ObterPorTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => db.Convites.FirstOrDefaultAsync(c => c.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<ConviteDeAcesso>> ObterEmAbertoAsync(Guid pacienteId, CancellationToken ct = default)
        => await db.Convites
            .Where(c => c.PacienteId == pacienteId && c.UsadoEmUtc == null && c.CanceladoEmUtc == null)
            .ToListAsync(ct);

    public async Task AdicionarAsync(ConviteDeAcesso convite, CancellationToken ct = default)
        => await db.Convites.AddAsync(convite, ct);
}
