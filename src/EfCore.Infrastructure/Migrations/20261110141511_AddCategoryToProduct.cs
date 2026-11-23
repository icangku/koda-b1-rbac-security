using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder mb)
        {
            mb.CreateTable(
                name: "Category",
                schema: "dbo",
                columns: t => new
                {
                    Id = t.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = t.Column<string>(maxLength: 30, nullable: false)
                },
                constraints: t => t.PrimaryKey("PK_Category", x => x.Id));

            mb.CreateIndex(name: "IX_Category_Name", table: "Category",
                column: "Name", schema: "dbo", unique: true);

            // expand: nullable first
            mb.AddColumn<int>(name: "CategoryId", table: "Product",
                schema: "dbo", nullable: true);

            // backfill
            mb.Sql(@"
                INSERT INTO [dbo].[Category] ([Name])
                SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM([Category])), ''), 'Uncategorized')
                FROM [dbo].[Product];");

            mb.Sql(@"
                UPDATE p
                SET p.[CategoryId] = c.[Id]
                FROM [dbo].[Product] p
                JOIN [dbo].[Category] c
                ON c.[Name] = COALESCE(NULLIF(LTRIM(RTRIM(p.[Category])), ''), 'Uncategorized');");

            // contract
            mb.DropColumn(name: "Category", table: "Product", schema: "dbo");

            mb.AlterColumn<int>(name: "CategoryId", table: "Product", schema: "dbo",
                nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);

            mb.CreateIndex(name: "IX_Product_CategoryId", table: "Product",
                column: "CategoryId", schema: "dbo");

            mb.AddForeignKey(
                name: "FK_Product_Category_CategoryId",
                table: "Product", column: "CategoryId", schema: "dbo",
                principalTable: "Category", principalColumn: "Id", principalSchema: "dbo",
                onDelete: ReferentialAction.Restrict);
        }
        /// <inheritdoc />
				protected override void Down(MigrationBuilder mb)
				{
						mb.DropForeignKey(name: "FK_Product_Category_CategoryId",
								table: "Product", schema: "dbo");

						mb.DropIndex(name: "IX_Product_CategoryId",
								table: "Product", schema: "dbo");

						// bring the old string column back, nullable while we refill it
						mb.AddColumn<string>(name: "Category", table: "Product", schema: "dbo",
								type: "nvarchar(30)", maxLength: 30, nullable: true);

						mb.Sql(@"
								UPDATE p
								SET p.[Category] = c.[Name]
								FROM [dbo].[Product] p
								JOIN [dbo].[Category] c ON c.[Id] = p.[CategoryId];");

						mb.AlterColumn<string>(name: "Category", table: "Product", schema: "dbo",
								type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "",
								oldClrType: typeof(string), oldType: "nvarchar(30)",
								oldMaxLength: 30, oldNullable: true);

						mb.DropColumn(name: "CategoryId", table: "Product", schema: "dbo");

						mb.DropTable(name: "Category", schema: "dbo");
				}
    }
}
