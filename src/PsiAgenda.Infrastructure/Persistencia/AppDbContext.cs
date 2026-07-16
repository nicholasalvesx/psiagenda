using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PsiAgenda.Domain.Agendamentos;
using PsiAgenda.Domain.Common;
using PsiAgenda.Domain.Pacientes;
using PsiAgenda.Domain.Psicologos;
using PsiAgenda.Infrastructure.Identity;

namespace PsiAgenda.Infrastructure.Persistencia;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<UsuarioApp, PerfilApp, Guid>(options), IUnitOfWork
{
    public DbSet<Psicologo> Psicologos => Set<Psicologo>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ConviteDeAcesso> Convites => Set<ConviteDeAcesso>();
    public DbSet<TokenDeRecuperacao> TokensDeRecuperacao => Set<TokenDeRecuperacao>();

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Tabelas do Identity com nomes previsiveis no Postgres.
        builder.Entity<UsuarioApp>().ToTable("usuarios");
        builder.Entity<PerfilApp>().ToTable("perfis");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("usuario_perfis");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("usuario_claims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("usuario_logins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("usuario_tokens");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("perfil_claims");
    }
}
