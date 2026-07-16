using System.Text.RegularExpressions;

namespace PsiAgenda.Domain.Common;

public sealed partial record Email
{
    public string Valor { get; }

    private Email(string valor) => Valor = valor;

    public static Email Criar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException("E-mail e obrigatorio.");

        var normalizado = valor.Trim().ToLowerInvariant();
        if (!FormatoEmail().IsMatch(normalizado))
            throw new DomainException($"E-mail invalido: {valor}");

        return new Email(normalizado);
    }

    public override string ToString() => Valor;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex FormatoEmail();
}
