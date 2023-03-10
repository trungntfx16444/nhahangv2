namespace PKWebShop.AppLB
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Resources;
    using System.Web;
    using System.Web.Routing;
    using System.Web.Security;
    using Inner.Libs.Helpful;
    using Newtonsoft.Json;
    using PKWebShop.Models;
    using PKWebShop.Models.CustomizeModels;

    public class UserContent
    {
        public enum Web_Feature
        {
            None,
            chung_dongdautrang_trai,
            chung_dongdautrang_phai,
            trangchu_slide,
            trangchu_slide_sanpham,
            trangchu_doitac,
            trangchu_tienich,
            trangchu_whyme,
            trangchu_tintuc,
            trangchu_duan,
            trangchu_sanpham,
            trangchu_sanpham2,
            trangchu_hinhanh,
            trangchu_gioithieu,
            trangchu_khachdanhgia,
            TopBackground,
            thankyou,
            contact,
            trangchu_dichvu,
            Thuvien_hinhanh,
            Tienich_chitiet,
            trangchu_lienhe,
            dathang_ghichu,
        }

        public enum Posts_topic
        {
            GioiThieu = 1,
            TinTuc = 2,
        }


        // khởi tạo một dict trạng thái order
        public static Dictionary<string, string> OrderStatus()
        {
            Dictionary<string, string> orderStatus = new();
            orderStatus.Add("-1", "Đã Hủy");
            orderStatus.Add("0", "Đơn hàng mới");
            orderStatus.Add("1", "Đang vận chuyển");
            orderStatus.Add("2", "Đã Thanh Toán");
            orderStatus.Add("3", "Đã Hoàn Tất");
            orderStatus.Add("4", "Đã trả hàng");
            orderStatus.Add("5", "Khác");
            return orderStatus;
        }

        // khởi tạo một dict trạng thái thanh toán
        public static Dictionary<string, string> ExpenseStatus()
        {
            Dictionary<string, string> expenseStatus = new();
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
            [EnumAttr("Partner", "Partner")] Manager = 2,
            [EnumAttr("Member", "Member")] Member = 1,
        }

        // tên session
        private static string sessionWebinfo = "webinfo";
       
        // lấy thông tin trang web 
        public static webconfiguration GetWebInfomation(bool reload = false)
        {
            // kiểm tra session[sessionWebinfo] của  đối tượng HttpContext hiện tái có bằng null hoặc reload 
            if (HttpContext.Current?.Session[sessionWebinfo] == null || reload == true)
            { 
                // lấy dữ liệu webconfig từ db
                DBLangCustom db = new();
                // lấy trường dữ liệu webcoifg đầu tiên
                var webinfo = db.webconfigurations.FirstOrDefault() ?? new webconfiguration();
                // lấy ra các thông tin khác
                webinfo.ZaloUrl = webinfo.ZaloUrl;
                webinfo.FacebookUrl = webinfo.FacebookUrl;
                webinfo.GoogleUrl = checkurl(webinfo?.GoogleUrl);
                webinfo.YoutubeUrl = checkurl(webinfo?.YoutubeUrl);

                // nếu đối tượng HttpContext tồn tại thì gán session[sesionInfo] = webinfoi lấy từ db;
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Session[sessionWebinfo] = webinfo;
                }
                return webinfo;
            }
            else
            {
                // nếu session[sesionWebinfo] tồn tại thì trả về giá trị tạm thời được lưu trữ trong phiên làm việc với từ khóa sessionWebinfo
                return HttpContext.Current.Session[sessionWebinfo] as webconfiguration;
            }
        }

        private static string sessionIntro = "webintro";

        // lấy danh sách bài viết giới thiệu
        public static List<n_news> GetWebIntroFromNews(bool reload = false, string cateId = "1")
        {
            // nếu session[sessionIntro] của đối tương HttpContext =null hoặc có yêu cầu reload lại trang
            if (HttpContext.Current.Session[sessionIntro] == null || reload == true)
            {
                WebShopEntities db = new();
                var webintro = (from n in db.n_news
                                where n.Active != false
                                && n.ParentTopicId == cateId
                                select n).ToList();
                // gán dữ liệu web intro vào sesion[sesionIntro]
                HttpContext.Current.Session[sessionIntro] = webintro;
                //trả về danh sách các bài viết giới thiệu
                return webintro;
            }
            else
            {
                // trả về danh sách list bài viết giới thiệu với để tránh lỗi thì ép kiểu về dạng list<bai_viet>
                return HttpContext.Current.Session[sessionIntro] as List<n_news>;
            }
        }
        
        private static string sessionTopic = "webtopic";

        // lấy danh sách các toppic (update)
        public static List<n_toppic> GetWebTopic(bool reload = false)
        {
            if (HttpContext.Current.Session[sessionTopic] == null || reload == true)
            {
                WebShopEntities db = new();
                var webtopic = (from t in db.n_toppic
                                where t.Name != "Giới thiệu công ty"
                                select t).ToList();
                HttpContext.Current.Session[sessionTopic] = webtopic;
                return webtopic;
            }
            else
            {
                return HttpContext.Current.Session[sessionTopic] as List<n_toppic>;
            }
        }

        private static string sessionService = "webservice";
        
        // lấy danh sách các dịch vụ (update)
        public static List<service> GetWebService(bool reload = false)
        {
            if (HttpContext.Current.Session[sessionService] == null || reload == true)
            {
                WebShopEntities db = new();
                var webservice = (from s in db.services
                                  orderby s.Order
                                  select s).ToList();
                HttpContext.Current.Session[sessionService] = webservice;
                return webservice;
            }
            else
            {
                return HttpContext.Current.Session[sessionService] as List<service>;
            }
        }

        private static string sessionCateprod = "webcateprod";

        // lay danh sách các sản phẩm
        public static List<category> GetWebCategoryProduct(bool reload = false)
        {
            if (HttpContext.Current.Session[sessionCateprod] == null || reload == true)
            {
                DBLangCustom db = new();
                // lấy danh sách các sản phẩm đang active và đang sellable
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
        
        //tiện ích
        private static string sessionUtilities = "SessionUtilities";

        // lấy môt danh sách tiện ích kieu tupble(section, pic)
        public static List<(sectionfeaturedetail fdetail, string pic)> GetUtilities(bool reload = false)
        {
            if (HttpContext.Current.Session[sessionUtilities] == null || reload == true)
            {
                WebShopEntities db = new();
                // lấy chuỗi trang chủ tiện ích từ danh sách enum
                string s_tienich = UserContent.Web_Feature.trangchu_tienich.ToString();

                // lấy ra tiện ích với ReId = s_tienich
                var tienich = db.sectionfeatures.FirstOrDefault(s => s.ReId == s_tienich && s.Status == true);

                /*
                 * lấy ra danh sách tiện ích từ bảng sectionfeaturedetails dựa trên sectonCode = trangchu_tienich 
                   sắp xếp dựa trên trường volumnnumber 
                    tạo một danh sách mới gồm các phần tử (tên tiện ích, tên file) trong đó hình ảnh được lấy ra từ tableid = featerID và tablename =sectioinfeateuredetials
               */ 
                var tienich_list = db.sectionfeaturedetails.Where(f => f.SectionCode == tienich.ReId).OrderBy(f => f.VolumeNumber)
                    .Select(f => new { f, pic = db.uploadmorefiles.FirstOrDefault(p => p.TableId == f.Id && p.TableName == "sectionfeaturedetails") }).ToList().Select(i => (fdetail: i.f, pic: i.pic?.FileName)).Take(3).ToList();
                HttpContext.Current.Session[sessionUtilities] = tienich_list;
                return tienich_list;
            }
            else
            {
                return HttpContext.Current.Session[sessionUtilities] as List<(sectionfeaturedetail fdetail, string pic)>;
            }
        }

        // lấy danh danh sach menu
        private static string sessionMenu = "menus";
        public static IEnumerable<MenuView> GetWebMenu(bool reload = false)
        {
            if (HttpContext.Current.Session[sessionMenu] == null || reload == true)
            {
                DBLangCustom db = new();
                // lấy ra danh sách menu
                var _menu = db.menus.ToList();

                /*
                    * tạo danh sách menu theo dạng cây
                    1 - lọc ra các menu cha ( là các menu không có menu cha vơi PrentMenuId = null)
                    2 - nối các menu con với menu cha tương ứng
                 */
                var menus = _menu.Where(m => string.IsNullOrEmpty(m.ParentMenuId)).GroupJoin(_menu, m1 => m1.Id, m2 => m2.ParentMenuId, (m1, m2) =>
                       new MenuView
                       {
                           menu = m1,
                           child = m2?.GroupJoin(_menu, m2 => m2.Id, m3 => m3.ParentMenuId, (m2, m3) =>
                           new MenuView
                           {
                               menu = m2,
                               child = m3?.Select(m =>
                               new MenuView
                               {
                                   menu = m,
                                   child = null,
                               }).ToList(),
                           }).ToList(),
                       }).ToList();
                return menus;
            }
            else
            {
                return HttpContext.Current.Session[sessionMenu] as List<MenuView>;
            }
        }

        private static string sessionDuAnLapDat = "duan";
        public static List<n_news> GetDuAnLapDat(bool reload = false)
        {
            if (HttpContext.Current.Session[sessionDuAnLapDat] == null || reload == true)
            {
                DBLangCustom db = new();
                var duAn = db.n_news.Where(x => x.ParentTopicId == "5" && x.Active == true).OrderByDescending(x => x.Order).ThenByDescending(x => x.CreatedAt).Take(4).ToList();
                HttpContext.Current.Session[sessionDuAnLapDat] = duAn;
                return duAn;
            }
            else
            {
                return HttpContext.Current.Session[sessionDuAnLapDat] as List<n_news>;
            }
        }

        public static string GetOgImage()
        {
            try
            {
                DirectoryInfo d = new(HttpContext.Current.Server.MapPath(@"\Upload\images\og\")); // Assuming Test is your Folder
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
                WebShopEntities db = new();
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

        public static customer getCurrentCustomer()
        {
            try
            {
                customer? cur = (customer?)HttpContext.Current.Session["Customer"];
                if (cur != null)
                {
                    cur.BonusPoints = new WebShopEntities().customers.Find(cur.Id)?.BonusPoints ?? 0;
                    return cur;
                }

                var ck = HttpContext.Current.Request.Cookies.Get("Customer");
                if (ck != null)
                {
                    var auth = AppLB.SecurityLibrary.Decrypt(ck.Value)?.Split('|');
                    string phone = auth.ElementAtOrDefault(0) ?? string.Empty, email = auth.ElementAtOrDefault(1) ?? string.Empty, pass = auth.ElementAtOrDefault(2) ?? string.Empty;
                    if (string.IsNullOrEmpty(pass) && !string.IsNullOrWhiteSpace(email))
                    {
                        cur = new WebShopEntities().customers.FirstOrDefault(c => c.Email == email && (c.AccountType == "Facebook" || c.AccountType == "Google"));
                    }
                    else
                    {
                        cur = new WebShopEntities().customers.FirstOrDefault(c => c.Email == email && c.Password == pass);
                    }

                    if (cur != null)
                    {
                        HttpContext.Current.Session["Customer"] = cur;
                        return cur;
                    }
                }
            }
            catch (Exception e)
            {
            }

            HttpContext.Current.Request.Cookies.Remove("Customer");
            return null;
        }
        
        // lấy đối tượng seo
        public static SiteSEO.SEO_Meta GetUrlSite(string code)
        {
            WebShopEntities db = new WebShopEntities();
            var web_config = db.webconfigurations.FirstOrDefault() ?? new webconfiguration();
            var list_SEO = string.IsNullOrEmpty(web_config.SEO_Meta) ? SiteSEO.New_SEO_Meta() : JsonConvert.DeserializeObject<List<SiteSEO.SEO_Meta>>(web_config.SEO_Meta);
            var SEO = list_SEO.FirstOrDefault(x => x.code == code);
            return SEO;
        }

        public static string GetAdminSite()
        {
            return ConfigurationManager.AppSettings["admin_site"] ?? string.Empty;
        }
        public static int CartProductCount()
        {
            var id = getCurrentCustomer()?.Id;
            if (id == null)
            {
                HttpCookie cookie = HttpContext.Current.Request.Cookies["g_cus_id"] ?? new HttpCookie("g_cus_id");
                cookie.Value ??= Guid.NewGuid().ToString();
                cookie.Expires = DateTime.UtcNow.AddDays(30);
                HttpContext.Current.Request.Cookies.Add(cookie);
                id = cookie.Value;
            }
            return new WebShopEntities().cart_details.Count(c => c.CustomerId == id && c.Type == "Product");
        }

        
        public static sFeatures_view getFeature(Web_Feature f)
        {
            return new DataAsset.DA_SectionFeatures().getViewFeature(f);
        }
        internal static List<string> GetAllChildCategory(string cateId)
        {
            using (var db = new WebShopEntities())
            {
                var cateIds = new List<string> { cateId };
                var listCateId = cateIds;
                while (cateIds.Count > 0)
                {
                    var child_cate = db.categories.Where(c => cateIds.Contains(c.ParentId)).Select(c => c.ReId).ToList();
                    listCateId.AddRange(child_cate);
                    cateIds = child_cate;
                }
                return db.categories.Where(c => listCateId.Contains(c.ReId)).Select(c => c.ReId).ToList();
            }

        }
    }
}