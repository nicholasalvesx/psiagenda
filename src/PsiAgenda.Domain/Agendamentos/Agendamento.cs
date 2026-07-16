using PsiAgenda.Domain.Common;

namespace PsiAgenda.Domain.Agendamentos;

public class Agendamento : AggregateRoot
{
    /// <summary>Janela em que o paciente ainda pode cancelar por conta propria.</summary>
    public const int HorasMinimasParaCancelamentoPeloPaciente = 24;

    public Guid PsicologoId { get; private set; }
    public Guid PacienteId { get; private set; }
    public DateTime InicioUtc { get; private set; }
    public int DuracaoMinutos { get; private set; }
    public StatusAgendamento Status { get; private set; } = StatusAgendamento.Pendente;
    public ModalidadeAtendimento Modalidade { get; private set; }

    /// <summary>Identificador da sala WebRTC. So existe para atendimento online.</summary>
    public Guid? SalaVideoId { get; private set; }

    public string? Motivo { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public DateTime? CanceladoEmUtc { get; private set; }

    /// <summary>
    /// Derivado de InicioUtc + DuracaoMinutos, mas persistido: a constraint EXCLUDE precisa de uma
    /// expressao IMMUTABLE, e 'timestamptz + interval' e apenas STABLE no Postgres.
    /// </summary>
    public DateTime FimUtc { get; private set; }

    private Agendamento() { } // EF

    public Agendamento(
        Guid psicologoId,
        Guid pacienteId,
        DateTime inicioUtc,
        int duracaoMinutos,
        ModalidadeAtendimento modalidade,
        string? motivo = null)
    {
        if (psicologoId == Guid.Empty || pacienteId == Guid.Empty)
            throw new DomainException("Agendamento exige psicologo e paciente.");
        if (inicioUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("Inicio do agendamento deve estar em UTC.");
        if (inicioUtc <= DateTime.UtcNow)
            throw new DomainException("Nao e possivel agendar em um horario passado.");
        if (duracaoMinutos is < 15 or > 240)
            throw new DomainException("Duracao da consulta deve ficar entre 15 e 240 minutos.");

        Id = Guid.NewGuid();
        PsicologoId = psicologoId;
        PacienteId = pacienteId;
        InicioUtc = inicioUtc;
        FimUtc = inicioUtc.AddMinutes(duracaoMinutos);
        DuracaoMinutos = duracaoMinutos;
        Modalidade = modalidade;
        Motivo = motivo?.Trim();

        if (modalidade == ModalidadeAtendimento.Online)
            SalaVideoId = Guid.NewGuid();
    }

    /// <summary>Sobreposicao de horario. Agendamento cancelado nao ocupa a agenda.</summary>
    public bool Conflita(DateTime inicioUtc, DateTime fimUtc)
        => Status != StatusAgendamento.Cancelado && InicioUtc < fimUtc && inicioUtc < FimUtc;

    public void Confirmar()
    {
        if (Status != StatusAgendamento.Pendente)
            throw new DomainException($"So e possivel confirmar um agendamento pendente (atual: {Status}).");

        Status = StatusAgendamento.Confirmado;
        MarcarAtualizado();
    }

    public void Reagendar(DateTime novoInicioUtc)
    {
        if (Status is StatusAgendamento.Cancelado or StatusAgendamento.Concluido)
            throw new DomainException($"Agendamento {Status} nao pode ser reagendado.");
        if (novoInicioUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("Inicio do agendamento deve estar em UTC.");
        if (novoInicioUtc <= DateTime.UtcNow)
            throw new DomainException("Nao e possivel reagendar para um horario passado.");

        InicioUtc = novoInicioUtc;
        FimUtc = novoInicioUtc.AddMinutes(DuracaoMinutos);
        Status = StatusAgendamento.Pendente;
        MarcarAtualizado();
    }

    public void CancelarPeloPsicologo(string? motivo)
        => Cancelar(motivo, respeitarJanela: false);

    public void CancelarPeloPaciente(string? motivo)
        => Cancelar(motivo, respeitarJanela: true);

    private void Cancelar(string? motivo, bool respeitarJanela)
    {
        if (Status == StatusAgendamento.Cancelado)
            throw new DomainException("Agendamento ja esta cancelado.");
        if (Status == StatusAgendamento.Concluido)
            throw new DomainException("Agendamento ja concluido nao pode ser cancelado.");

        if (respeitarJanela && DateTime.UtcNow > InicioUtc.AddHours(-HorasMinimasParaCancelamentoPeloPaciente))
            throw new DomainException(
                $"O cancelamento pelo paciente exige antecedencia de {HorasMinimasParaCancelamentoPeloPaciente}h. " +
                "Entre em contato com o profissional.");

        Status = StatusAgendamento.Cancelado;
        MotivoCancelamento = motivo?.Trim();
        CanceladoEmUtc = DateTime.UtcNow;
        MarcarAtualizado();
    }

    public void Concluir()
    {
        if (Status is not (StatusAgendamento.Confirmado or StatusAgendamento.Pendente))
            throw new DomainException($"Agendamento {Status} nao pode ser concluido.");
        if (DateTime.UtcNow < InicioUtc)
            throw new DomainException("Nao e possivel concluir uma consulta que ainda nao comecou.");

        Status = StatusAgendamento.Concluido;
        MarcarAtualizado();
    }

    public void RegistrarFalta()
    {
        if (Status is not (StatusAgendamento.Confirmado or StatusAgendamento.Pendente))
            throw new DomainException($"Agendamento {Status} nao aceita registro de falta.");
        if (DateTime.UtcNow < InicioUtc)
            throw new DomainException("Nao e possivel registrar falta antes do horario da consulta.");

        Status = StatusAgendamento.Falta;
        MarcarAtualizado();
    }

    /// <summary>Sala liberada da antecedencia de 10min ate o fim da consulta, e so se confirmada.</summary>
    public bool PodeEntrarNaSala(DateTime agoraUtc)
        => Modalidade == ModalidadeAtendimento.Online
           && Status is StatusAgendamento.Confirmado or StatusAgendamento.Pendente
           && agoraUtc >= InicioUtc.AddMinutes(-10)
           && agoraUtc <= FimUtc;
}
