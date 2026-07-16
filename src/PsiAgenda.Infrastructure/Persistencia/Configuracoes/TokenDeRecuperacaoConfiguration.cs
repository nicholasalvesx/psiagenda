using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsiAgenda.Infrastructure.Identity;

namespace PsiAgenda.Infrastructure.Persistencia.Configuracoes;

public class TokenDeRecuperacaoConfiguration : IEntityTypeConfiguration<TokenDeRecuperacao>
{
    public void Configure(EntityTypeBuilder<TokenDeRecuperacao> builder)
    {
        builder.ToTable("tokens_de_recuperacao");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UsuarioId);

        builder.HasOne<UsuarioApp>()
            .WithMany()
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
