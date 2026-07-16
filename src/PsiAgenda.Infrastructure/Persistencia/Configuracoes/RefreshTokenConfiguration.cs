using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsiAgenda.Infrastructure.Identity;

namespace PsiAgenda.Infrastructure.Persistencia.Configuracoes;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.FamiliaId);
        builder.HasIndex(t => t.UsuarioId);

        builder.Property(t => t.MotivoRevogacao).HasMaxLength(60);
        builder.Ignore(t => t.Ativo);

        // Apagar o login leva as sessoes junto.
        builder.HasOne<UsuarioApp>()
            .WithMany()
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
