using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DynamicObjectAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterObjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MasterObjectId",
                table: "DynamicObjects",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MasterObjectId",
                table: "DynamicObjects");
        }
    }
}
