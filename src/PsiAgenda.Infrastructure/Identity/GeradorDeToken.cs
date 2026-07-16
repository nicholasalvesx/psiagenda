using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PsiAgenda.Application.Auth;
using PsiAgenda.Application.Common;

namespace PsiAgenda.Infrastructure.Identity;

public class GeradorDeToken(IOptions<JwtOptions> opcoes) : IGeradorDeToken
{
    private readonly JwtOptions _opcoes = opcoes.Value;

    public TokenResponse Gerar(
        Guid usuarioId, string email, string role, string nomeExibicao, Guid? psicologoId, Guid? pacienteId)
    {
        var agora = DateTime.UtcNow;
        var expiraEm = agora.AddMinutes(_opcoes.ExpiracaoEmMinutos);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new(ClaimTypes.Name, nomeExibicao),
            // A API autoriza por esta claim ([Authorize(Roles = ...)]).
            new(ClaimTypes.Role, role)
        };

        // O tenant viaja no token: e o que a API usa para nunca cruzar dados entre profissionais.
        if (psicologoId is not null)
            claims.Add(new Claim(ClaimsDoSistema.PsicologoId, psicologoId.Value.ToString()));
        if (pacienteId is not null)
            claims.Add(new Claim(ClaimsDoSistema.PacienteId, pacienteId.Value.ToString()));

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcoes.ChaveSecreta));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opcoes.Issuer,
            audience: _opcoes.Audience,
            claims: claims,
            notBefore: agora,
            expires: expiraEm,
            signingCredentials: credenciais);

        return new TokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiraEm,
            role,
            nomeExibicao);
    }
}
