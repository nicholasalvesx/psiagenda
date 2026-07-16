using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace PsiAgenda.Api.RateLimit;

public static class ConfiguracaoDeRateLimit
{
    public static IServiceCollection AddRateLimitDoPsiAgenda(this IServiceCollection services, IConfiguration config)
    {
        var opcoes = config.GetSection(OpcoesDeRateLimit.SecaoConfig).Get<OpcoesDeRateLimit>() ?? new OpcoesDeRateLimit();
        services.Configure<OpcoesDeRateLimit>(config.GetSection(OpcoesDeRateLimit.SecaoConfig));

        services.AddRateLimiter(limiter =>
        {
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
            {
                if (!opcoes.Habilitado)
                    return RateLimitPartition.GetNoLimiter("desligado");

                // O signaling do WebRTC manda uma rajada de candidatos ICE numa conexao longa.
                // Contar isso como requisicao derrubaria a consulta no meio.
                if (contexto.Request.Path.StartsWithSegments("/hubs"))
                    return RateLimitPartition.GetNoLimiter("hubs");

                return RateLimitPartition.GetFixedWindowLimiter(
                    ChaveDoCliente(contexto),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opcoes.Global.Permissoes,
                        Window = opcoes.Global.Janela,
                    });
            });

            // Rotas anonimas e sensiveis: sempre por IP, porque nao ha usuario para partir.
            AdicionarPoliticaPorIp(limiter, PoliticasDeRateLimit.Login, opcoes.Login, opcoes.Habilitado);
            AdicionarPoliticaPorIp(limiter, PoliticasDeRateLimit.RecuperacaoDeSenha, opcoes.RecuperacaoDeSenha, opcoes.Habilitado);
            AdicionarPoliticaPorIp(limiter, PoliticasDeRateLimit.Registro, opcoes.Registro, opcoes.Habilitado);

            limiter.OnRejected = async (contexto, ct) =>
            {
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                    contexto.HttpContext.Response.Headers.RetryAfter =
                        ((int)espera.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);

                contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                // Mesmo formato de erro do resto da API, para o front tratar sem caso especial.
                await contexto.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Muitas tentativas",
                    Detail = "Voce fez muitas tentativas em pouco tempo. Aguarde um instante e tente novamente.",
                }, ct);
            };
        });

        return services;
    }

    private static void AdicionarPoliticaPorIp(
        RateLimiterOptions limiter, string nome, Limite limite, bool habilitado)
        => limiter.AddPolicy(nome, contexto => habilitado
            ? RateLimitPartition.GetFixedWindowLimiter(
                $"{nome}:{Ip(contexto)}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limite.Permissoes,
                    Window = limite.Janela,
                })
            : RateLimitPartition.GetNoLimiter("desligado"));

    /// <summary>
    /// Autenticado, limita a pessoa; anonimo, limita o IP. Limitar sempre por IP puniria a
    /// clinica inteira atras de um NAT por causa de um colega que abusou.
    /// </summary>
    private static string ChaveDoCliente(HttpContext contexto)
    {
        var usuarioId = contexto.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(usuarioId) ? $"ip:{Ip(contexto)}" : $"usuario:{usuarioId}";
    }

    private static string Ip(HttpContext contexto)
        => contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
}
