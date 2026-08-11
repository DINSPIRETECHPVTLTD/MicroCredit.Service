using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCredit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LoanSchedulers_LoanId",
                schema: "dinspire_sa",
                table: "LoanSchedulers");

            migrationBuilder.AddColumn<int>(
                name: "ParentLoanSchedulerId",
                schema: "dinspire_sa",
                table: "LoanSchedulers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubInstallmentSequence",
                schema: "dinspire_sa",
                table: "LoanSchedulers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RecoveryPostingIdempotency",
                schema: "dinspire_sa",
                columns: table => new
                {
                    ClientRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryPostingIdempotency", x => x.ClientRequestId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoanSchedulers_ParentLoanSchedulerId",
                schema: "dinspire_sa",
                table: "LoanSchedulers",
                column: "ParentLoanSchedulerId");

            migrationBuilder.CreateIndex(
                name: "UX_LoanSchedulers_Loan_Installment_Base",
                schema: "dinspire_sa",
                table: "LoanSchedulers",
                columns: new[] { "LoanId", "InstallmentNo" },
                unique: true,
                filter: "[ParentLoanSchedulerId] IS NULL AND [SubInstallmentSequence] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_LoanSchedulers_Parent_Seq",
                schema: "dinspire_sa",
                table: "LoanSchedulers",
                columns: new[] { "ParentLoanSchedulerId", "SubInstallmentSequence" },
                unique: true,
                filter: "[ParentLoanSchedulerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryPostingIdempotency_OrgId_BranchId_CreatedDate",
                schema: "dinspire_sa",
                table: "RecoveryPostingIdempotency",
                columns: new[] { "OrgId", "BranchId", "CreatedDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_LoanSchedulers_LoanSchedulers_ParentLoanSchedulerId",
                schema: "dinspire_sa",
                table: "LoanSchedulers",
                column: "ParentLoanSchedulerId",
                principalSchema: "dinspire_sa",
                principalTable: "LoanSchedulers",
                principalColumn: "LoanSchedulerId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoanSchedulers_LoanSchedulers_ParentLoanSchedulerId",
                schema: "dinspire_sa",
                table: "LoanSchedulers");

            migrationBuilder.DropTable(
                name: "RecoveryPostingIdempotency",
                schema: "dinspire_sa");

            migrationBuilder.DropIndex(
                name: "IX_LoanSchedulers_ParentLoanSchedulerId",
                schema: "dinspire_sa",
                table: "LoanSchedulers");

            migrationBuilder.DropIndex(
                name: "UX_LoanSchedulers_Loan_Installment_Base",
                schema: "dinspire_sa",
                table: "LoanSchedulers");

            migrationBuilder.DropIndex(
                name: "UX_LoanSchedulers_Parent_Seq",
                schema: "dinspire_sa",
                table: "LoanSchedulers");

            migrationBuilder.DropColumn(
                name: "ParentLoanSchedulerId",
                schema: "dinspire_sa",
                table: "LoanSchedulers");

            migrationBuilder.DropColumn(
                name: "SubInstallmentSequence",
                schema: "dinspire_sa",
                table: "LoanSchedulers");

            migrationBuilder.CreateIndex(
                name: "IX_LoanSchedulers_LoanId",
                schema: "dinspire_sa",
                table: "LoanSchedulers",
                column: "LoanId");
        }
    }
}
