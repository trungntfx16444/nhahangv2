namespace PKWebShop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class update_db_to_ver102206 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.categories", "VendorId", c => c.String(unicode: false));
            AddColumn("dbo.categories", "SubTitle", c => c.String(unicode: false));
            AddColumn("dbo.PackageSettings", "Vendor", c => c.Boolean(nullable: false));
            AddColumn("dbo.PackageSettings", "Comment", c => c.Boolean(nullable: false));
            AddColumn("dbo.vendors", "Logo", c => c.String(unicode: false));
            AddColumn("dbo.vendors", "Description", c => c.String(unicode: false));
            AddColumn("dbo.webconfigurations", "ActiveSales", c => c.Boolean());
        }
        
        public override void Down()
        {
            DropColumn("dbo.webconfigurations", "ActiveSales");
            DropColumn("dbo.vendors", "Description");
            DropColumn("dbo.vendors", "Logo");
            DropColumn("dbo.PackageSettings", "Comment");
            DropColumn("dbo.PackageSettings", "Vendor");
            DropColumn("dbo.categories", "SubTitle");
            DropColumn("dbo.categories", "VendorId");
        }
    }
}
