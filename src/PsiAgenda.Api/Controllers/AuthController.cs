using Microsoft.AspNetCore.Mvc;
using PsiAgenda.Application.Auth;
using PsiAgenda.Application.Pacientes;
using PsiAgenda.Domain.Common;

namespace PsiAgenda.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    ServicoDeAutenticacao auth,
    ServicoDeConvites convites,
    ServicoDeRecuperacaoDeSenha recuperacao) : ControllerBase
{
    /// <summary>
    /// O refresh token nunca chega ao JavaScript: vive num cookie httpOnly restrito a /api/auth.
    /// Se o front for comprometido por XSS, o atacante leva no maximo um access token de 15 min.
    /// </summary>
    private const string CookieRefresh = "psiagenda_refresh";

    [HttpPost("registrar/psicologo")]
    public async Task<ActionResult<TokenResponse>> RegistrarPsicologo(RegistrarPsicologoRequest req, CancellationToken ct)
        => Responder(await auth.RegistrarPsicologoAsync(req, ct));

    /// <summary>Cria o acesso do paciente a partir do token do convite recebido por e-mail.</summary>
    [HttpPost("registrar/paciente")]
    public async Task<ActionResult<TokenResponse>> RegistrarPaciente(RegistrarPacienteRequest req, CancellationToken ct)
        => Responder(await auth.RegistrarPacienteAsync(req, ct));

    /// <summary>Dados do convite para a tela de cadastro. Publico: o token e a credencial.</summary>
    [HttpGet("convite/{token}")]
    public async Task<ActionResult<ConvitePreview>> ConsultarConvite(string token, CancellationToken ct)
        => Ok(await convites.ConsultarAsync(token, ct));

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest req, CancellationToken ct)
        => Responder(await auth.LoginAsync(req, ct));

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(CookieRefresh, out var refresh) || string.IsNullOrWhiteSpace(refresh))
            return Unauthorized(new { detail = "Sessao nao encontrada." });

        try
        {
            return Responder(await auth.RenovarAsync(refresh, ct));
        }
        catch (DomainException ex)
        {
            // Sessao invalida e 401, nao 400: e o que diz ao cliente para parar de tentar e ir ao login.
            // O cookie morto sai junto, senao o browser reenviaria um refresh que nunca mais vale.
            Response.Cookies.Delete(CookieRefresh, OpcoesDoCookie(DateTimeOffset.UnixEpoch));
            return Unauthorized(new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Sempre 204, exista ou nao a conta. Responder diferente para e-mail desconhecido
    /// entregaria de bandeja a lista de quem tem cadastro.
    /// </summary>
    [HttpPost("recuperar-senha")]
    public async Task<IActionResult> PedirRecuperacao(PedirRecuperacaoRequest req, CancellationToken ct)
    {
        await recuperacao.PedirAsync(req, ct);
        return NoContent();
    }

    /// <summary>Diz se o link ainda vale, para a tela escolher entre o formulario e o aviso.</summary>
    [HttpGet("recuperar-senha/{token}")]
    public async Task<IActionResult> ValidarLinkDeRecuperacao(string token, CancellationToken ct)
        => Ok(new { valido = await recuperacao.TokenEhValidoAsync(token, ct) });

    /// <summary>Redefine e derruba as sessoes. Nao autentica: o usuario entra com a senha nova.</summary>
    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha(RedefinirSenhaRequest req, CancellationToken ct)
    {
        await recuperacao.RedefinirAsync(req, ct);
        Response.Cookies.Delete(CookieRefresh, OpcoesDoCookie(DateTimeOffset.UnixEpoch));
        return NoContent();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(CookieRefresh, out var refresh) && !string.IsNullOrWhiteSpace(refresh))
            await auth.EncerrarSessaoAsync(refresh, ct);

        Response.Cookies.Delete(CookieRefresh, OpcoesDoCookie(DateTimeOffset.UnixEpoch));
        return NoContent();
    }

    private ActionResult<TokenResponse> Responder(ParDeTokens par)
    {
        Response.Cookies.Append(CookieRefresh, par.RefreshToken, OpcoesDoCookie(DateTimeOffset.UtcNow.AddDays(14)));
        return Ok(par.Acesso);
    }

    private CookieOptions OpcoesDoCookie(DateTimeOffset expira) => new()
    {
        HttpOnly = true,
        // Em dev a origem e http://localhost via proxy do Vite; em producao (https) o cookie so viaja cifrado.
        Secure = Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        // Restringe o envio ao endpoint que precisa dele: nenhuma outra rota recebe o refresh.
        Path = "/api/auth",
        Expires = expira,
    };
}
