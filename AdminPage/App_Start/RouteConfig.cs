using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Newtonsoft.Json;
using AdminPage.Models;
using AdminPage.Models.CustomizeModels;

namespace AdminPage
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            var db = new AdminEntities();
            var web_config = db.webconfigurations.FirstOrDefault() ?? new webconfiguration();
            var list_SEO = string.IsNullOrEmpty(web_config.SEO_Meta) ? SiteSEO.New_SEO_Meta() : JsonConvert.DeserializeObject<List<SiteSEO.SEO_Meta>>(web_config.SEO_Meta);
            var SEO_Product = list_SEO.FirstOrDefault(x => x.code == SiteSEO.code_SEO.SanPham);

            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
               name: "login",
               url: "login",
               defaults: new { Controller = "home", action = "login" },
               namespaces: new string[] { "AdminPage.Controllers" });

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
