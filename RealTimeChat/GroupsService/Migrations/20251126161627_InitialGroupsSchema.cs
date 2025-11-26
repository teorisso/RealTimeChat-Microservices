using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GroupsService.Migrations
{
    /// <inheritdoc />
    public partial class InitialGroupsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "grupos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreadorId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupos", x => x.Id);
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
                    table.ForeignKey(
                        name: "FK_grupo_miembros_grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_grupo_miembros_GrupoId_UsuarioId",
                table: "grupo_miembros",
                columns: new[] { "GrupoId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grupo_miembros_UsuarioId",
                table: "grupo_miembros",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_grupos_CreadorId",
                table: "grupos",
                column: "CreadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grupo_miembros");

            migrationBuilder.DropTable(
                name: "grupos");
        }
    }
}
