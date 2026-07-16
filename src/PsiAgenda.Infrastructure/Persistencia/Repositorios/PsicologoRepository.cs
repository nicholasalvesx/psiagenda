using Microsoft.EntityFrameworkCore;
using PsiAgenda.Domain.Psicologos;

namespace PsiAgenda.Infrastructure.Persistencia.Repositorios;

public class PsicologoRepository(AppDbContext db) : IPsicologoRepository
{
    public Task<Psicologo?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => db.Psicologos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Psicologo?> ObterPorUsuarioIdAsync(Guid usuarioId, CancellationToken ct = default)
        => db.Psicologos.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

    public Task<bool> ExisteComCrpAsync(string crp, CancellationToken ct = default)
    {
        // Criar fora da expressao: o EF nao traduz a chamada, so a comparacao do valor convertido.
        var alvo = Crp.Criar(crp);
        return db.Psicologos.AnyAsync(p => p.Crp == alvo, ct);
    }

    public async Task AdicionarAsync(Psicologo psicologo, CancellationToken ct = default)
        => await db.Psicologos.AddAsync(psicologo, ct);
}
