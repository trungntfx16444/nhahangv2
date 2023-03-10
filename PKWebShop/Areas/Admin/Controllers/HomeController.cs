namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Security;
    using Newtonsoft.Json;
    using PKWebShop.AppLB;
    using PKWebShop.Models;

    [Authorize]
    public class HomeController : ExpiredCheckController
    {
        private WebShopEntities db = new ();
        private user cmem = Authority.GetThisUser();

        // GET: Admin/Home
        public ActionResult Index()
        {

           
            int m = Request["month"] == null ? DateTime.Today.Month : int.Parse(Request["month"]);
            int y = Request["year"] == null ? DateTime.Today.Year : int.Parse(Request["year"]);
            TempData["m"] = m;
            TempData["y"] = y;
            int days = DateTime.DaysInMonth(y, m);
            var viewTracker = (from vt in db.viewpagetrackers
                               where vt.Date != null && vt.Date.Value.Month == m && vt.Date.Value.Year == y
                               orderby vt.Date
                               select new
                               {
                                   day = vt.Date.Value.Day,
                                   count = vt.ViewCount,
                               }).ToList();

            List<int> data = new ();

            for (DateTime i = new (y, m, 1); i <= DateTime.Now.Date && i <= new DateTime(y, m, days); i = i.AddDays(1))
            {
                var count = viewTracker.FirstOrDefault(v => v.day == i.Day)?.count ?? 0;
                data.Add(count);
            }
            ViewBag.days = days.ToString();
            ViewBag.data = JsonConvert.SerializeObject(data);
            return View();
        }

        public JsonResult GetBarChartData(string month, string year)
        {
            try
            {
                if (string.IsNullOrEmpty(month))
                {
                    month = DateTime.Today.Month.ToString();
                }

                if (string.IsNullOrEmpty(year))
                {
                    year = DateTime.Today.Year.ToString();
                }

                DateTime fDate = DateTime.Parse(month + "/01/" + year + " 00:00:00");
                DateTime tDate = fDate.AddMonths(1);

                List<object> chartData = new ();
                chartData.Add(new object[] { "Sản phẩm", "Số lượng đã bán" });

                var listChartData = (from od in db.orders_detail
                                     where od.CreateAt >= fDate && od.CreateAt <= tDate
                                     group od by od.ProductId into g
                                     select new BarChartData
                                     {
                                         ProductId = g.Key,
                                         ProductName = g.FirstOrDefault().ProductName,
                                         Quantity = g.Sum(x => x.Quantity),
                                     }).ToList();

                foreach (var item in listChartData.OrderByDescending(x => x.Quantity).Take(10))
                {
                    chartData.Add(new object[] { item.ProductName, item.Quantity });
                }

                return Json(new object[] { true, chartData });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        #region LOGIN - LOGOUT

        [AllowAnonymous]
        public ActionResult Login()
        {
            var session = System.Web.HttpContext.Current.Session;
            user user = session["user"] as user;

            if (user == null)
            {
                try
                {
                    if (System.Web.HttpContext.Current.Request.Cookies["admin_user"] != null)
                    {
                        FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(System.Web.HttpContext.Current.Request.Cookies["admin_user"]?.Value);
                        string[] info = ticket.UserData?.Split(new char[] { '|' });
                        string username = info[0];
                        string password = info[1];

                        var userAutoLogin = db.users.Where(u => u.UserName.Equals(username) && u.Password.Equals(password)).FirstOrDefault();
                        if (userAutoLogin != null)
                        {
                            session["user"] = userAutoLogin;
                            FormsAuthentication.SetAuthCookie(userAutoLogin.UserName, true);
                        }
                        else
                        {
                            return Logout();
                        }
                    }
                    else
                    {
                        return View();
                    }
                }
                catch (Exception)
                {
                    return View();
                }
            }

            return RedirectToAction("index");
        }

        [HttpPost]
        [AllowAnonymous]
        [ActionName("Login")]

        // [ValidateAntiForgeryToken]
        public ActionResult LoginSubmit()
        {
            try
            {
                string username = Request["Username"];
                string password = Request["Password"];
                bool rememberme = Request["RememberMe"] == null ? false : true;
                string returnurl = Request["ReturnUrl"];

                var user = db.users.Where(u => u.UserName.Equals(username) && u.Password.Equals(password)).FirstOrDefault();
                if (user != null)
                {
                    user.LastLogin = DateTime.Now;
                    db.Entry(user).State = System.Data.Entity.EntityState.Modified;

                    db.user_actions_log.Add(new user_actions_log
                    {
                        UserId = user.UserId,
                        UserName = user.UserName,
                        Action = "author",
                        Time = DateTime.Now,
                        Description = "Đăng nhập",
                    });

                    db.SaveChanges();

                    Session["user"] = user;

                    string dt = user.UserName + "|" + user.Password;
                    FormsAuthenticationTicket ticket = new (1, "tic_user", DateTime.Now, DateTime.Now.AddDays(7), true, dt);
                    string ticEncrypt = FormsAuthentication.Encrypt(ticket);
                    HttpCookie dquser = new ("admin_user")
                    {
                        Expires = DateTime.Now.AddDays(7),
                        Value = ticEncrypt,
                    };

                    Response.Cookies.Add(dquser);

                    FormsAuthentication.SetAuthCookie(user.UserName, true);

                    if (!string.IsNullOrWhiteSpace(returnurl) && Url.IsLocalUrl(returnurl))
                    {
                        return Redirect(returnurl);
                    }
                    else
                    {
                        return RedirectToAction("index");
                    }
                }

                throw new Exception("Login failure");
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
                return View();
            }
        }

        public ActionResult Logout()
        {
            var curMem = AppLB.Authority.GetThisUser(false);
            try
            {
                
                db.user_actions_log.Add(new user_actions_log
                {
                    UserId = curMem.UserId,
                    UserName = curMem.UserName,
                    Action = "author",
                    Time = DateTime.Now,
                    Description = "Đăng xuất",
                });
                db.SaveChanges();
            }
            catch
            {
            }
            HttpCookie cookie = Response.Cookies["admin_user"] as HttpCookie;
            cookie.Expires = DateTime.Today.AddDays(-1);
            cookie.Value = null;
            Response.Cookies.Add(cookie);
            FormsAuthentication.SignOut();
            Session.RemoveAll();

            return RedirectToAction("Login");
        }

        #endregion

        #region JQUERY LOAD FULL ADDRESS

        /// <summary>
        /// load tinh/tp.
        /// </summary>
        /// <returns>json.</returns>
        [AllowAnonymous]
        public JsonResult LoadProvince()
        {
            try
            {
                var listProvince = db.vn_province.OrderByDescending(x => x.type).ThenBy(x => x.name).ToList();
                return Json(new object[] { true, listProvince }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        /// <summary>
        /// load quan/huyen theo tinh/tp.
        /// </summary>
        /// <param name="province">ProvinceId|ProvinceName.</param>
        /// <returns>json.</returns>
        [AllowAnonymous]
        public JsonResult LoadDistrict(string province)
        {
            try
            {
                var listDistrict = db.vn_district.Where(x => x.provinceid == province).OrderByDescending(x => x.type).ThenBy(x => x.name).ToList();
                return Json(new object[] { true, listDistrict }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        /// <summary>
        /// load xã/phường theo quan/huyen.
        /// </summary>
        /// <param name="district">ProvinceId|ProvinceName.</param>
        /// <returns>json.</returns>
        [AllowAnonymous]
        public JsonResult LoadWard(string district)
        {
            try
            {
                var listDistrict = db.vn_ward.Where(x => x.districtid == district).OrderBy(x => new { x.type, x.name }).ToList();
                return Json(new object[] { true, listDistrict }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }
        #endregion

        public ActionResult TestEmail(string lang)
        {
            using (var db = new DBLangCustom())
            {
                db.setLanguage(lang);
                return View(db.webconfigurations.FirstOrDefault() ?? new webconfiguration());
            }
        }

        [ValidateInput(false)]
        public JsonResult TestEmail_Send(string Email, string Subject, string Content)
        {
            var result = SendEmail.Send(Email, Subject, Content, string.Empty, string.Empty, null);
            if (result == string.Empty)
            {
                return Json(new object[] { true, "Gửi email thành công, hãy kiểm tra hộp thư của bạn!" });
            }
            else
            {
                return Json(new object[] { false, $"Gửi email thất bại, {result}!" });
            }
        }

        public JsonResult EmailConfigUpdate(webconfiguration info)
        {
            try
            {
                var db = new WebShopEntities();
                var wi = db.webconfigurations.Find(info.Id);
                if (wi == null)
                {
                    throw new Exception("Không tìm thấy cấu hình web");
                }
                else
                {
                    wi.MailServer = info.MailServer;
                    wi.AuthEmail = info.AuthEmail;
                    wi.AuthPassword = info.AuthPassword;
                    wi.Port = info.Port;
                    wi.SSL = info.SSL;

                    db.Entry(wi).State = System.Data.Entity.EntityState.Modified;
                }

                db.SaveChanges();
                UserContent.GetWebInfomation(true);
                return Json(new object[] { true, "Đã cập nhật cấu hình thành công!" });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, $"Lưu cấu hình thất bại, {e.Message}!" });
            }
        }

        public ActionResult SyncLanguages()
        {
            var curMem = Authority.GetThisUser(false);
            if (curMem.RoleLevel != (int)UserContent.UserRole.Admin)
            {
                TempData["error"] = "Không có quyền truy cập";
                return RedirectToAction("index");
            }

            var result = Services.SyncLanguages.Product();
            if (string.IsNullOrEmpty(result))
            {
                TempData["success"] = "Đồng bộ ngôn ngữ thành công";
            }
            else
            {
                TempData["error"] = $"Đồng bộ ngôn ngữ gặp sự cố. Lỗi: {result}";
            }
            return RedirectToAction("index");
        }
    }

    public class BarChartData
    {
        public string ProductId { get; set; }

        public string ProductName { get; set; }

        public int? Quantity { get; set; }
    }
}