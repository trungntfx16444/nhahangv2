namespace PKWebShop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class update_db_product_option_value : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.product_option_value",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        productId = c.String(unicode: false),
                        option1 = c.String(maxLength: 20, storeType: "nvarchar"),
                        option2 = c.String(maxLength: 20, storeType: "nvarchar"),
                        price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        wholeSalePrice = c.Decimal(precision: 18, scale: 2),
                        qty = c.Int(nullable: false),
                        sold = c.Int(nullable: false),
                        PropertyId = c.String(unicode: false),
                        option1_name = c.String(unicode: false),
                        option2_name = c.String(unicode: false),
                    })
                .PrimaryKey(t => t.id);
            
            CreateTable(
                "dbo.product_option",
                c => new
                    {
                        id = c.String(nullable: false, maxLength: 20, storeType: "nvarchar"),
                        name = c.String(unicode: false),
                        parentId = c.String(unicode: false),
                        parentName = c.String(unicode: false),
                        ProductId = c.String(unicode: false),
                    })
                .PrimaryKey(t => t.id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.product_option");
            DropTable("dbo.product_option_value");
        }
    }
}
