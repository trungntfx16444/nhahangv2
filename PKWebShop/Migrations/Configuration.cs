
using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;

namespace PKWebShop.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<PKWebShop.Models.WebShopEntities>
    {
        public Configuration()
        {
            SetSqlGenerator("MySql.Data.MySqlClient", new MySql.Data.Entity.MySqlMigrationSqlGenerator());
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
        }
    } 
}