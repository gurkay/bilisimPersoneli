using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilisimPersoneli.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPersonelTest_v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_personelTests_Personeller_PersonelId",
                table: "personelTests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_personelTests",
                table: "personelTests");

            migrationBuilder.RenameTable(
                name: "personelTests",
                newName: "PersonelTests");

            migrationBuilder.RenameIndex(
                name: "IX_personelTests_PersonelId",
                table: "PersonelTests",
                newName: "IX_PersonelTests_PersonelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonelTests",
                table: "PersonelTests",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonelTests_Personeller_PersonelId",
                table: "PersonelTests",
                column: "PersonelId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonelTests_Personeller_PersonelId",
                table: "PersonelTests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonelTests",
                table: "PersonelTests");

            migrationBuilder.RenameTable(
                name: "PersonelTests",
                newName: "personelTests");

            migrationBuilder.RenameIndex(
                name: "IX_PersonelTests_PersonelId",
                table: "personelTests",
                newName: "IX_personelTests_PersonelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_personelTests",
                table: "personelTests",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_personelTests_Personeller_PersonelId",
                table: "personelTests",
                column: "PersonelId",
                principalTable: "Personeller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
