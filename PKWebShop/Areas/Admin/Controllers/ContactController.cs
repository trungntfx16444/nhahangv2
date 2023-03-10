namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Web.Mvc;
    using Newtonsoft.Json;
    using PKWebShop.Models;
    using PKWebShop.Models.CustomizeModels;
    using PKWebShop.Services;
    using PKWebShop.Utils;

    [Authorize]
    public class ContactController : UploadController
    {
        private WebShopEntities _db = new ();

        // GET: Admin/Contact
        public ActionResult Index(string lang)
        {
            using (var db = new DBLangCustom())
            {
                db.setLanguage(lang);
                var web_config = db.webconfigurations.FirstOrDefault() ?? new webconfiguration();
                var list_SEO = string.IsNullOrEmpty(web_config.SEO_Meta) ? SiteSEO.New_SEO_Meta() : JsonConvert.DeserializeObject<List<SiteSEO.SEO_Meta>>(web_config.SEO_Meta);
                ViewBag.ListSEO = list_SEO;
                return View(db.webconfigurations.FirstOrDefault() ?? new webconfiguration());
            }
        }

        [HttpPost]
        public ActionResult Update(webconfiguration info)
        {
            try
            {
                var webLicense = new PackageServices().WebPackInfo();
                UploadAttachFile("/upload/logo/", "LogoPicture", string.Empty, out string logo);
                UploadAttachFile("/upload/logo/", "LogoHeader", string.Empty, out string logoHeader);
                UploadAttachFile("/upload/logo/", "LogoFooter", string.Empty, out string logoFooter);
                UploadAttachFile("/upload/logo/", "ImageShared", string.Empty, out string imageShared);

                var wi = _db.webconfigurations.Find(info.Id);
                if (wi == null)
                {
                    // create
                    // wi.LogoPicture = logo;
                    Random rd = new ();
                    info.Id = AppFunc.NewShortId();
                    info.LogoPicture = logo;
                    info.LogoHeader = logoHeader;
                    info.LogoFooter = logoFooter;
                    info.ImageShared = imageShared;
                    _db.webconfigurations.Add(info);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(logo) == false)
                    {
                        wi.LogoPicture = logo;
                    }
                    if (string.IsNullOrWhiteSpace(logoHeader) == false)
                    {
                        wi.LogoHeader = logoHeader;
                    }
                    if (string.IsNullOrWhiteSpace(logoFooter) == false)
                    {
                        wi.LogoFooter = logoFooter;
                    }
                    if (string.IsNullOrWhiteSpace(imageShared) == false)
                    {
                        wi.ImageShared = imageShared;
                    }

                    wi.CompanyName = info.CompanyName;
                    wi.CompanyPhone = info.CompanyPhone;
                    wi.ShortAboutUs = info.ShortAboutUs;
                    wi.Address = info.Address;
                    wi.Hotline = info.Hotline;
                    wi.ContactEmail = info.ContactEmail;
                    wi.GoogleUrl = info.GoogleUrl;
                    wi.YoutubeUrl = info.YoutubeUrl;
                    wi.ZaloUrl = info.ZaloUrl;
                    wi.InstagramUrl = info.InstagramUrl;
                    wi.TwitterUrl = info.TwitterUrl;
                    wi.FacebookFanpage = info.FacebookFanpage;
                    wi.FacebookUrl = info.FacebookUrl;
                    wi.GoogleMap = Request.Unvalidated["gm"];
                    wi.GoogleAnalytics = Request.Unvalidated["ga"];
                    wi.GoogleOrdersStatistic = Request.Unvalidated["gos"];
                    wi.ChatToolEmbed = Request.Unvalidated["ct"];
                    wi.SEO_MetaDescription = info.SEO_MetaDescription;
                    wi.SEO_MetaKeyWord = info.SEO_MetaKeyWord;
                    wi.WorkTime = info.WorkTime;
                    wi.Copyright = info.Copyright;
                    wi.Instagram = info.Instagram;

                    if (webLicense.SocialLogin)
                    {
                        wi.GoogleClientId = info.GoogleClientId;
                        wi.FacebookAppId = info.FacebookAppId;
                        wi.FacebookClientToken = info.FacebookClientToken;
                        wi.FacebookAppVersion = info.FacebookAppVersion;
                    }
                    _db.Entry(wi).State = System.Data.Entity.EntityState.Modified;
                }

                _db.SaveChanges();
                TempData["success"] = "Cập nhật thành công";
                AppLB.UserContent.GetWebInfomation(true);
                info.LangCode = wi.LangCode;
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }

            return RedirectToAction("index", new { lang = info.LangCode });
        }

        public JsonResult Update_ajax(webconfiguration info)
        {
            try
            {
                UploadAttachFile("/upload/logo/", "LogoPicture", string.Empty, out string logo);
                UploadAttachFile("/upload/logo/", "LogoHeader", string.Empty, out string logoHeader);
                UploadAttachFile("/upload/logo/", "LogoFooter", string.Empty, out string logoFooter);
                UploadAttachFile("/upload/logo/", "ImageShared", string.Empty, out string imageShared);

                var wi = _db.webconfigurations.Find(info.Id);
                if (wi == null)
                {
                    // create
                    // wi.LogoPicture = logo;
                    Random rd = new ();
                    info.Id = AppFunc.NewShortId();
                    info.LogoPicture = logo;
                    info.LogoHeader = logoHeader;
                    info.LogoFooter = logoFooter;
                    info.ImageShared = imageShared;
                    _db.webconfigurations.Add(info);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(logo) == false)
                    {
                        wi.LogoPicture = logo;
                    }
                    if (string.IsNullOrWhiteSpace(logoHeader) == false)
                    {
                        wi.LogoHeader = logoHeader;
                    }
                    if (string.IsNullOrWhiteSpace(logoFooter) == false)
                    {
                        wi.LogoFooter = logoFooter;
                    }
                    if (string.IsNullOrWhiteSpace(imageShared) == false)
                    {
                        wi.ImageShared = imageShared;
                    }

                    wi.CompanyName = info.CompanyName;
                    wi.CompanyPhone = info.CompanyPhone;
                    wi.ShortAboutUs = info.ShortAboutUs;
                    wi.Address = info.Address;
                    wi.Hotline = info.Hotline;
                    wi.ContactEmail = info.ContactEmail;
                    wi.GoogleUrl = info.GoogleUrl;
                    wi.YoutubeUrl = info.YoutubeUrl;
                    wi.ZaloUrl = info.ZaloUrl;
                    wi.InstagramUrl = info.InstagramUrl;
                    wi.TwitterUrl = info.TwitterUrl;
                    wi.FacebookUrl = info.FacebookUrl;
                    wi.GoogleMap = Request.Unvalidated["gm"];
                    wi.GoogleAnalytics = Request.Unvalidated["ga"];
                    wi.GoogleOrdersStatistic = Request.Unvalidated["gos"];
                    wi.ChatToolEmbed = Request.Unvalidated["ct"];
                    wi.SEO_MetaDescription = info.SEO_MetaDescription;
                    wi.SEO_MetaKeyWord = info.SEO_MetaKeyWord;
                    wi.WorkTime = info.WorkTime;
                    wi.Copyright = info.Copyright;
                    wi.Instagram = info.Instagram;
                    _db.Entry(wi).State = System.Data.Entity.EntityState.Modified;
                }
                _db.SaveChanges();
                AppLB.UserContent.GetWebInfomation(true);
                return Json(new object[] { true, "Cập nhật thành công" });
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
                return Json(new object[] { true, e.Message, e.ToString() });
            }
        }

        public JsonResult DeleteLogo(string id)
        {
            var infoWeb = _db.webconfigurations.Find(id);
            if (infoWeb != null && string.IsNullOrWhiteSpace(infoWeb.LogoPicture) == false)
            {
                infoWeb.LogoPicture = null;
                _db.Entry(infoWeb).State = System.Data.Entity.EntityState.Modified;
                _db.SaveChanges();
                AppLB.UserContent.GetWebInfomation(true);
                try
                {
                    FileInfo f = new (Server.MapPath(infoWeb.LogoPicture));
                    if (f.Exists)
                    {
                        f.Delete();
                    }
                }
                catch (Exception)
                {
                }
            }

            return Json(true);
        }

        public JsonResult UpdateMetaSEO(SiteSEO.SEO_Meta dataSEO)
        {
            using (var db = new DBLangCustom())
            {
                try
                {
                    string langeCode = !string.IsNullOrEmpty(Request["langCode"]) ? Request["langCode"] : SiteLang.GetDefault().Code;
                    db.setLanguage(langeCode);
                    var web_config = db.webconfigurations.FirstOrDefault(x => x.LangCode == langeCode);
                    if (web_config == null)
                    {
                        throw new Exception("Lưu thất bại. Lỗi web_config == null");
                    }
                    var list_SEO = string.IsNullOrEmpty(web_config.SEO_Meta) ? SiteSEO.New_SEO_Meta() : JsonConvert.DeserializeObject<List<SiteSEO.SEO_Meta>>(web_config.SEO_Meta);
                    foreach (var item in list_SEO)
                    {
                        if (item.code == dataSEO.code)
                        {
                            item.meta_title = dataSEO.meta_title;
                            item.meta_keyword = dataSEO.meta_keyword;
                            item.meta_desc = dataSEO.meta_desc;
                            item.meta_extend = dataSEO.meta_extend;
                            item.url = (item.code == SiteSEO.code_SEO.SanPham || item.code == SiteSEO.code_SEO.TinTuc) ? dataSEO.url?.Replace("/", string.Empty) : string.Empty;
                        }
                    }

                    var list_SEO_json = JsonConvert.SerializeObject(list_SEO);
                    web_config.SEO_Meta = list_SEO_json;
                    db.Entry(web_config).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    #region UPDATE URL MENU
                    menu _menu = null;
                    if (dataSEO.code == SiteSEO.code_SEO.SanPham)
                    {
                        _menu = db.menus.FirstOrDefault(x => x.Id == "2"); // 2 => id menu san pham
                    }
                    else if (dataSEO.code == SiteSEO.code_SEO.TinTuc)
                    {
                        _menu = db.menus.FirstOrDefault(x => x.Id == "3"); // 2 => id menu tin tuc
                    }

                    if (_menu != null)
                    {
                        _menu.URL = dataSEO.url;
                        db.Entry(_menu).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                    #endregion

                    return Json(new object[] { true, "Lưu thành công!" });
                }
                catch (Exception ex)
                {
                    return Json(new object[] { false, ex.Message });
                }
            }
        }
    }
}