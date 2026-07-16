namespace PsiAgenda.Application.Common;

/// <summary>Contexto do usuario autenticado, lido das claims do JWT.</summary>
public interface IUsuarioAtual
{
    Guid? UsuarioId { get; }
    Guid? PsicologoId { get; }
    Guid? PacienteId { get; }
    bool EstaAutenticado { get; }
    bool EhPsicologo { get; }
    bool EhPaciente { get; }
}
