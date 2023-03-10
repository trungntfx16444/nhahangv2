namespace PKWebShop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class update_cart : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.cart_detail",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CustomerId = c.String(unicode: false),
                        ShopId = c.String(unicode: false),
                        Type = c.String(unicode: false),
                        ItemId = c.String(unicode: false),
                        PriceOpt1 = c.String(unicode: false),
                        PriceOpt2 = c.String(unicode: false),
                        Properties = c.String(unicode: false),
                        Quantity = c.Int(nullable: false),
                        CreatedAt = c.DateTime(precision: 0),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.orders_detail", "PriceOptId1", c => c.String(unicode: false));
            AddColumn("dbo.orders_detail", "PriceOptId2", c => c.String(unicode: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.orders_detail", "PriceOptId2");
            DropColumn("dbo.orders_detail", "PriceOptId1");
            DropTable("dbo.cart_detail");
        }
    }
}
