using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsiAgenda.Domain.Agendamentos;

namespace PsiAgenda.Infrastructure.Persistencia.Configuracoes;

public class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
    public void Configure(EntityTypeBuilder<Agendamento> builder)
    {
        builder.ToTable("agendamentos");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.PsicologoId).IsRequired();
        builder.Property(a => a.PacienteId).IsRequired();
        builder.Property(a => a.InicioUtc).IsRequired();
        builder.Property(a => a.FimUtc).IsRequired();
        builder.Property(a => a.DuracaoMinutos).IsRequired();
        builder.Property(a => a.Status).HasConversion<int>().IsRequired();
        builder.Property(a => a.Modalidade).HasConversion<int>().IsRequired();
        builder.Property(a => a.Motivo).HasMaxLength(500);
        builder.Property(a => a.MotivoCancelamento).HasMaxLength(500);

        builder.HasIndex(a => new { a.PsicologoId, a.InicioUtc });
        builder.HasIndex(a => new { a.PacienteId, a.InicioUtc });
        builder.HasIndex(a => a.SalaVideoId).IsUnique().HasFilter("sala_video_id IS NOT NULL");

        builder.HasOne<Domain.Psicologos.Psicologo>()
            .WithMany()
            .HasForeignKey(a => a.PsicologoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Pacientes.Paciente>()
            .WithMany()
            .HasForeignKey(a => a.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
