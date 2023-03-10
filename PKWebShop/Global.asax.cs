using PKWebShop.Utils;

namespace PKWebShop
{
    using System;
    using System.Data.Entity;
    using System.Globalization;
    using System.Linq;
    using System.Web;
    using System.Web.Http;
    using System.Web.Mvc;
    using System.Web.Optimization;
    using System.Web.Routing;
    using PKWebShop.Models;
    using PKWebShop.Models;
    using PKWebShop.Services;

    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            CultureInfo ci = CultureInfo.CreateSpecificCulture("vi-VN");
            ci.DateTimeFormat.ShortDatePattern = "dd-MM-yyyy";
            CultureInfo.CurrentCulture = ci;
            CultureInfo.CurrentUICulture = ci;
            CultureInfo.DefaultThreadCurrentCulture = ci;

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            Database.SetInitializer<WebShopEntities>(null);
        }

        protected void Session_Start()
        {
            WebShopEntities db = new ();
            Random rd = new ();
            var viewCountTracker = db.viewpagetrackers.Where(t => t.Date == DateTime.Today).FirstOrDefault();
            int viewCount = (viewCountTracker?.ViewCount ?? 0) + 1;
            if (viewCountTracker == null)
            {
                db.viewpagetrackers.Add(new viewpagetracker { Id = AppFunc.NewShortId(), Date = DateTime.Today, ViewCount = viewCount });
            }
            else
            {
                viewCountTracker.ViewCount++;
                db.Entry(viewCountTracker).State = System.Data.Entity.EntityState.Modified;
            }
            db.SaveChanges();

            // get language in cookie
            string lang = null;
            HttpCookie langCookie = Request.Cookies["culture"];
            if (langCookie != null)
            {
                lang = langCookie.Value;
            }
            else
            {
                var userLanguage = Request.UserLanguages;
                var userLang = userLanguage != null ? userLanguage[0] : string.Empty;
            }

            SiteLang.SetLanguage(lang);
        }
    }
}
