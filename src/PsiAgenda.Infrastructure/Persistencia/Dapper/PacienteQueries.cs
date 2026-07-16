using Dapper;
using PsiAgenda.Application.Pacientes;
using PsiAgenda.Domain.Agendamentos;

namespace PsiAgenda.Infrastructure.Persistencia.Dapper;

public class PacienteQueries(IConexaoFactory factory) : IPacienteQueries
{
    /// <summary>
    /// Classe com setters, nao record posicional: o Dapper exige um construtor que case com os tipos
    /// do reader, e o Npgsql entrega 'date' como DateTime — a conversao para DateOnly fica no mapeamento.
    /// </summary>
    private sealed class Linha
    {
        public Guid Id { get; set; }
        public string NomeCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public DateTime? DataNascimento { get; set; }
        public bool PossuiAcessoAoPortal { get; set; }
        public bool Ativo { get; set; }
        public DateTime? ProximaConsultaUtc { get; set; }
    }

    public async Task<IReadOnlyList<PacienteResumo>> ListarAsync(
        Guid psicologoId, string? busca, bool apenasAtivos, CancellationToken ct = default)
    {
        // A proxima consulta vem de um LATERAL: evita N+1 e mantem a lista em uma unica ida ao banco.
        const string sql = """
            SELECT  p.id              AS Id,
                    p.nome_completo   AS NomeCompleto,
                    p.email           AS Email,
                    p.telefone        AS Telefone,
                    p.data_nascimento AS DataNascimento,
                    (p.usuario_id IS NOT NULL) AS PossuiAcessoAoPortal,
                    p.ativo           AS Ativo,
                    prox.inicio_utc   AS ProximaConsultaUtc
            FROM    pacientes p
            LEFT JOIN LATERAL (
                SELECT  a.inicio_utc
                FROM    agendamentos a
                WHERE   a.paciente_id = p.id
                  AND   a.status = ANY(@statusAtivos)
                  AND   a.inicio_utc >= now()
                ORDER BY a.inicio_utc
                LIMIT 1
            ) prox ON true
            WHERE   p.psicologo_id = @psicologoId
              AND   (@apenasAtivos = false OR p.ativo = true)
              AND   (@busca IS NULL OR p.nome_completo ILIKE @padraoBusca OR p.email ILIKE @padraoBusca)
            ORDER BY p.nome_completo
            """;

        var statusAtivos = new[] { (int)StatusAgendamento.Pendente, (int)StatusAgendamento.Confirmado };
        var buscaLimpa = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim();

        using var conexao = await factory.AbrirAsync(ct);
        var linhas = await conexao.QueryAsync<Linha>(new CommandDefinition(sql, new
        {
            psicologoId,
            apenasAtivos,
            busca = buscaLimpa,
            padraoBusca = buscaLimpa is null ? null : $"%{buscaLimpa}%",
            statusAtivos
        }, cancellationToken: ct));

        return [.. linhas.Select(l => new PacienteResumo(
            l.Id,
            l.NomeCompleto,
            l.Email,
            l.Telefone,
            l.DataNascimento is null ? null : DateOnly.FromDateTime(l.DataNascimento.Value),
            l.PossuiAcessoAoPortal,
            l.Ativo,
            l.ProximaConsultaUtc is null ? null : DateTime.SpecifyKind(l.ProximaConsultaUtc.Value, DateTimeKind.Utc)))];
    }
}
