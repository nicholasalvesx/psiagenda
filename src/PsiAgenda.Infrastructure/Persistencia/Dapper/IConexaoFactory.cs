using System.Data;
using Npgsql;

namespace PsiAgenda.Infrastructure.Persistencia.Dapper;

public interface IConexaoFactory
{
    Task<IDbConnection> AbrirAsync(CancellationToken ct = default);
}

public class ConexaoFactory(string connectionString) : IConexaoFactory
{
    public async Task<IDbConnection> AbrirAsync(CancellationToken ct = default)
    {
        var conexao = new NpgsqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        return conexao;
    }
}
