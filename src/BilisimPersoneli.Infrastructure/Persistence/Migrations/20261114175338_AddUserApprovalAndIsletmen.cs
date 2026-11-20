using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilisimPersoneli.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserApprovalAndIsletmen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Mevcut seed / admin hesapları onaylı kalsın
            migrationBuilder.Sql("UPDATE AspNetUsers SET IsApproved = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "AspNetUsers");
        }
    }
}
