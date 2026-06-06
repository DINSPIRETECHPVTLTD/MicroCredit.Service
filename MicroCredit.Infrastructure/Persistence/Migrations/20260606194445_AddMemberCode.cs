using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCredit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MemberCode",
                table: "Members",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_MemberCode",
                table: "Members",
                column: "MemberCode",
                unique: true,
                filter: "[MemberCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Members_MemberCode",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "MemberCode",
                table: "Members");
        }
    }
}
