namespace PsiAgenda.Application.Common;

public static class RolesDoSistema
{
    public const string Psicologo = "Psicologo";
    public const string Paciente = "Paciente";

    public static readonly IReadOnlyList<string> Todas = [Psicologo, Paciente];
}

/// <summary>Claims proprias do token, alem das do Identity.</summary>
public static class ClaimsDoSistema
{
    /// <summary>Id do agregado Psicologo (= tenant). Presente no token das duas roles.</summary>
    public const string PsicologoId = "psicologo_id";

    /// <summary>Id do agregado Paciente. So existe no token da role Paciente.</summary>
    public const string PacienteId = "paciente_id";
}
