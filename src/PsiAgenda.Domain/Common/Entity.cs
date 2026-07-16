namespace PsiAgenda.Domain.Common;

public abstract class Entity
{
    /// <summary>
    /// Sem valor default de proposito. As raizes atribuem o Id no construtor; entidades filhas
    /// deixam vazio para o EF gerar. Se toda entidade nascesse com Guid.NewGuid(), o EF veria a
    /// chave preenchida ao descobrir a filha no grafo e a trataria como registro existente (UPDATE).
    /// </summary>
    public Guid Id { get; protected set; }

    public override bool Equals(object? obj)
        => obj is Entity other && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
