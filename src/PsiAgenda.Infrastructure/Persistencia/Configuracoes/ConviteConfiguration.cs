using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsiAgenda.Domain.Pacientes;

namespace PsiAgenda.Infrastructure.Persistencia.Configuracoes;

public class ConviteConfiguration : IEntityTypeConfiguration<ConviteDeAcesso>
{
    public void Configure(EntityTypeBuilder<ConviteDeAcesso> builder)
    {
        builder.ToTable("convites_de_acesso");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(c => c.TokenHash).IsUnique();
        builder.HasIndex(c => c.PacienteId);

        builder.HasOne<Paciente>()
            .WithMany()
            .HasForeignKey(c => c.PacienteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
