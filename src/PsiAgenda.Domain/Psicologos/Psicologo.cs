using PsiAgenda.Domain.Common;

namespace PsiAgenda.Domain.Psicologos;

/// <summary>
/// Raiz do tenant: nesta versao 1 conta = 1 profissional, e o Id do Psicologo e o proprio TenantId
/// carregado nos demais agregados. Isso permite evoluir para clinica sem remodelar.
/// </summary>
public class Psicologo : AggregateRoot
{
    public Guid UsuarioId { get; private set; }
    public string NomeCompleto { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public Crp Crp { get; private set; } = null!;

    /// <summary>Res. CFP 11/2018: atendimento online exige cadastro previo do profissional no e-Psi.</summary>
    public bool CadastroEPsiAtivo { get; private set; }

    public int DuracaoPadraoConsultaMinutos { get; private set; } = 50;
    public bool Ativo { get; private set; } = true;

    /// <summary>
    /// IANA. Agendamento e persistido em UTC, mas a disponibilidade e declarada em hora local:
    /// sem o fuso nao da para saber se as 14h UTC caem dentro da janela "seg 09:00-12:00" do profissional.
    /// </summary>
    public string FusoHorario { get; private set; } = "America/Sao_Paulo";

    private readonly List<Disponibilidade> _disponibilidades = [];
    public IReadOnlyCollection<Disponibilidade> Disponibilidades => _disponibilidades.AsReadOnly();

    private Psicologo() { } // EF

    public Psicologo(Guid usuarioId, string nomeCompleto, Email email, Crp crp)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new DomainException("Nome do psicologo e obrigatorio.");

        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        NomeCompleto = nomeCompleto.Trim();
        Email = email;
        Crp = crp;
    }

    public void AtualizarPerfil(string nomeCompleto, int duracaoPadraoMinutos, string fusoHorario)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new DomainException("Nome do psicologo e obrigatorio.");
        if (duracaoPadraoMinutos is < 15 or > 240)
            throw new DomainException("Duracao padrao deve ficar entre 15 e 240 minutos.");
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(fusoHorario, out _))
            throw new DomainException($"Fuso horario invalido: '{fusoHorario}'.");

        NomeCompleto = nomeCompleto.Trim();
        DuracaoPadraoConsultaMinutos = duracaoPadraoMinutos;
        FusoHorario = fusoHorario;
        MarcarAtualizado();
    }

    public void ConfirmarCadastroEPsi()
    {
        CadastroEPsiAtivo = true;
        MarcarAtualizado();
    }

    public Disponibilidade DefinirDisponibilidade(DayOfWeek diaSemana, TimeOnly inicio, TimeOnly fim)
    {
        var nova = new Disponibilidade(Id, diaSemana, inicio, fim);

        if (_disponibilidades.Any(d => d.Conflita(nova)))
            throw new DomainException("Ja existe uma disponibilidade que se sobrepoe a esse horario.");

        _disponibilidades.Add(nova);
        MarcarAtualizado();
        return nova;
    }

    public void RemoverDisponibilidade(Guid disponibilidadeId)
    {
        var alvo = _disponibilidades.FirstOrDefault(d => d.Id == disponibilidadeId)
            ?? throw new DomainException("Disponibilidade nao encontrada.");

        _disponibilidades.Remove(alvo);
        MarcarAtualizado();
    }

    /// <summary>A janela UTC cai dentro de alguma disponibilidade declarada (comparada em hora local)?</summary>
    public bool AceitaHorario(DateTime inicioUtc, DateTime fimUtc)
    {
        var fuso = TimeZoneInfo.FindSystemTimeZoneById(FusoHorario);
        var inicioLocal = TimeZoneInfo.ConvertTimeFromUtc(inicioUtc, fuso);
        var fimLocal = TimeZoneInfo.ConvertTimeFromUtc(fimUtc, fuso);

        // Consulta que vira o dia nunca cabe numa janela semanal de um dia so.
        if (inicioLocal.Date != fimLocal.Date)
            return false;

        return _disponibilidades.Any(d =>
            d.DiaSemana == inicioLocal.DayOfWeek &&
            d.Cobre(TimeOnly.FromDateTime(inicioLocal), TimeOnly.FromDateTime(fimLocal)));
    }

    public void Desativar()
    {
        Ativo = false;
        MarcarAtualizado();
    }
}
