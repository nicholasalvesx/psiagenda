namespace PsiAgenda.Application.Notificacoes;

public record MensagemDeEmail(string Para, string NomeDoDestinatario, string Assunto, string CorpoHtml, string CorpoTexto);

public interface IEnviadorDeEmail
{
    /// <summary>
    /// Sem CancellationToken de proposito. Se o envio recebesse o token da requisicao, o cliente
    /// fechando a aba logo apos salvar mataria o SMTP no meio — e o paciente ficaria cadastrado
    /// sem nunca receber o convite. O envio tem seu proprio timeout interno.
    /// </summary>
    Task EnviarAsync(MensagemDeEmail mensagem);
}

public class OpcoesDeEmail
{
    public const string SecaoConfig = "Email";

    public string Host { get; set; } = "localhost";
    public int Porta { get; set; } = 1025;
    public bool UsarSsl { get; set; }
    public string? Usuario { get; set; }
    public string? Senha { get; set; }
    public string RemetenteEmail { get; set; } = "nao-responda@psiagenda.local";
    public string RemetenteNome { get; set; } = "PsiAgenda";

    /// <summary>Base do link enviado ao paciente (o portal, nao a API).</summary>
    public string UrlDoPortal { get; set; } = "http://localhost:5173";
}
