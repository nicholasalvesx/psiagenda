using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PsiAgenda.Application.Common;
using PsiAgenda.Application.Notificacoes;
using PsiAgenda.Domain.Common;
using PsiAgenda.Domain.Pacientes;
using PsiAgenda.Domain.Psicologos;

namespace PsiAgenda.Application.Auth;

public class ServicoDeRecuperacaoDeSenha(
    IGerenciadorDeUsuarios usuarios,
    IRepositorioDeRecuperacao repositorio,
    IGerenciadorDeRefreshToken refresh,
    IGeradorDeTokenOpaco tokens,
    IEnviadorDeEmail email,
    IPsicologoRepository psicologos,
    IPacienteRepository pacientes,
    IOptions<OpcoesDeEmail> opcoes,
    ILogger<ServicoDeRecuperacaoDeSenha> logger)
{
    private const int ValidadeEmMinutos = 30;
    private readonly OpcoesDeEmail _opcoes = opcoes.Value;

    /// <summary>
    /// Nao devolve nada e nunca falha por e-mail inexistente: qualquer diferenca de resposta
    /// transformaria este endpoint em um verificador de quem tem conta no sistema.
    /// </summary>
    public async Task PedirAsync(PedirRecuperacaoRequest req, CancellationToken ct = default)
    {
        var alvo = Email.Criar(req.Email);
        var usuarioId = await usuarios.ObterIdPorEmailAsync(alvo.Valor, ct);

        if (usuarioId is null)
        {
            logger.LogInformation("Recuperacao pedida para e-mail sem conta; nada enviado.");
            return;
        }

        // Pedir um link novo invalida os anteriores.
        await repositorio.InvalidarEmAbertoAsync(usuarioId.Value, ct);

        var token = tokens.Gerar();
        await repositorio.CriarAsync(usuarioId.Value, tokens.Hash(token), ValidadeEmMinutos, ct);

        var nome = await ObterNomeAsync(usuarioId.Value, ct);
        await email.EnviarAsync(MontarEmail(alvo.Valor, nome, token));
    }

    /// <summary>Checagem para a tela decidir entre mostrar o formulario ou o aviso de link vencido.</summary>
    public async Task<bool> TokenEhValidoAsync(string token, CancellationToken ct = default)
        => !string.IsNullOrWhiteSpace(token)
           && await repositorio.ObterValidoAsync(tokens.Hash(token), ct) is not null;

    public async Task RedefinirAsync(RedefinirSenhaRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
            throw new DomainException("Link invalido ou expirado. Peca a recuperacao novamente.");

        var info = await repositorio.ObterValidoAsync(tokens.Hash(req.Token), ct)
            ?? throw new DomainException("Link invalido ou expirado. Peca a recuperacao novamente.");

        // A politica de senha e validada aqui dentro; se reprovar, o token continua de pe
        // para o usuario tentar outra senha sem pedir um link novo.
        await usuarios.RedefinirSenhaAsync(info.UsuarioId, req.NovaSenha, ct);

        await repositorio.ConsumirAsync(info.Id, ct);

        // Trocar a senha tem que expulsar quem ja estava logado — inclusive o invasor
        // que motivou a troca. Sem isso, o refresh dele continuaria valendo por 14 dias.
        await refresh.RevogarTudoDoUsuarioAsync(info.UsuarioId, "senha redefinida", ct);
    }

    private async Task<string> ObterNomeAsync(Guid usuarioId, CancellationToken ct)
    {
        var psicologo = await psicologos.ObterPorUsuarioIdAsync(usuarioId, ct);
        if (psicologo is not null) return psicologo.NomeCompleto;

        var paciente = await pacientes.ObterPorUsuarioIdAsync(usuarioId, ct);
        return paciente?.NomeCompleto ?? "";
    }

    private MensagemDeEmail MontarEmail(string destino, string nome, string token)
    {
        var link = $"{_opcoes.UrlDoPortal.TrimEnd('/')}/redefinir-senha?token={Uri.EscapeDataString(token)}";
        var primeiroNome = string.IsNullOrWhiteSpace(nome) ? "" : $", {nome.Split(' ')[0]}";

        var texto = $"""
            Ola{primeiroNome}!

            Recebemos um pedido para redefinir a senha da sua conta no PsiAgenda.

            Crie uma nova senha aqui: {link}

            O link vale por {ValidadeEmMinutos} minutos e so pode ser usado uma vez.
            Se voce nao pediu isso, ignore este e-mail: sua senha continua a mesma.
            """;

        var html = $"""
            <div style="font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;max-width:520px;margin:0 auto;color:#0f172a">
              <div style="background:#1d4ed8;color:#fff;padding:20px;border-radius:12px 12px 0 0">
                <h1 style="margin:0;font-size:20px">PsiAgenda</h1>
              </div>
              <div style="border:1px solid #e2e8f0;border-top:0;border-radius:0 0 12px 12px;padding:24px">
                <p>Ola{primeiroNome}!</p>
                <p>Recebemos um pedido para redefinir a senha da sua conta no PsiAgenda.</p>
                <p style="text-align:center;margin:28px 0">
                  <a href="{link}" style="background:#1d4ed8;color:#fff;text-decoration:none;padding:12px 24px;border-radius:8px;display:inline-block;font-weight:500">
                    Criar nova senha
                  </a>
                </p>
                <p style="color:#64748b;font-size:13px">
                  O link vale por {ValidadeEmMinutos} minutos e so pode ser usado uma vez.<br>
                  Se voce nao pediu isso, ignore este e-mail: sua senha continua a mesma.
                </p>
              </div>
            </div>
            """;

        return new MensagemDeEmail(destino, nome, "Redefinir sua senha no PsiAgenda", html, texto);
    }
}
