namespace PsiAgenda.Application.Common;

/// <summary>
/// Token aleatorio sem significado, usado em link de convite e em refresh token.
/// So o hash e persistido; o valor cru so existe no e-mail/cookie do destinatario.
/// </summary>
public interface IGeradorDeTokenOpaco
{
    string Gerar();
    string Hash(string token);
}
