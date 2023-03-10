using AdminPage.Utils;

namespace AdminPage.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using AdminPage.AppLB;
    using AdminPage.Models;
    using AdminPage.Models.CustomizeModels;
    using AdminPage.Services;

    [Authorize]
    public class ServicesController : UploadController
    {
        private AdminEntities _db = new ();
        private SiteLang.Language defaultLanguage = SiteLang.GetDefault();
        private user cMem = Authority.GetThisUser();

        // GET: Admin/Services
        public ActionResult Index()
        {
            var sv = (from n in _db.services.AsEnumerable()
                      orderby SiteLang.getOrder(n.LangCode)
                      group n by n.ReId into g_n
                      select new Group_Service { service = g_n.FirstOrDefault(), langs = g_n.Select(g => g.LangCode).ToList() }).ToList();
            return View(sv);
        }

        // public ActionResult Save(string id)
        // {
        //    if (!string.IsNullOrEmpty(id))
        //    {
        //        return View(_db.services.Find(id));
        //    }
        //    else
        //    {
        //        return View(new service());
        //    }
        // }
        public ActionResult Save(string id, string lang)
        {
            using (var db = new DBLangCustom())
            {
                db.setLanguage(lang);
                ViewBag.langCode = db.LangCode;
                var rs = _db.services.Where(n => n.ReId == id).Select(n => n.LangCode ?? defaultLanguage.Code).ToList() ?? new List<string>();
                var exL = SiteLang.GetListLangs().Where(s => rs.Contains(s.Code) || s.Code == lang).ToList();
                ViewBag.exitsLangs = exL;
                var news = db.services.FirstOrDefault(n => n.ReId == id);
                ViewBag.gallery = get_tablefile(id, "services").ToList();
                if (news != null)
                {
                    return View(news);
                }
                else
                {
                    return View(new service { ReId = id, LangCode = lang });
                }
            }
        }

        public ActionResult _Save(string id, string lang)
        {
            using (var db = new DBLangCustom())
            {
                db.setLanguage(lang);
                ViewBag.langCode = db.LangCode;
                var rs = _db.services.Where(n => n.ReId == id).Select(n => n.LangCode ?? defaultLanguage.Code).ToList() ?? new List<string>();
                var exL = SiteLang.GetListLangs().Where(s => rs.Contains(s.Code) || s.Code == lang).ToList();
                ViewBag.exitsLangs = exL;
                var news = db.services.FirstOrDefault(n => n.ReId == id);
                ViewBag.gallery = get_tablefile(id, "service").ToList();
                if (news != null)
                {
                    return PartialView("_save", news);
                }
                else
                {
                    return PartialView("_save", new n_news { Active = true });
                }
            }
        }

        [HttpPost]
        [ActionName("Save")]
        public ActionResult SaveSubmit(service sm)
        {
            try
            {
                sm.Decription = Request.Unvalidated["description"];

                if (string.IsNullOrEmpty(sm.LangCode))
                {
                    sm.LangCode = defaultLanguage.Code;
                }
                if (string.IsNullOrEmpty(sm.ReId))
                {
                    sm.ReId = sm.ServiceId;
                }
                var result = Services.services.SaveService(sm);
                int filesCount = int.Parse(Request["filescount"]);
                UploadMoreFiles(result.ReId, "services", filesCount, "/upload/service");

                return RedirectToAction("detail", new { id = result.ReId, lang = sm.LangCode });
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
                return RedirectToAction("save", new { id = sm.ReId, lang = sm.LangCode });
            }
        }

        public JsonResult SaveJson(service sm)
        {
            try
            {
                sm.Decription = Request.Unvalidated["description"];

                if (string.IsNullOrEmpty(sm.LangCode))
                {
                    sm.LangCode = defaultLanguage.Code;
                }
                if (string.IsNullOrEmpty(sm.ReId))
                {
                    sm.ReId = sm.ServiceId;
                }

                var rs = Services.services.SaveService(sm);
                int filesCount = int.Parse(Request["filescount"]);
                UploadMoreFiles(rs.ReId, "services", filesCount, "/upload/service");
                return Json(new object[] { true, "Đã lưu bài viết." });
            }
            catch (Exception e)
            {
                return Json(new object[] { true, e.Message, e.ToString() });
            }
        }

        public ActionResult Delete(string id)
        {
            try
            {
                var service = _db.services.Find(id);
                if (service != null)
                {
                    _db.services.Remove(service);
                    _db.SaveChanges();

                    // var picUrl = service.Picture;
                    // FileInfo f = new FileInfo(Server.MapPath(picUrl));
                    // if (f.Exists)
                    // {
                    //    f.Delete();
                    // }
                    TempData["success"] = "Xóa thành công";

                    if (_db.services.Any(n => n.ReId == service.ReId))
                    {
                        return RedirectToAction("detail", new { id = service.ReId });
                    }
                }
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }

            return RedirectToAction("index");
        }

        // public ActionResult Detail(string id)
        // {
        //    var sv = _db.services.Find(id);
        //    if (sv != null)
        //    {
        //        return View(sv);
        //    }
        //    else
        //    {
        //        TempData["error"] = "Không tìm thấy dịch vụ này";
        //        return RedirectToAction("index");
        //    }

        // }
        public ActionResult Detail(string id, string lang)
        {
            using (var db = new DBLangCustom())
            {
                db.setLanguage(lang);
                var rs = _db.services.Where(n => n.ReId == id).Select(n => n.LangCode ?? defaultLanguage.Code).ToList() ?? new List<string>();
                var exL = SiteLang.GetListLangs().Where(s => rs.Contains(s.Code)).ToList();
                ViewBag.exitsLangs = exL;
                var d = db.services.FirstOrDefault(n => n.ReId == id);
                if (d == null)
                {
                    d = _db.services.FirstOrDefault(n => n.ReId == id);
                }
                if (d == null)
                {
                    d = _db.services.Find(id);
                }
                if (string.IsNullOrEmpty(d?.LangCode))
                {
                    d.LangCode = defaultLanguage.Code;
                }

                return View(d);
            }
        }

        public ActionResult _AddLanguage_Modal(string id)
        {
            try
            {
                var rs = _db.services.Where(n => n.ReId == id).Select(n => n.LangCode ?? defaultLanguage.Code).ToList() ?? new List<string>();
                var exL = SiteLang.GetListLangs().Where(s => rs.Contains(s.Code)).ToList();
                var newL = SiteLang.GetListLangs().Where(s => !rs.Contains(s.Code)).ToList();
                ViewBag.Id = id;
                ViewBag.action = "/services/NewlanguageService";
                return PartialView("_AddLanguage_Modal", (newL, exL));
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message, e.ToString() });
            }
        }

        public ActionResult NewlanguageService(string Id, string From, string To)
        {
            try
            {
                var service = _db.services.Where(n => n.ReId == Id && (n.LangCode ?? defaultLanguage.Code) == From).FirstOrDefault();
                var copy = new service();
                if (!SiteLang.IsLanguageAvailable(To))
                {
                    throw new Exception("Không tìm thấy ngôn ngữ!");
                }
                if (_db.services.Any(n => n.ReId == Id && (n.LangCode ?? defaultLanguage.Code) == To))
                {
                    throw new Exception("Bài viết này đã tồn tại ngôn ngữ này!");
                }
                if (service != null)
                {
                    copy = new service
                    {
                        Decription = service.Decription,
                        LangCode = To,
                        Name = service.Name,
                        ServiceId = AppFunc.NewShortId(),
                        Order = service.Order,
                        Picture = service.Picture,
                        ReId = service.ReId,
                        CreateAt = DateTime.UtcNow,
                        CreateBy = cMem.Fullname,
                        DiscountAmount = service.DiscountAmount,
                        DiscountPercent = service.DiscountPercent,
                        Price = service.Price,
                        Quotes = service.Quotes,
                    };

                    _db.services.Add(copy);
                    _db.SaveChanges();
                }
                return Redirect($"/services/save/{Id}?lang={To}");
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
                return Redirect($"/services/save/{Id}?lang={From}");
            }
        }
    }
}