namespace AdminPage.AppLB
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Resources;
    using System.Web;
    using System.Web.Routing;
    using System.Web.Security;
    using Inner.Libs.Helpful;
    using Newtonsoft.Json;
    using AdminPage.Models;
    using AdminPage.Models.CustomizeModels;
    using System.Configuration;

    public class UserContent
    {
        public static bool isLandingPage = bool.Parse(ConfigurationManager.AppSettings["isLandingPage"] ?? "false");

        public enum Web_Feature
        {
            None,
            chung_dongdautrang_trai,
            chung_dongdautrang_phai,
            trangchu_slide,
            trangchu_tienich,
            trangchu_whyme,
            trangchu_tintuc,
            trangchu_sanpham,
            trangchu_gioithieu,
            trangchu_khachdanhgia,
            TopBackground,
            thankyou,
            contact,
            trangchu_dichvu,
            Thuvien_hinhanh,
            Tienich_chitiet,
            trangchu_lienhe,
        }

        public enum Posts_topic
        {
            GioiThieu = 1,
            TinTuc = 2,
        }

        public static Dictionary<string, string> OrderStatus()
        {
            var orderStatus = new Dictionary<string, string>();
            orderStatus.Add("-1", "Đã Hủy");
            orderStatus.Add("0", "Đơn hàng mới");
            orderStatus.Add("1", "Đang vận chuyển");
            orderStatus.Add("2", "Đã Thanh Toán");
            orderStatus.Add("3", "Đã Hoàn Tất");
            orderStatus.Add("4", "Đã trả hàng");
            orderStatus.Add("5", "Khác");
            return orderStatus;
        }

        public static Dictionary<string, string> ExpenseStatus()
        {
            var expenseStatus = new Dictionary<string, string>();
            expenseStatus.Add("0", "Phiếu tạm");
            expenseStatus.Add("1", "Đã thanh toán");
            return expenseStatus;
        }

        public enum UserRole
        {
            [EnumAttr("Admin", "Admin")] Admin = 100,
            [EnumAttr("Manager", "Manager")] Manager = 50,
            [EnumAttr("Member", "Member")] Member = 1,
        }
        public enum CustomerRole
        {
            [EnumAttr("Partner", "Partner")] Partner = 2,
            [EnumAttr("Member", "Member")] Member = 1,
        }

        public static string GetOgImage()
        {
            try
            {
                var d = new DirectoryInfo(HttpContext.Current.Server.MapPath(@"\Upload\images\og\")); // Assuming Test is your Folder
                var file = d.GetFiles().OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault(); // Getting Text files
                return "/Upload/images/og/" + file?.Name;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static string sessionTracker = "tracker";
        public static string GetTracker(bool reload = false)
        {
            if (HttpContext.Current.Session[sessionTracker] == null || reload == true)
            {
                var db = new AdminEntities();
                var tracker = db.viewpagetrackers.ToList();
                int total = tracker.Sum(x => x.ViewCount ?? 0);
                int today = tracker.Where(x => x.Date == DateTime.Today)?.FirstOrDefault()?.ViewCount ?? 0;

                HttpContext.Current.Session[sessionTracker] = total + "|" + today;
                return total + "|" + today;
            }
            else
            {
                return HttpContext.Current.Session[sessionTracker] as string;
            }
        }

        private static string sessionWebinfo = "webinfo";
        public static webconfiguration GetWebInfomation(bool reload = false)
        {
            if (HttpContext.Current.Session[sessionWebinfo] == null || reload == true)
            {
                var db = new AdminEntities();
                var webinfo = db.webconfigurations.FirstOrDefault() ?? new webconfiguration();
                webinfo.ZaloUrl = webinfo.ZaloUrl;
                webinfo.FacebookUrl = webinfo.FacebookUrl;
                webinfo.GoogleUrl = checkurl(webinfo?.GoogleUrl);
                webinfo.YoutubeUrl = checkurl(webinfo?.YoutubeUrl);
                HttpContext.Current.Session[sessionWebinfo] = webinfo;
                return webinfo;
            }
            else
            {
                return HttpContext.Current.Session[sessionWebinfo] as webconfiguration;
            }
        }

        private static string sessionCateprod = "webcateprod";
        public static List<category> GetWebCategoryProduct(bool reload = false)
        {
            if (HttpContext.Current.Session[sessionCateprod] == null || reload == true)
            {
                DBLangCustom db = new();
                var webcateprod = (from c in db.categories
                                   where c.Active == true && c.Sellable != false
                                   select c).OrderBy(x => x.Order).ToList();
                HttpContext.Current.Session[sessionCateprod] = webcateprod;
                return webcateprod;
            }
            else
            {
                return HttpContext.Current.Session[sessionCateprod] as List<category>;
            }
        }

        public static string ScrollEffect(string description)
        {
            return description.Replace("<img", "<img data-aos=\"fade-up-right\" ").Replace("<p", "<p data-aos=\"fade-up-left\" ").Replace("<h3", "<h3 data-aos=\"flip-left\" ").Replace("<ul", "<ul data-aos=\"flip-left\" ");
        }

        public static string checkurl(string url)
        {
            if (url != null && url.Contains("http://") == false && url.Contains("https://") == false)
            {
                url = "http://" + url;
            }

            return url;
        }

        public static SiteSEO.SEO_Meta GetUrlSite(string code)
        {
            var db = new AdminEntities();
            var web_config = db.webconfigurations.FirstOrDefault() ?? new webconfiguration();
            var list_SEO = string.IsNullOrEmpty(web_config.SEO_Meta) ? SiteSEO.New_SEO_Meta() : JsonConvert.DeserializeObject<List<SiteSEO.SEO_Meta>>(web_config.SEO_Meta);
            var SEO = list_SEO.FirstOrDefault(x => x.code == code);
            return SEO;
        }
    }
}