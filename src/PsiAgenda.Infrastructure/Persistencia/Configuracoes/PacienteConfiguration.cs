using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsiAgenda.Domain.Common;
using PsiAgenda.Domain.Pacientes;

namespace PsiAgenda.Infrastructure.Persistencia.Configuracoes;

public class PacienteConfiguration : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> builder)
    {
        builder.ToTable("pacientes");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PsicologoId).IsRequired();
        builder.Property(p => p.NomeCompleto).HasMaxLength(200).IsRequired();

        builder.Property(p => p.Email)
            .HasConversion(v => v.Valor, v => Email.Criar(v))
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(p => p.Telefone).HasMaxLength(30);
        builder.Property(p => p.Ativo).IsRequired();

        // O mesmo e-mail pode ser paciente de dois profissionais diferentes, mas nao duas vezes do mesmo.
        builder.HasIndex(p => new { p.PsicologoId, p.Email }).IsUnique();
        builder.HasIndex(p => p.UsuarioId).IsUnique().HasFilter("usuario_id IS NOT NULL");

        builder.HasOne<Domain.Psicologos.Psicologo>()
            .WithMany()
            .HasForeignKey(p => p.PsicologoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
