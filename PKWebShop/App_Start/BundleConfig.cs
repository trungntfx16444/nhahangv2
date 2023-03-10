using System.Web.Optimization;

namespace PKWebShop
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/js").Include(
               "~/Content/client/js/vendor/modernizr-3.6.0.min.js",
               "~/Content/client/js/plugins/jquery-ui.js",
               "~/Content/client/js/plugins/jquery-ui-touch-punch.js",
               "~/Content/client/js/vendor/jquery-migrate-3.3.0.min.js",
               "~/Content/js/jquery.matchHeight-min.js",
               "~/Content/admin/js/jquery.form.min.js",
               "~/Content/client/js/vendor/bootstrap.bundle.min.js",
               "~/Content/client/js/plugins/slick.js",
               "~/Content/client/js/plugins/jquery.syotimer.min.js",
               "~/Content/client/js/plugins/jquery.instagramfeed.min.js",
               "~/Content/client/js/plugins/jquery.nice-select.min.js",
               "~/Content/client/js/plugins/wow.js",
               "~/Content/client/js/plugins/magnific-popup.js",
               "~/Content/client/js/plugins/sticky-sidebar.js",
               "~/Content/client/js/plugins/easyzoom.js",
               "~/Content/client/js/plugins/scrollup.js",
               //"~/Content/client/js/plugins/ajax-mail.js",
               "~/Content/admin/js/jquery.cookie.js",
               "~/Content/js/noty-cfg.js",
               "~/Content/admin/js/notify/notify.min.js"));

            bundles.Add(new StyleBundle("~/bundles/css").Include(
                "~/Content/client/css/plugins/nice-select.css",
                "~/Content/client/css/plugins/easyzoom.css",
                "~/Content/client/css/plugins/slick.css",
                "~/Content/client/css/plugins/animate.css",
                "~/Content/client/css/plugins/jquery-ui.css",
                "~/Content/client/css/loading-background.css",
                "~/Content/admin/css/noty_theme_default.css",
                "~/Content/css/Custom.css"));
        }
    }
}
