using Microsoft.EntityFrameworkCore;
using PsiAgenda.Domain.Agendamentos;

namespace PsiAgenda.Infrastructure.Persistencia.Repositorios;

public class AgendamentoRepository(AppDbContext db) : IAgendamentoRepository
{
    public Task<Agendamento?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Agendamento?> ObterPorSalaVideoAsync(Guid salaVideoId, CancellationToken ct = default)
        => db.Agendamentos.FirstOrDefaultAsync(a => a.SalaVideoId == salaVideoId, ct);

    public async Task<IReadOnlyList<Agendamento>> ObterNaJanelaAsync(
        Guid psicologoId, DateTime inicioUtc, DateTime fimUtc, CancellationToken ct = default)
    {
        // Filtro por Inicio dos dois lados; a sobreposicao exata quem decide e Agendamento.Conflita.
        // A margem de 4h para tras cobre a consulta que comeca antes da janela e invade ela.
        var limiteInferior = inicioUtc.AddHours(-4);

        return await db.Agendamentos
            .Where(a => a.PsicologoId == psicologoId
                        && a.Status != StatusAgendamento.Cancelado
                        && a.InicioUtc >= limiteInferior
                        && a.InicioUtc < fimUtc)
            .ToListAsync(ct);
    }

    public async Task AdicionarAsync(Agendamento agendamento, CancellationToken ct = default)
        => await db.Agendamentos.AddAsync(agendamento, ct);
}
