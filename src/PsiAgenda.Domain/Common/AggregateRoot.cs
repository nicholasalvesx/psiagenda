namespace PsiAgenda.Domain.Common;

public abstract class AggregateRoot : Entity
{
    public DateTime CriadoEmUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEmUtc { get; protected set; }

    protected void MarcarAtualizado() => AtualizadoEmUtc = DateTime.UtcNow;
}
