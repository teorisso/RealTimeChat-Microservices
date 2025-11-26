using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MessagesService.Migrations
{
    /// <inheritdoc />
    public partial class InitialMessagesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Usuario1Id = table.Column<int>(type: "integer", nullable: true),
                    Usuario2Id = table.Column<int>(type: "integer", nullable: true),
                    GrupoId = table.Column<int>(type: "integer", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "grupo_miembros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GrupoId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    FechaUnion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupo_miembros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "grupos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreadorId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mensajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversacionId = table.Column<int>(type: "integer", nullable: false),
                    RemitenteId = table.Column<int>(type: "integer", nullable: false),
                    Contenido = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mensajes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mensajes_conversaciones_ConversacionId",
                        column: x => x.ConversacionId,
                        principalTable: "conversaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "participantes_conversacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversacionId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    FechaUnion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_participantes_conversacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_participantes_conversacion_conversaciones_ConversacionId",
                        column: x => x.ConversacionId,
                        principalTable: "conversaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mensajes_leidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MensajeId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    FechaLectura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mensajes_leidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mensajes_leidos_mensajes_MensajeId",
                        column: x => x.MensajeId,
                        principalTable: "mensajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_ConversacionId",
                table: "mensajes",
                column: "ConversacionId");

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_FechaEnvio",
                table: "mensajes",
                column: "FechaEnvio");

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_RemitenteId",
                table: "mensajes",
                column: "RemitenteId");

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_leidos_MensajeId_UsuarioId",
                table: "mensajes_leidos",
                columns: new[] { "MensajeId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_participantes_conversacion_ConversacionId_UsuarioId",
                table: "participantes_conversacion",
                columns: new[] { "ConversacionId", "UsuarioId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grupo_miembros");

            migrationBuilder.DropTable(
                name: "grupos");

            migrationBuilder.DropTable(
                name: "mensajes_leidos");

            migrationBuilder.DropTable(
                name: "participantes_conversacion");

            migrationBuilder.DropTable(
                name: "mensajes");

            migrationBuilder.DropTable(
                name: "conversaciones");
        }
    }
}
