using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsiAgenda.Domain.Common;
using PsiAgenda.Domain.Psicologos;

namespace PsiAgenda.Infrastructure.Persistencia.Configuracoes;

public class PsicologoConfiguration : IEntityTypeConfiguration<Psicologo>
{
    public void Configure(EntityTypeBuilder<Psicologo> builder)
    {
        builder.ToTable("psicologos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UsuarioId).IsRequired();
        builder.HasIndex(p => p.UsuarioId).IsUnique();

        builder.Property(p => p.NomeCompleto).HasMaxLength(200).IsRequired();

        builder.Property(p => p.Email)
            .HasConversion(v => v.Valor, v => Email.Criar(v))
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(p => p.Crp)
            .HasConversion(v => v.Valor, v => Crp.Criar(v))
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(p => p.Crp).IsUnique();

        builder.Property(p => p.FusoHorario).HasMaxLength(64).IsRequired();
        builder.Property(p => p.DuracaoPadraoConsultaMinutos).IsRequired();
        builder.Property(p => p.CadastroEPsiAtivo).IsRequired();
        builder.Property(p => p.Ativo).IsRequired();

        builder.OwnsMany(p => p.Disponibilidades, d =>
        {
            d.ToTable("disponibilidades");
            d.WithOwner().HasForeignKey(x => x.PsicologoId);
            d.HasKey(x => x.Id);
            // Id gerado pelo EF no insert: e o que faz a nova disponibilidade ser tratada como Added.
            d.Property(x => x.Id).ValueGeneratedOnAdd();
            d.Property(x => x.DiaSemana).HasConversion<int>().IsRequired();
            d.Property(x => x.HoraInicio).IsRequired();
            d.Property(x => x.HoraFim).IsRequired();
            d.HasIndex(x => new { x.PsicologoId, x.DiaSemana });
        });
        // Owned collection ja vem carregada junto do agregado; AceitaHorario depende disso.
    }
}
