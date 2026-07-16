using System.Text.RegularExpressions;
using PsiAgenda.Domain.Common;

namespace PsiAgenda.Domain.Psicologos;

/// <summary>Registro no Conselho Regional de Psicologia, no formato RR/NNNNNN (regiao/numero).</summary>
public sealed partial record Crp
{
    public string Valor { get; }

    private Crp(string valor) => Valor = valor;

    public static Crp Criar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException("CRP e obrigatorio.");

        var normalizado = valor.Trim();
        if (!FormatoCrp().IsMatch(normalizado))
            throw new DomainException($"CRP invalido: '{valor}'. Formato esperado: 06/123456.");

        return new Crp(normalizado);
    }

    public override string ToString() => Valor;

    [GeneratedRegex(@"^\d{2}/\d{4,6}$")]
    private static partial Regex FormatoCrp();
}
