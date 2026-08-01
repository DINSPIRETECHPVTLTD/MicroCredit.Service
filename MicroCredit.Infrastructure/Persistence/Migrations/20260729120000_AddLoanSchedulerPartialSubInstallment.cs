using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCredit.Infrastructure.Persistence.Migrations;

public partial class AddLoanSchedulerPartialSubInstallment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

        migrationBuilder.CreateIndex(
            name: "IX_LoanSchedulers_LoanId_InstallmentNo_SubInstallmentSequence",
            schema: "dinspire_sa",
            table: "LoanSchedulers",
            columns: new[] { "LoanId", "InstallmentNo", "SubInstallmentSequence" });

        migrationBuilder.CreateIndex(
            name: "IX_LoanSchedulers_ParentLoanSchedulerId",
            schema: "dinspire_sa",
            table: "LoanSchedulers",
            column: "ParentLoanSchedulerId");

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

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_LoanSchedulers_LoanSchedulers_ParentLoanSchedulerId",
            schema: "dinspire_sa",
            table: "LoanSchedulers");

        migrationBuilder.DropIndex(
            name: "IX_LoanSchedulers_ParentLoanSchedulerId",
            schema: "dinspire_sa",
            table: "LoanSchedulers");

        migrationBuilder.DropIndex(
            name: "IX_LoanSchedulers_LoanId_InstallmentNo_SubInstallmentSequence",
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
    }
}
