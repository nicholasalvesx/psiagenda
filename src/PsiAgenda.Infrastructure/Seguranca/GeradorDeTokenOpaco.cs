using System.Security.Cryptography;
using System.Text;
using PsiAgenda.Application.Common;

namespace PsiAgenda.Infrastructure.Seguranca;

public class GeradorDeTokenOpaco : IGeradorDeTokenOpaco
{
    public string Gerar()
    {
        // 256 bits: inviavel de adivinhar, e o link do convite continua curto o suficiente.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// SHA-256 sem salt: o token ja tem 256 bits de entropia, entao nao existe dicionario a forcar.
    /// O que importa aqui e que um dump do banco nao entregue convites nem sessoes utilizaveis.
    /// </summary>
    public string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
