using PsiAgenda.Domain.Common;

namespace PsiAgenda.Domain.Psicologos;

/// <summary>Janela semanal recorrente em que o psicologo aceita agendamentos.</summary>
public class Disponibilidade : Entity
{
    public Guid PsicologoId { get; private set; }
    public DayOfWeek DiaSemana { get; private set; }
    public TimeOnly HoraInicio { get; private set; }
    public TimeOnly HoraFim { get; private set; }

    private Disponibilidade() { } // EF

    internal Disponibilidade(Guid psicologoId, DayOfWeek diaSemana, TimeOnly horaInicio, TimeOnly horaFim)
    {
        if (horaFim <= horaInicio)
            throw new DomainException("Hora final deve ser maior que a inicial.");

        PsicologoId = psicologoId;
        DiaSemana = diaSemana;
        HoraInicio = horaInicio;
        HoraFim = horaFim;
    }

    public bool Conflita(Disponibilidade outra)
        => DiaSemana == outra.DiaSemana && HoraInicio < outra.HoraFim && outra.HoraInicio < HoraFim;

    public bool Cobre(TimeOnly inicio, TimeOnly fim)
        => inicio >= HoraInicio && fim <= HoraFim;
}
