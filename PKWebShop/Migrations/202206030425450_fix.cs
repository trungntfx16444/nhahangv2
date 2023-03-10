namespace PKWebShop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class fix : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.menu", "Order", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.menu", "Order", c => c.Int());
        }
    }
}
