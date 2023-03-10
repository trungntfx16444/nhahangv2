namespace PKWebShop
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Newtonsoft.Json;
    using PKWebShop.Models;
    using PKWebShop.Models.CustomizeModels;

    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            WebShopEntities db = new WebShopEntities();
            // tạo một đối tượng webconfig
            var web_config = db.webconfigurations.FirstOrDefault() ?? new webconfiguration();
            // kiểm tra dữ webconfig.seo nếu null thì tạo một đối tượng seo mới, còn khác null thì chuyển chỗi json về thành một đối tượng.
            var list_SEO = string.IsNullOrEmpty(web_config.SEO_Meta) ? SiteSEO.New_SEO_Meta() : JsonConvert.DeserializeObject<List<SiteSEO.SEO_Meta>>(web_config.SEO_Meta);
            var SEO_Product = list_SEO.FirstOrDefault(x => x.code == SiteSEO.code_SEO.SanPham);
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "sitemap",
                url: "sitemap.xml",
                defaults: new { Controller = "sitemap", action = "index" },
                namespaces: new string[] { "PKWebShop.Controllers" });

            // trang chu
            routes.MapRoute(
                name: "trang-chu",
                url: string.Empty,
                defaults: new { Controller = "home", action = "index" },
                namespaces: new string[] { "PKWebShop.Controllers" });
            routes.MapRoute(
                name: "doi-ngon-ngu",
                url: "home/ChangeLanguage",
                defaults: new { Controller = "home", action = "ChangeLanguage" },
                namespaces: new string[] { "PKWebShop.Controllers" });
            routes.MapRoute(
                name: "het-han",
                url: "thong-bao-het-han",
                defaults: new { Controller = "home", action = "expired" },
                namespaces: new string[] { "PKWebShop.Controllers" });
            routes.MapRoute(
                name: "notfound",
                url: "notfound",
                defaults: new { controller = "home", action = "notfound" },
                namespaces: new string[] { "PKWebShop.Controllers" });
            routes.MapRoute(
                name: "expired",
                url: "Expired",
                defaults: new { controller = "Home", action = "Expired" },
                namespaces: new string[] { "PKWebShop.Controllers" });


            routes.MapRoute(
                name: "tien-ich",
                url: "tien-ich",
                defaults: new { controller = "Utilities", action = "index", url = "tien-ich" },
                namespaces: new string[] { "PKWebShop.Controllers" });

            //routes.MapRoute(
            //    name: "tin-tuc-tong",
            //    url: "tin-tuc/tong-quan",
            //    defaults: new { controller = "news", action = "Overview", Url = "tin-tuc" },
            //    namespaces: new string[] { "PKWebShop.Controllers" });
            routes.MapRoute(
                name: "giohang",
                url: "gio-hang/{action}",
                defaults: new { controller = "cart", action = "index" },
                namespaces: new string[] { "PKWebShop.Controllers" });
            //routes.MapRoute(
            //    name: "bai-viet-detail",
            //    url: "bai-viet/{detailUrl}",
            //    defaults: new { controller = "news", action = "detail" },
            //    namespaces: new string[] { "PKWebShop.Controllers" });

            routes.MapRoute(
               name: "san-pham",
               url: SEO_Product.url,
               defaults: new { controller = "product", action = "Index" },
               namespaces: new string[] { "PKWebShop.Controllers" });


            routes.MapRoute(
               name: "san-pham-category",
               url: "san-pham/{categoryUrl}",
               defaults: new { controller = "product", action = "Index" },
               namespaces: new string[] { "PKWebShop.Controllers" });
            routes.MapRoute(
               name: "san-pham-detail",
               url: "san-pham/{categoryUrl}/{detailUrl}",
               defaults: new { controller = "product", action = "detail" },
               namespaces: new string[] { "PKWebShop.Controllers" });
            routes.MapRoute(
               name: "hinh-anh",
               url: "hinh-anh",
               defaults: new { controller = "gallery", action = "Index" },
               namespaces: new string[] { "PKWebShop.Controllers" });
            routes.MapRoute(
               name: "lien-he",
               url: "lien-he",
               defaults: new { controller = "contact", action = "index" },
               namespaces: new string[] { "PKWebShop.Controllers" });
            routes.MapRoute(
               name: "dat-phong",
               url: "dat-phong",
               defaults: new { controller = "home", action = "BookingConfirm" },
               namespaces: new string[] { "PKWebShop.Controllers" });
            routes.MapRoute(
               name: "thank-you",
               url: "thank-you",
               defaults: new { controller = "home", action = "thanks" },
               namespaces: new string[] { "PKWebShop.Controllers" });

            routes.MapRoute(
                name: "product-category",
                url: "{url}-pc{cateId}",
                defaults: new { controller = "product", action = "Index" },
                constraints: new { cateId = @"^([a-z0-9]*)$" },
                namespaces: new string[] { "PKWebShop.Controllers" });

            routes.MapRoute(
                name: "product-detail",
                url: "{url}-pd{Id}",
                defaults: new { controller = "product", action = "Detail" },
                constraints: new { Id = @"^([a-z0-9]*)$" },
                namespaces: new string[] { "PKWebShop.Controllers" });

            routes.MapRoute(
                name: "post-detail",
                url: "{detailUrl}-nd{Id}",
                defaults: new { controller = "news", action = "Detail" },
                constraints: new { Id = @"^([a-z0-9]*)$" },
                namespaces: new string[] { "PKWebShop.Controllers" });

            routes.MapRoute(
                name: "tin-tuc",
                url: "{Url}",
                defaults: new { controller = "news", action = "index" },
                namespaces: new string[] { "PKWebShop.Controllers" });


            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new string[] { "PKWebShop.Controllers" });

        }
    }
}
