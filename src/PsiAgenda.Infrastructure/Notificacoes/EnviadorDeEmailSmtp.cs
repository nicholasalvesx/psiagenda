using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PsiAgenda.Application.Notificacoes;

namespace PsiAgenda.Infrastructure.Notificacoes;

public class EnviadorDeEmailSmtp(
    IOptions<OpcoesDeEmail> opcoes,
    ILogger<EnviadorDeEmailSmtp> logger) : IEnviadorDeEmail
{
    private readonly OpcoesDeEmail _opcoes = opcoes.Value;

    /// <summary>Timeout proprio: nao pode ficar preso para sempre, mas tambem nao morre com o request.</summary>
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(20);

    public async Task EnviarAsync(MensagemDeEmail mensagem)
    {
        using var cts = new CancellationTokenSource(Limite);
        var ct = cts.Token;

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_opcoes.RemetenteNome, _opcoes.RemetenteEmail));
        email.To.Add(new MailboxAddress(mensagem.NomeDoDestinatario, mensagem.Para));
        email.Subject = mensagem.Assunto;

        email.Body = new BodyBuilder
        {
            HtmlBody = mensagem.CorpoHtml,
            TextBody = mensagem.CorpoTexto,
        }.ToMessageBody();

        using var cliente = new SmtpClient();

        var seguranca = _opcoes.UsarSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None; // Mailpit local nao fala TLS

        await cliente.ConnectAsync(_opcoes.Host, _opcoes.Porta, seguranca, ct);

        if (!string.IsNullOrWhiteSpace(_opcoes.Usuario))
            await cliente.AuthenticateAsync(_opcoes.Usuario, _opcoes.Senha, ct);

        await cliente.SendAsync(email, ct);
        await cliente.DisconnectAsync(true, ct);

        // Nunca logar o corpo: o link do convite da acesso a conta do paciente.
        logger.LogInformation("E-mail '{Assunto}' enviado para {Destinatario}", mensagem.Assunto, mensagem.Para);
    }
}
