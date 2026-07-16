using Microsoft.AspNetCore.Identity;

namespace PsiAgenda.Infrastructure.Identity;

/// <summary>Login. O "quem e" (Psicologo/Paciente) mora no dominio, ligado por UsuarioId.</summary>
public class UsuarioApp : IdentityUser<Guid>
{
    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
}

public class PerfilApp : IdentityRole<Guid>
{
    public PerfilApp() { }
    public PerfilApp(string nome) : base(nome) { }
}
