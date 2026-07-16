using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsiAgenda.Infrastructure.Persistencia.Migrations
{
    /// <summary>
    /// Trava de sobreposicao no proprio banco. A checagem em ServicoDeAgendamento roda em duas etapas
    /// (consulta e depois insere), entao dois pedidos simultaneos passariam pelos dois checks e gravariam
    /// consultas em cima uma da outra. So o banco resolve isso de verdade.
    /// Status 4 = Cancelado: agendamento cancelado libera o horario.
    /// </summary>
    public partial class RestricaoDeSobreposicaoDeAgenda : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Necessario para combinar '=' (uuid) e '&&' (range) no mesmo indice GiST.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            // tstzrange sobre duas colunas e IMMUTABLE; 'inicio_utc + interval' seria apenas STABLE
            // e o Postgres recusa expressao STABLE em indice — por isso fim_utc e coluna persistida.
            migrationBuilder.Sql("""
                ALTER TABLE agendamentos
                ADD CONSTRAINT ck_agendamentos_sem_sobreposicao
                EXCLUDE USING gist (
                    psicologo_id WITH =,
                    tstzrange(inicio_utc, fim_utc, '[)') WITH &&
                )
                WHERE (status <> 4);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE agendamentos DROP CONSTRAINT IF EXISTS ck_agendamentos_sem_sobreposicao;");
        }
    }
}
