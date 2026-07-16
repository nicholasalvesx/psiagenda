namespace PsiAgenda.Api.RateLimit;

public class OpcoesDeRateLimit
{
    public const string SecaoConfig = "RateLimit";

    /// <summary>Desligar so faz sentido em teste de carga. Em producao, manter ligado.</summary>
    public bool Habilitado { get; set; } = true;

    /// <summary>
    /// Rede de protecao para a API inteira. Proposital ser folgado: a agenda dispara varias
    /// chamadas legitimas quando o psicologo navega semanas, e apertar aqui quebra o uso normal.
    /// </summary>
    public Limite Global { get; set; } = new() { Permissoes = 300, JanelaEmMinutos = 1 };

    /// <summary>Forca bruta de senha. O lockout do Identity cuida da conta; isto cuida do IP.</summary>
    public Limite Login { get; set; } = new() { Permissoes = 8, JanelaEmMinutos = 1 };

    /// <summary>Evita usar a recuperacao para inundar a caixa de entrada de alguem.</summary>
    public Limite RecuperacaoDeSenha { get; set; } = new() { Permissoes = 3, JanelaEmMinutos = 60 };

    /// <summary>Criacao de conta em massa.</summary>
    public Limite Registro { get; set; } = new() { Permissoes = 5, JanelaEmMinutos = 60 };
}

public class Limite
{
    public int Permissoes { get; set; }
    public int JanelaEmMinutos { get; set; }

    public TimeSpan Janela => TimeSpan.FromMinutes(JanelaEmMinutos);
}

public static class PoliticasDeRateLimit
{
    public const string Login = "login";
    public const string RecuperacaoDeSenha = "recuperacao-senha";
    public const string Registro = "registro";
}
