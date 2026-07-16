using Dapper;
using PsiAgenda.Application.Agendamentos;
using PsiAgenda.Domain.Agendamentos;

namespace PsiAgenda.Infrastructure.Persistencia.Dapper;

/// <summary>Leitura da agenda em SQL direto: a grade e read-only e nao precisa do change tracker.</summary>
public class AgendaQueries(IConexaoFactory factory) : IAgendaQueries
{
    private const string SelectBase = """
        SELECT  a.id                AS Id,
                a.paciente_id       AS PacienteId,
                p.nome_completo     AS PacienteNome,
                a.inicio_utc        AS InicioUtc,
                a.fim_utc           AS FimUtc,
                a.duracao_minutos   AS DuracaoMinutos,
                a.status            AS Status,
                a.modalidade        AS Modalidade,
                a.sala_video_id     AS SalaVideoId
        FROM    agendamentos a
        JOIN    pacientes p ON p.id = a.paciente_id
        """;

    private sealed record Linha(
        Guid Id, Guid PacienteId, string PacienteNome, DateTime InicioUtc, DateTime FimUtc,
        int DuracaoMinutos, int Status, int Modalidade, Guid? SalaVideoId);

    public async Task<IReadOnlyList<AgendamentoResumo>> ObterAgendaDoPsicologoAsync(
        Guid psicologoId, DateTime deUtc, DateTime ateUtc, CancellationToken ct = default)
    {
        const string sql = $"""
            {SelectBase}
            WHERE   a.psicologo_id = @psicologoId
              AND   a.inicio_utc >= @deUtc
              AND   a.inicio_utc <  @ateUtc
            ORDER BY a.inicio_utc
            """;

        using var conexao = await factory.AbrirAsync(ct);
        var linhas = await conexao.QueryAsync<Linha>(
            new CommandDefinition(sql, new { psicologoId, deUtc, ateUtc }, cancellationToken: ct));

        return [.. linhas.Select(Mapear)];
    }

    public async Task<IReadOnlyList<AgendamentoResumo>> ObterConsultasDoPacienteAsync(
        Guid pacienteId, CancellationToken ct = default)
    {
        const string sql = $"""
            {SelectBase}
            WHERE   a.paciente_id = @pacienteId
            ORDER BY a.inicio_utc DESC
            """;

        using var conexao = await factory.AbrirAsync(ct);
        var linhas = await conexao.QueryAsync<Linha>(
            new CommandDefinition(sql, new { pacienteId }, cancellationToken: ct));

        return [.. linhas.Select(Mapear)];
    }

    private static AgendamentoResumo Mapear(Linha l) => new(
        l.Id,
        l.PacienteId,
        l.PacienteNome,
        DateTime.SpecifyKind(l.InicioUtc, DateTimeKind.Utc),
        DateTime.SpecifyKind(l.FimUtc, DateTimeKind.Utc),
        l.DuracaoMinutos,
        ((StatusAgendamento)l.Status).ToString(),
        ((ModalidadeAtendimento)l.Modalidade).ToString(),
        l.SalaVideoId);
}
