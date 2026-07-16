using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsiAgenda.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokensEConvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "convites_de_acesso",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expira_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_convites_de_acesso", x => x.id);
                    table.ForeignKey(
                        name: "fk_convites_de_acesso_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    familia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expira_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revogado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_revogacao = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_asp_net_users_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_convites_de_acesso_paciente_id",
                table: "convites_de_acesso",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_convites_de_acesso_token_hash",
                table: "convites_de_acesso",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_familia_id",
                table: "refresh_tokens",
                column: "familia_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_usuario_id",
                table: "refresh_tokens",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "convites_de_acesso");

            migrationBuilder.DropTable(
                name: "refresh_tokens");
        }
    }
}
