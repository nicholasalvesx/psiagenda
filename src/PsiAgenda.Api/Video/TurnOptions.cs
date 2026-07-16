namespace PsiAgenda.Api.Video;

public class TurnOptions
{
    public const string SecaoConfig = "Turn";

    /// <summary>
    /// Default vazio de proposito: o binder de configuracao ACRESCENTA itens a um array ja preenchido
    /// em vez de substituir, e um default aqui vazaria para dentro do que o appsettings define.
    /// </summary>
    public string[] StunUrls { get; set; } = [];

    /// <summary>Sem TURN, conexoes atras de CGNAT/NAT simetrico simplesmente nao fecham.</summary>
    public string[] TurnUrls { get; set; } = [];
    public string Usuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public record IceServer(string[] Urls, string? Username = null, string? Credential = null);

public record SalaResponse(Guid SalaVideoId, DateTime InicioUtc, DateTime FimUtc, string PapelNaSala, IceServer[] IceServers);
