using System.Security.Claims;
using PsiAgenda.Application.Common;

namespace PsiAgenda.Api.Autenticacao;

/// <summary>Le o contexto do tenant direto das claims do JWT — nunca de parametro vindo do cliente.</summary>
public class UsuarioAtual(IHttpContextAccessor acessor) : IUsuarioAtual
{
    private ClaimsPrincipal? Principal => acessor.HttpContext?.User;

    public bool EstaAutenticado => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UsuarioId => LerGuid(ClaimTypes.NameIdentifier);
    public Guid? PsicologoId => LerGuid(ClaimsDoSistema.PsicologoId);
    public Guid? PacienteId => LerGuid(ClaimsDoSistema.PacienteId);

    public bool EhPsicologo => Principal?.IsInRole(RolesDoSistema.Psicologo) ?? false;
    public bool EhPaciente => Principal?.IsInRole(RolesDoSistema.Paciente) ?? false;

    private Guid? LerGuid(string claim)
        => Guid.TryParse(Principal?.FindFirstValue(claim), out var valor) ? valor : null;
}

public static class UsuarioAtualExtensions
{
    public static Guid PsicologoIdObrigatorio(this IUsuarioAtual usuario)
        => usuario.PsicologoId ?? throw new UnauthorizedAccessException("Token sem psicologo_id.");

    public static Guid PacienteIdObrigatorio(this IUsuarioAtual usuario)
        => usuario.PacienteId ?? throw new UnauthorizedAccessException("Token sem paciente_id.");
}
