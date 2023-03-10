namespace PKWebShop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class fix1 : DbMigration
    {
        public override void Up()
        {
            //DropPrimaryKey("product_option_value");
            //DropPrimaryKey("product_option");
            AlterColumn("product_option_value", "id", c => c.Int(nullable: false, identity: true));
            AlterColumn("product_option", "id", c => c.String(nullable: false, maxLength: 20, storeType: "nvarchar"));
            //AddPrimaryKey("product_option_value", "id");
            //AddPrimaryKey("product_option", "id");
        }
        
        public override void Down()
        {
            DropPrimaryKey("product_option");
            DropPrimaryKey("product_option_value");
            AlterColumn("product_option", "id", c => c.String(maxLength: 50, storeType: "nvarchar"));
            AlterColumn("product_option_value", "id", c => c.String(maxLength: 50, storeType: "nvarchar"));
            AddPrimaryKey("product_option", "id");
            AddPrimaryKey("product_option_value", "id");
        }
    }
}
