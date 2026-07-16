namespace PsiAgenda.Infrastructure.Identity;

public class JwtOptions
{
    public const string SecaoConfig = "Jwt";

    public string Issuer { get; set; } = "psiagenda";
    public string Audience { get; set; } = "psiagenda-app";

    /// <summary>Minimo de 32 bytes para HS256. Em producao vem de secret manager, nunca do appsettings.</summary>
    public string ChaveSecreta { get; set; } = string.Empty;

    /// <summary>Curto de proposito: o access token nao e revogavel, entao a janela de estrago tem que ser pequena.</summary>
    public int ExpiracaoEmMinutos { get; set; } = 15;

    /// <summary>Quanto tempo o usuario fica sem precisar digitar a senha de novo.</summary>
    public int RefreshExpiracaoEmDias { get; set; } = 14;
}
