using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DynamicObjectAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddModifyDateToDynamicObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModifyDate",
                table: "DynamicObjects",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifyDate",
                table: "DynamicObjects");
        }
    }
}
