namespace PsiAgenda.Domain.Agendamentos;

public enum StatusAgendamento
{
    Pendente = 1,
    Confirmado = 2,
    Concluido = 3,
    Cancelado = 4,
    Falta = 5
}

public enum ModalidadeAtendimento
{
    Online = 1,
    Presencial = 2
}
