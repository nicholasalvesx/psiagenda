using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PsiAgenda.Domain.Common;

namespace PsiAgenda.Api.Middlewares;

/// <summary>DomainException vira 400 com a mensagem; o resto vira 500 generico e vai para o log.</summary>
public class TratamentoDeErros(ILogger<TratamentoDeErros> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken ct)
    {
        var problema = excecao switch
        {
            DomainException dominio => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Regra de negocio",
                Detail = dominio.Message
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Acesso negado",
                Detail = "Voce nao tem permissao para essa operacao."
            },
            _ => null
        };

        if (problema is null)
        {
            // Erro inesperado: loga o detalhe, devolve mensagem generica (nada de stack trace para o cliente).
            logger.LogError(excecao, "Erro nao tratado em {Rota}", contexto.Request.Path);

            problema = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno",
                Detail = "Ocorreu um erro inesperado. Tente novamente."
            };
        }

        contexto.Response.StatusCode = problema.Status!.Value;
        await contexto.Response.WriteAsJsonAsync(problema, ct);
        return true;
    }
}
