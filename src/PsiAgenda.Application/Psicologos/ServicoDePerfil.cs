using PsiAgenda.Domain.Common;
using PsiAgenda.Domain.Psicologos;

namespace PsiAgenda.Application.Psicologos;

public record AtualizarPerfilRequest(string NomeCompleto, int DuracaoPadraoConsultaMinutos, string FusoHorario);

public record DefinirDisponibilidadeRequest(DayOfWeek DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFim);

public record DisponibilidadeResumo(Guid Id, DayOfWeek DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFim);

public record PerfilResponse(
    Guid Id,
    string NomeCompleto,
    string Email,
    string Crp,
    string FusoHorario,
    int DuracaoPadraoConsultaMinutos,
    bool CadastroEPsiAtivo,
    IReadOnlyList<DisponibilidadeResumo> Disponibilidades);

public class ServicoDePerfil(IPsicologoRepository psicologos, IUnitOfWork uow)
{
    public async Task<PerfilResponse> ObterAsync(Guid psicologoId, CancellationToken ct = default)
        => Mapear(await CarregarAsync(psicologoId, ct));

    public async Task<PerfilResponse> AtualizarAsync(Guid psicologoId, AtualizarPerfilRequest req, CancellationToken ct = default)
    {
        var psicologo = await CarregarAsync(psicologoId, ct);
        psicologo.AtualizarPerfil(req.NomeCompleto, req.DuracaoPadraoConsultaMinutos, req.FusoHorario);
        await uow.SalvarAlteracoesAsync(ct);
        return Mapear(psicologo);
    }

    public async Task<PerfilResponse> ConfirmarEPsiAsync(Guid psicologoId, CancellationToken ct = default)
    {
        var psicologo = await CarregarAsync(psicologoId, ct);
        psicologo.ConfirmarCadastroEPsi();
        await uow.SalvarAlteracoesAsync(ct);
        return Mapear(psicologo);
    }

    public async Task<PerfilResponse> AdicionarDisponibilidadeAsync(
        Guid psicologoId, DefinirDisponibilidadeRequest req, CancellationToken ct = default)
    {
        var psicologo = await CarregarAsync(psicologoId, ct);
        psicologo.DefinirDisponibilidade(req.DiaSemana, req.HoraInicio, req.HoraFim);
        await uow.SalvarAlteracoesAsync(ct);
        return Mapear(psicologo);
    }

    public async Task<PerfilResponse> RemoverDisponibilidadeAsync(
        Guid psicologoId, Guid disponibilidadeId, CancellationToken ct = default)
    {
        var psicologo = await CarregarAsync(psicologoId, ct);
        psicologo.RemoverDisponibilidade(disponibilidadeId);
        await uow.SalvarAlteracoesAsync(ct);
        return Mapear(psicologo);
    }

    private async Task<Psicologo> CarregarAsync(Guid psicologoId, CancellationToken ct)
        => await psicologos.ObterPorIdAsync(psicologoId, ct)
           ?? throw new DomainException("Psicologo nao encontrado.");

    private static PerfilResponse Mapear(Psicologo p) => new(
        p.Id,
        p.NomeCompleto,
        p.Email.Valor,
        p.Crp.Valor,
        p.FusoHorario,
        p.DuracaoPadraoConsultaMinutos,
        p.CadastroEPsiAtivo,
        [.. p.Disponibilidades
            .OrderBy(d => d.DiaSemana).ThenBy(d => d.HoraInicio)
            .Select(d => new DisponibilidadeResumo(d.Id, d.DiaSemana, d.HoraInicio, d.HoraFim))]);
}
