using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetFamily.Species.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_breeds_species_species_fk_id",
                schema: "species",
                table: "breeds");

            migrationBuilder.RenameColumn(
                name: "species_fk_id",
                schema: "species",
                table: "breeds",
                newName: "species_id");

            migrationBuilder.RenameIndex(
                name: "ix_breeds_species_fk_id",
                schema: "species",
                table: "breeds",
                newName: "ix_breeds_species_id");

            migrationBuilder.AddForeignKey(
                name: "fk_breeds_species_species_id",
                schema: "species",
                table: "breeds",
                column: "species_id",
                principalSchema: "species",
                principalTable: "species",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_breeds_species_species_id",
                schema: "species",
                table: "breeds");

            migrationBuilder.RenameColumn(
                name: "species_id",
                schema: "species",
                table: "breeds",
                newName: "species_fk_id");

            migrationBuilder.RenameIndex(
                name: "ix_breeds_species_id",
                schema: "species",
                table: "breeds",
                newName: "ix_breeds_species_fk_id");

            migrationBuilder.AddForeignKey(
                name: "fk_breeds_species_species_fk_id",
                schema: "species",
                table: "breeds",
                column: "species_fk_id",
                principalSchema: "species",
                principalTable: "species",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
