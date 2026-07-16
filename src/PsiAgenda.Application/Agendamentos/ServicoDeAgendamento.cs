using PsiAgenda.Domain.Agendamentos;
using PsiAgenda.Domain.Common;
using PsiAgenda.Domain.Pacientes;
using PsiAgenda.Domain.Psicologos;

namespace PsiAgenda.Application.Agendamentos;

public class ServicoDeAgendamento(
    IAgendamentoRepository agendamentos,
    IPsicologoRepository psicologos,
    IPacienteRepository pacientes,
    IUnitOfWork uow)
{
    public async Task<Agendamento> AgendarAsync(Guid psicologoId, AgendarConsultaRequest req, CancellationToken ct = default)
    {
        var psicologo = await psicologos.ObterPorIdAsync(psicologoId, ct)
            ?? throw new DomainException("Psicologo nao encontrado.");

        var paciente = await pacientes.ObterPorIdAsync(req.PacienteId, psicologoId, ct)
            ?? throw new DomainException("Paciente nao encontrado.");
        if (!paciente.Ativo)
            throw new DomainException("Paciente inativo nao pode ser agendado.");

        if (req.Modalidade == ModalidadeAtendimento.Online && !psicologo.CadastroEPsiAtivo)
            throw new DomainException(
                "Para atender online e preciso confirmar o cadastro no e-Psi (Res. CFP 11/2018) no seu perfil.");

        var duracao = req.DuracaoMinutos ?? psicologo.DuracaoPadraoConsultaMinutos;
        var inicioUtc = NormalizarParaUtc(req.InicioUtc);
        var fimUtc = inicioUtc.AddMinutes(duracao);

        if (!psicologo.AceitaHorario(inicioUtc, fimUtc))
            throw new DomainException("Esse horario esta fora da sua disponibilidade cadastrada.");

        await GarantirQueNaoHaConflitoAsync(psicologoId, inicioUtc, fimUtc, ignorar: null, ct);

        var agendamento = new Agendamento(psicologoId, req.PacienteId, inicioUtc, duracao, req.Modalidade, req.Motivo);
        await agendamentos.AdicionarAsync(agendamento, ct);
        await uow.SalvarAlteracoesAsync(ct);

        return agendamento;
    }

    public async Task<Agendamento> ReagendarAsync(Guid psicologoId, Guid agendamentoId, DateTime novoInicioUtc, CancellationToken ct = default)
    {
        var agendamento = await ObterDoTenantAsync(agendamentoId, psicologoId, ct);
        var psicologo = await psicologos.ObterPorIdAsync(psicologoId, ct)
            ?? throw new DomainException("Psicologo nao encontrado.");

        var inicioUtc = NormalizarParaUtc(novoInicioUtc);
        var fimUtc = inicioUtc.AddMinutes(agendamento.DuracaoMinutos);

        if (!psicologo.AceitaHorario(inicioUtc, fimUtc))
            throw new DomainException("Esse horario esta fora da sua disponibilidade cadastrada.");

        await GarantirQueNaoHaConflitoAsync(psicologoId, inicioUtc, fimUtc, ignorar: agendamentoId, ct);

        agendamento.Reagendar(inicioUtc);
        await uow.SalvarAlteracoesAsync(ct);
        return agendamento;
    }

    public async Task ConfirmarAsync(Guid psicologoId, Guid agendamentoId, CancellationToken ct = default)
    {
        var agendamento = await ObterDoTenantAsync(agendamentoId, psicologoId, ct);
        agendamento.Confirmar();
        await uow.SalvarAlteracoesAsync(ct);
    }

    public async Task ConcluirAsync(Guid psicologoId, Guid agendamentoId, CancellationToken ct = default)
    {
        var agendamento = await ObterDoTenantAsync(agendamentoId, psicologoId, ct);
        agendamento.Concluir();
        await uow.SalvarAlteracoesAsync(ct);
    }

    public async Task RegistrarFaltaAsync(Guid psicologoId, Guid agendamentoId, CancellationToken ct = default)
    {
        var agendamento = await ObterDoTenantAsync(agendamentoId, psicologoId, ct);
        agendamento.RegistrarFalta();
        await uow.SalvarAlteracoesAsync(ct);
    }

    public async Task CancelarPeloPsicologoAsync(Guid psicologoId, Guid agendamentoId, string? motivo, CancellationToken ct = default)
    {
        var agendamento = await ObterDoTenantAsync(agendamentoId, psicologoId, ct);
        agendamento.CancelarPeloPsicologo(motivo);
        await uow.SalvarAlteracoesAsync(ct);
    }

    public async Task CancelarPeloPacienteAsync(Guid pacienteId, Guid agendamentoId, string? motivo, CancellationToken ct = default)
    {
        var agendamento = await agendamentos.ObterPorIdAsync(agendamentoId, ct)
            ?? throw new DomainException("Agendamento nao encontrado.");

        // O paciente so enxerga o que e dele.
        if (agendamento.PacienteId != pacienteId)
            throw new DomainException("Agendamento nao encontrado.");

        agendamento.CancelarPeloPaciente(motivo);
        await uow.SalvarAlteracoesAsync(ct);
    }

    private async Task<Agendamento> ObterDoTenantAsync(Guid agendamentoId, Guid psicologoId, CancellationToken ct)
    {
        var agendamento = await agendamentos.ObterPorIdAsync(agendamentoId, ct)
            ?? throw new DomainException("Agendamento nao encontrado.");

        if (agendamento.PsicologoId != psicologoId)
            throw new DomainException("Agendamento nao encontrado.");

        return agendamento;
    }

    /// <summary>
    /// Checagem em memoria; a garantia real contra duas marcacoes simultaneas e a constraint
    /// EXCLUDE do Postgres (ver migration), que impede sobreposicao mesmo em corrida.
    /// </summary>
    private async Task GarantirQueNaoHaConflitoAsync(
        Guid psicologoId, DateTime inicioUtc, DateTime fimUtc, Guid? ignorar, CancellationToken ct)
    {
        var naJanela = await agendamentos.ObterNaJanelaAsync(psicologoId, inicioUtc, fimUtc, ct);

        if (naJanela.Any(a => a.Id != ignorar && a.Conflita(inicioUtc, fimUtc)))
            throw new DomainException("Ja existe uma consulta marcada nesse horario.");
    }

    private static DateTime NormalizarParaUtc(DateTime valor) => valor.Kind switch
    {
        DateTimeKind.Utc => valor,
        DateTimeKind.Local => valor.ToUniversalTime(),
        // Sem offset explicito o cliente mandou algo ambiguo; recusar e melhor que agendar na hora errada.
        _ => throw new DomainException("Informe o horario com fuso explicito (ISO 8601 com 'Z' ou offset).")
    };
}
