using Microsoft.EntityFrameworkCore;
using PsiAgenda.Domain.Common;
using PsiAgenda.Domain.Pacientes;

namespace PsiAgenda.Infrastructure.Persistencia.Repositorios;

public class PacienteRepository(AppDbContext db) : IPacienteRepository
{
    public Task<Paciente?> ObterPorIdAsync(Guid id, Guid psicologoId, CancellationToken ct = default)
        => db.Pacientes.FirstOrDefaultAsync(p => p.Id == id && p.PsicologoId == psicologoId, ct);

    public Task<Paciente?> ObterPorUsuarioIdAsync(Guid usuarioId, CancellationToken ct = default)
        => db.Pacientes.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

    public Task<Paciente?> ObterPorIdSemTenantAsync(Guid id, CancellationToken ct = default)
        => db.Pacientes.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExisteComEmailAsync(string email, Guid psicologoId, CancellationToken ct = default)
    {
        var alvo = Email.Criar(email);
        return db.Pacientes.AnyAsync(p => p.Email == alvo && p.PsicologoId == psicologoId, ct);
    }

    public async Task AdicionarAsync(Paciente paciente, CancellationToken ct = default)
        => await db.Pacientes.AddAsync(paciente, ct);
}
