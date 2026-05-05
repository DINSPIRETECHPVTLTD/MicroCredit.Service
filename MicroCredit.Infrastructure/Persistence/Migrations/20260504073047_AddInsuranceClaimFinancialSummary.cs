using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCredit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInsuranceClaimFinancialSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "dinspire_sa");

            migrationBuilder.CreateTable(
                name: "Insurance_Claim_Financial_Summary",
                columns: table => new
                {
                    SummaryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TotalInsuranceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TotalClaimedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TotalProcessingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TotalJoiningFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TotalExpenseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insurance_Claim_Financial_Summary", x => x.SummaryId);
                },
                schema: "dinspire_sa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Insurance_Claim_Financial_Summary",
                schema: "dinspire_sa");
        }
    }
}
