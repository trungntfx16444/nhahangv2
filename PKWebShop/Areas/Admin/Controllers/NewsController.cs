using PKWebShop.Utils;

namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Net;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Newtonsoft.Json;
    using PKWebShop.AppLB;
    using PKWebShop.Areas.Admin.CustomizeModel;
    using PKWebShop.Models;
    using PKWebShop.Models.CustomizeModels;
    using PKWebShop.Services;

    [Authorize]
    public class NewsController : UploadController
    {
        private WebShopEntities _db = new();
        private Dictionary<string, bool> access = Authority.GetAccessAuthority();
        private user cMem = Authority.GetThisUser();
        private SiteLang.Language defaultLanguage = SiteLang.GetDefault();

        public ActionResult Index()
        {
            var rs = (from p1 in _db.n_parent_topic.OrderBy(p => p.Order)
                      group p1 by p1.ReId into gp
                      select gp).ToList().Select(g => g.OrderBy(x => SiteLang.getOrder(x.LangCode)).FirstOrDefault()).ToList();
            ViewBag.Topics = _db.n_toppic.ToList();

            return View(rs);
        }

        public JsonResult Load_table(DataTable_request data, string search, string parentTopicId, string topicId, string language)
        {
            // Session["parentId"] = parentTopicId = !string.IsNullOrEmpty(parentTopicId) ? parentTopicId : (Session["parentId"]?.ToString() ?? "");
            // db.setLanguage(language);
            var recordsTotal = _db.n_news.Where(x => x.LangCode == (language ?? "vi")).Count();
            var news = _db.n_news.SqlQuery(CommonFunc.SearchCommand("n_news", search, "Name")).AsQueryable();
            news = news.Where(n => n.Active != false);
            if (parentTopicId != "all")
            {
                if (parentTopicId == "-1")
                {
                    news = news.Where(n => string.IsNullOrEmpty(n.ParentTopicId));
                }
                else
                {
                    Response.Cookies.Remove("newspage_parentId");
                    Response.Cookies["newspage_parentId"].Value = parentTopicId;
                    Response.Cookies["newspage_parentId"].Expires = DateTime.UtcNow.AddDays(14);
                    Response.Cookies["newspage_parentId"].Path = "/";
                    news = news.Where(n => n.ParentTopicId == parentTopicId);
                }
            }

            if (!string.IsNullOrEmpty(topicId))
            {
                news = news.Where(n => !string.IsNullOrEmpty(n.TopicId) && n.TopicId.Contains("\"" + topicId + "\""));
            }
            var listlang = SiteLang.GetListLangs().Select((x, i) => new { x.Code, i }).ToDictionary(x => x.Code, x => x.i);
            var l_news = news.OrderBy(n => listlang[n.LangCode]).ThenByDescending(n => n.CreatedAt);
            ViewBag.Toppic = _db.n_toppic.OrderBy(x => x.Name).ToList();
            var filtered_count = news.Count();
            string[] orderColumns = { "Name", null, "Order", "Active", "CreatedAt", null };
            var orderColumn = orderColumns[data.order?.FirstOrDefault()?.column ?? 1] ?? "CreatedAt";

            var group_news = (from n in l_news
                              group n by n.ReId into g_n
                              select new Group_News { news = g_n.FirstOrDefault(), langs = g_n.Select(g => g.LangCode).ToList() }).AsQueryable();
            var order = orderColumn + " " + data.order?.FirstOrDefault()?.dir;
            var list_news = group_news.OrderBy("news." + order).Skip(data.start).Take(data.length).ToList();
            var html = list_news.Count > 0 ? CommonFunc.RenderRazorViewToString("_tableData", list_news, this) : string.Empty;

            return Json(new { draw = data.draw, recordsFiltered = filtered_count, recordsTotal = recordsTotal, data = html });
        }

        public ActionResult Save(string id, string lang)
        {
            var parentId = Request.Cookies["newspage_parentId"]?.Value;
            lang = string.IsNullOrEmpty(lang) ? defaultLanguage.Code : lang;
            var ptopic = _db.n_parent_topic.Find(parentId);
            if (ptopic == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.ptopic = ptopic;
            ViewBag.Tags = _db.n_news_tags.OrderBy(t => t.tag_name).ToList();
            var list_topics = _db.n_toppic.Where(t => t.ParentTopicId == ptopic.ReId && (lang == null || t.LangCode == lang)).OrderBy(x => x.Order).ToList();
            ViewBag.list_topics = list_topics;
            ViewBag.langCode = lang;
            var rs = _db.n_news.Where(n => n.ReId == id).Select(n => n.LangCode ?? defaultLanguage.Code).ToList() ?? new List<string>();
            var exL = SiteLang.GetListLangs().Where(s => rs.Contains(s.Code) || s.Code == lang).ToList();
            ViewBag.exitsLangs = exL;
            var news = _db.n_news.FirstOrDefault(n => n.ReId == id && n.LangCode == lang);

            ViewBag.tags_auto = JsonConvert.SerializeObject(_db.n_news_tags.Select(t => t.tag_name).ToList());
            if (news != null)
            {
                ViewBag.uploadFiles = _db.uploadmorefiles.Where(f => f.TableId == news.ReId && f.TableName.Equals("n_news")).ToList();
                return View(news);
            }
            else
            {
                return View(new n_news { Active = true, ReId = id, LangCode = lang });
            }
        }

        public ActionResult _Save(string id, string LangCode)
        {
            var parentId = Request.Cookies["newspage_parentId"].Value;
            var ptopic = _db.n_parent_topic.Find(parentId);

            ViewBag.ptopic = ptopic;
            ViewBag.Tags = _db.n_news_tags.OrderBy(t => t.tag_name).ToList();
            var list_topics = _db.n_toppic.Where(t => t.ParentTopicId == ptopic.ReId && t.LangCode == ptopic.LangCode).OrderBy(x => x.Order).ToList();
            ViewBag.list_topics = list_topics;
            ViewBag.langCode = LangCode;
            var rs = _db.n_news.Where(n => n.ReId == id).Select(n => n.LangCode ?? defaultLanguage.Code).ToList() ?? new List<string>();
            var exL = SiteLang.GetListLangs().Where(s => rs.Contains(s.Code) || s.Code == LangCode).ToList();
            ViewBag.exitsLangs = exL;
            var news = _db.n_news.FirstOrDefault(n => n.ReId == id && n.LangCode == LangCode);

            ViewBag.tags_auto = JsonConvert.SerializeObject(_db.n_news_tags.Select(t => t.tag_name).ToList());
            if (news != null)
            {
                ViewBag.uploadFiles = _db.uploadmorefiles.Where(f => f.TableId == news.ReId && f.TableName.Equals("n_news")).ToList();
                return PartialView("_save", news);
            }
            else
            {
                return PartialView("_save", new n_news { Active = true });
            }
        }

        public ActionResult _NewsAddLang_Modal(string id)
        {
            try
            {
                var rs = _db.n_news.Where(n => n.ReId == id).Select(n => n.LangCode ?? defaultLanguage.Code).ToList() ?? new List<string>();
                var exL = SiteLang.GetListLangs().Where(s => rs.Contains(s.Code)).ToList();
                var newL = SiteLang.GetListLangs().Where(s => !rs.Contains(s.Code)).ToList();
                ViewBag.Id = id;
                return PartialView("_NewsAddLang_Modal", (newL, exL));
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message, e.ToString() });
            }
        }

        public ActionResult CopyNewsLang(string Id, string From, string To)
        {
            try
            {
                var news = _db.n_news.Where(n => n.ReId == Id && (n.LangCode ?? defaultLanguage.Code) == From).FirstOrDefault();
                var copy = new n_news();
                if (!SiteLang.IsLanguageAvailable(To))
                {
                    throw new Exception("Không tìm thấy ngôn ngữ!");
                }
                if (_db.n_news.Any(n => n.ReId == Id && (n.LangCode ?? defaultLanguage.Code) == To))
                {
                    throw new Exception("Bài viết này đã tồn tại ngôn ngữ này!");
                }
                if (news != null)
                {
                    copy = new n_news
                    {
                        Active = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = cMem.Fullname,
                        Decription = news.Decription,
                        LangCode = To,
                        Name = news.Name,
                        NewsId = AppFunc.NewShortId(),
                        Order = news.Order,

                        Picture = news.Picture,
                        ReId = news.ReId,
                        SEODescription = news.SEODescription,
                        ShortDescription = news.ShortDescription,
                        Tags = news.Tags,
                        TitleWebsite = news.TitleWebsite,
                        UrlCode = news.UrlCode,
                        ViewCount = news.ViewCount,
                    };
                    if (_db.n_parent_topic.Any(p => p.ReId == news.ParentTopicId))
                    {
                        var pTopic = _db.n_parent_topic.Where(p => p.ReId == news.ParentTopicId && (p.LangCode ?? defaultLanguage.Code) == To).FirstOrDefault();
                        if (pTopic == null)
                        {
                            pTopic = CopyPTopicLang(news.ParentTopicId, From, To);
                        }
                        copy.ParentTopicId = pTopic.ReId;
                        copy.ParentTopicName = pTopic.Name;
                    }
                    _db.n_news.Add(copy);
                    _db.SaveChanges();
                }
                return Redirect($"/admin/news/save/{Id}?LangCode={To}");
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
                return Redirect($"/admin/news/save/{Id}?LangCode={From}");
            }
        }

        private n_parent_topic CopyPTopicLang(string parentTopicId, string from, string to)
        {
            var pTopic = _db.n_parent_topic.Where(p => p.ReId == parentTopicId && (p.LangCode ?? defaultLanguage.Code) == from).FirstOrDefault();
            if (!SiteLang.IsLanguageAvailable(to))
            {
                throw new Exception("New language not found!");
            }
            if (pTopic == null)
            {
                throw new Exception("Copy News not found!");
            }
            else
            {
                var copy = new n_parent_topic
                {
                    CreateAt = DateTime.UtcNow,
                    CreateBy = cMem.Fullname,
                    Id = AppFunc.NewShortId(),
                    LangCode = to,
                    Name = pTopic.Name,
                    Order = pTopic.Order,
                    ReId = pTopic.ReId,
                    URL = pTopic.URL,
                };
                _db.n_parent_topic.Add(copy);
                _db.SaveChanges();
                return copy;
            }
        }

        [HttpPost]
        [ActionName("Save")]
        [ValidateInput(false)]
        public ActionResult SaveSubmit(n_news nm, List<string> TopicId, List<string> picmore)
        {
            // update
            try
            {
                if (string.IsNullOrWhiteSpace(nm.Name))
                {
                    throw new Exception("Tên bài viết không được trống.");
                }

                if (string.IsNullOrEmpty(nm.TitleWebsite))
                {
                    nm.TitleWebsite = nm.Name;
                }
                if (string.IsNullOrEmpty(nm.SEODescription))
                {
                    nm.SEODescription = nm.ShortDescription;
                }
                var parentId = Request.Cookies["newspage_parentId"].Value;
                var ptopic = _db.n_parent_topic.Find(parentId);
                nm.ParentTopicId = ptopic?.ReId;
                nm.ParentTopicName = ptopic?.Name;
                nm.TopicId = JsonConvert.SerializeObject(TopicId);
                n_news savednews = News.SaveNews(nm, picmore, Request["id12char"]);
                TempData["success"] = "Lưu bài viết thành công";

                return RedirectToAction("detail", new { id = savednews.ReId, lang = savednews.LangCode });
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
                return RedirectToAction("save", new { id = nm.ReId, lang = nm.LangCode });
            }
        }

        [ValidateInput(false)]
        public JsonResult SaveJson(n_news nm, List<string> TopicId, List<string> picmore)
        {
            try
            {
                if (Request["checkbox_TitleWebsite"] == "1" || string.IsNullOrEmpty(nm.TitleWebsite))
                {
                    nm.TitleWebsite = nm.Name;
                }
                if (Request["checkbox_SEODescription"] == "1" || string.IsNullOrEmpty(nm.SEODescription))
                {
                    nm.SEODescription = nm.ShortDescription;
                }
                var parentId = Request.Cookies["newspage_parentId"].Value;
                var ptopic = _db.n_parent_topic.Find(parentId);
                nm.ParentTopicId = ptopic?.ReId;
                nm.ParentTopicName = ptopic?.Name;
                nm.TopicId = JsonConvert.SerializeObject(TopicId);
                n_news savednews = News.SaveNews(nm, picmore, Request["id12char"]);
                return Json(new object[] { true, "Đã lưu bài viết." });
            }
            catch (Exception e)
            {
                return Json(new object[] { true, e.Message, e.ToString() });
            }
        }

        public ActionResult Delete(string id, string lang)
        {
            try
            {
                var news = _db.n_news.FirstOrDefault(n => n.ReId == id && n.LangCode == lang);
                if (news == null)
                {
                    throw new Exception("Không tìm thấy bài viết");
                }
                _db.n_news.Remove(news);
                _db.SaveChanges();

                // Loc Inactive 
                // var picUrl = news.Picture;
                // FileInfo f = new FileInfo(Server.MapPath(picUrl));
                // if (f.Exists)
                // {
                //    f.Delete();
                // }
                TempData["success"] = "Xóa thành công";

                // cap nhat lai session
                AppLB.UserContent.GetWebIntroFromNews(true);
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }

            return RedirectToAction("index");
        }

        public ActionResult Detail(string id, string lang)
        {
            using (var db = new DBLangCustom())
            {
                db.setLanguage(lang);
                var list_topics = db.n_toppic.OrderBy(x => x.Order).ToList();
                var list_parentTopics = db.n_parent_topic.OrderBy(x => x.Name).ToList();
                var arrData = new object[] { list_parentTopics, list_topics };
                ViewBag.ArrData = arrData;
                var rs = _db.n_news.Where(n => n.ReId == id).Select(n => n.LangCode ?? defaultLanguage.Code).ToList() ?? new List<string>();
                var exL = SiteLang.GetListLangs().Where(s => rs.Contains(s.Code)).ToList();
                ViewBag.exitsLangs = exL;
                var d = db.n_news.FirstOrDefault(n => n.ReId == id);
                if (d == null)
                {
                    d = _db.n_news.FirstOrDefault(n => n.ReId == id);
                }

                if (d == null)
                {
                    return HttpNotFound();
                }
                else
                    if (string.IsNullOrEmpty(d?.LangCode))
                {
                    d.LangCode = defaultLanguage.Code;
                }
                return View(d);
            }
        }

        public ActionResult _partial_ptopic(string lang)
        {
            using (var db = new DBLangCustom())
            {
                db.setLanguage(lang);
                var list_parent_topic = db.n_parent_topic.OrderBy(x => x.CreateAt).ToList();
                var listRelatAble = _db.n_parent_topic.Where(n =>
                string.IsNullOrEmpty(n.LangCode) || n.LangCode == defaultLanguage.Code).ToList();
                ViewBag.listRelatAble = listRelatAble.Where(n => !list_parent_topic.Any(p => p.ReId == n.ReId)).OrderBy(x => x.CreateAt).ToList();
                return PartialView("_Partial_ParentTopic", list_parent_topic);
            }
        }

        public ActionResult SaveParentTopic(string id, string name, string lang, string RelationPostId)
        {
            using (var trans = _db.Database.BeginTransaction())
            {
                try
                {
                    if (!access.ContainsKey("update_posts"))
                    {
                        throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin.");
                    }

                    if (string.IsNullOrEmpty(name))
                    {
                        throw new Exception("Tên nhóm chủ đề không được trống");
                    }
                    var relat = _db.n_parent_topic.FirstOrDefault(p => p.ReId == RelationPostId);
                    string url = relat != null ? relat.URL : CommonFunc.ConvertNonUnicodeURL(name);
                    if (string.IsNullOrEmpty(id))
                    {
                        if (_db.n_parent_topic.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            throw new Exception("Nhóm chủ đề đã tồn tại.");
                        }

                        // create
                        var parent_topic = new n_parent_topic
                        {
                            Id = AppFunc.NewShortId(),
                            Name = name,
                            URL = url,
                            CreateAt = DateTime.Now,
                            CreateBy = User.Identity.Name,
                        };
                        parent_topic.LangCode = !string.IsNullOrEmpty(lang) ? lang : defaultLanguage.Code;
                        parent_topic.ReId = relat != null ? relat.ReId : parent_topic.Id;
                        _db.n_parent_topic.Add(parent_topic);
                        ViewBag.IdRespond = parent_topic.Id;
                    }
                    else
                    {
                        var parent_topic = _db.n_parent_topic.Find(id);
                        if (parent_topic == null)
                        {
                            throw new Exception("Không tìm thấy nhóm chủ đề.");
                        }
                        else if (_db.n_parent_topic.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && x.Id != parent_topic.Id))
                        {
                            throw new Exception("Nhóm chủ đề đã tồn tại.");
                        }

                        // update
                        parent_topic.Name = name;
                        parent_topic.UpdateAt = DateTime.Now;
                        parent_topic.UpdateBy = User.Identity.Name;
                        _db.Entry(parent_topic).State = System.Data.Entity.EntityState.Modified;
                        ViewBag.IdRespond = parent_topic.Id;

                        _db.n_toppic.Where(x => x.ParentTopicId == parent_topic.ReId && x.LangCode == parent_topic.LangCode).ToList()?.ForEach(x =>
                           {
                               x.ParentTopicName = parent_topic.Name;
                               x.UpdateAt = DateTime.Now;
                               x.UpdateBy = User.Identity.Name;
                               _db.Entry(x).State = System.Data.Entity.EntityState.Modified;
                           });

                        _db.n_news.Where(x => x.ParentTopicId == parent_topic.Id).ToList().ForEach(x =>
                        {
                            x.ParentTopicName = parent_topic.Name;
                            _db.Entry(x).State = System.Data.Entity.EntityState.Modified;
                        });
                    }

                    _db.SaveChanges();
                    trans.Commit();
                    return Json(new object[] { true, string.IsNullOrEmpty(id) ? "Tạo nhóm chủ đề mới hoàn tất." : "Chỉnh nhóm sửa chủ đề hoàn tất" });
                }
                catch (Exception ex)
                {
                    trans.Dispose();
                    return Json(new object[] { false, ex.Message, ex.ToString() });
                }
            }
        }

        public JsonResult DeleteParentTopic(string id)
        {
            try
            {
                if (!access.ContainsKey("update_posts"))
                {
                    throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin.");
                }

                var parent_topic = _db.n_parent_topic.Find(id);
                if (parent_topic == null)
                {
                    throw new Exception("Không tìm thấy nhóm chủ đề");
                }
                else if (parent_topic.IsDefault == true)
                {
                    throw new Exception("Vui lòng không xóa nhóm chủ đề này");
                }
                else if (_db.n_toppic.Any(x => x.ParentTopicId == parent_topic.ReId && x.LangCode == parent_topic.LangCode))
                {
                    throw new Exception("Nhóm chủ đề này có chứa các chủ đề, không thể xóa!");
                }

                _db.n_parent_topic.Remove(parent_topic);
                _db.SaveChanges();

                return Json(new object[] { true, "Xóa thành công" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }
        public JsonResult ptopicOrder(List<string> data, string lang)
        {
            try
            {
                using (var db = new DBLangCustom())
                {
                    db.setLanguage(lang);
                    db.n_parent_topic.Where(p => data.Contains(p.ReId)).ToList().ForEach(p =>
                    {
                        p.Order = data.IndexOf(p.ReId);
                        db.Entry(p).State = System.Data.Entity.EntityState.Modified;
                    });
                    db.SaveChanges();
                }
                return Json(new object[] { true, "Đã lưu hoàn tất!" });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }

        }
        #region Toppic
        public ActionResult Toppic(string lang)
        {
            using (var db = new DBLangCustom())
            {
                db.setLanguage(lang);
                var toppics = db.n_toppic.OrderBy(t => t.Order).ToList();
                ViewBag.ParentTopic = db.n_parent_topic.OrderBy(x => x.CreateAt).ToList();
                ViewBag.Header = "Tất cả nhóm chủ đề";
                return View(toppics);
            }
        }

        public ActionResult SearchTopic(string parentId, string searchText)
        {
            var list_toppics = new List<n_toppic>();
            ViewBag.Search = searchText;
            try
            {
                searchText = AppLB.CommonFunc.ConvertNonUnicodeURL(searchText).ToLower();

                list_toppics = (from topic in _db.n_toppic.AsEnumerable<n_toppic>()
                                join parentTopic in _db.n_parent_topic
                                on topic.ParentTopicId equals parentTopic.ReId
                                where (string.IsNullOrEmpty(parentId)
                                || (!string.IsNullOrEmpty(parentId) && parentTopic.Id == parentId))
                                && (string.IsNullOrEmpty(searchText)
                                || (!string.IsNullOrEmpty(searchText)
                                && AppLB.CommonFunc.ConvertNonUnicodeURL(topic.Name).ToLower().Contains(searchText)))
                                orderby topic.Order
                                select topic).ToList();

                ViewBag.ErrMsg = string.Empty;
                ViewBag.ParentTopic = _db.n_parent_topic.OrderBy(x => x.CreateAt).ToList();
            }
            catch (Exception ex)
            {
                ViewBag.ErrMsg = ex.Message;
            }

            return PartialView("_partial_toppic", list_toppics);
        }

        public JsonResult GetListTopicByParent(string parentId)
        {
            try
            {
                var list_topic = _db.n_toppic.Where(x => x.ParentTopicId == parentId).ToList();
                return Json(new object[] { true, list_topic });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        /// <summary>
        /// Get list Topic.
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public ActionResult _partial_toppic(string parentId, string lang)
        {
            using (var db = new DBLangCustom())
            {
                db.setLanguage(lang);
                var list_toppics = new List<n_toppic>();
                try
                {
                    if (parentId == null || parentId == "all")
                    {
                        list_toppics = db.n_toppic.OrderBy(t => t.Order).ToList();
                        ViewBag.Header = "Tất cả nhóm chủ đề";
                    }
                    else
                    {
                        var parentToppic = db.n_parent_topic.FirstOrDefault(p => p.Id == parentId);
                        ViewBag.Header = $"Nhóm chủ đề {parentToppic?.Name}";
                        ViewBag.ParentUrl = parentToppic?.URL;
                        list_toppics = db.n_toppic.Where(t => t.ParentTopicId == parentToppic.ReId).OrderBy(t => t.Order).ToList();
                    }

                    ViewBag.ErrMsg = string.Empty;
                }
                catch (Exception ex)
                {
                    ViewBag.ErrMsg = ex.Message;
                }
                ViewBag.ParentTopic = db.n_parent_topic.OrderBy(x => x.CreateAt).ToList();
                return PartialView("_partial_toppic", list_toppics);
            }
        }

        /// <summary>
        /// Add toppic.
        /// </summary>
        /// <param name="Id">Toppic Id.</param>
        /// <param name="Name">Toppic name.</param>
        /// <param name="Order">Toppic order.</param>
        /// <param name="ShowMenu">Show menu - true/false.</param>
        /// <param name="ShowNews">Show news - true/false.</param>
        /// <param name="parentId">Parent topic id.</param>
        /// <param name="relatedTopicId"></param>
        /// <returns></returns>
        public JsonResult SaveToppic(string Id, string Name, int? Order, bool? ShowMenu, bool? ShowNews, string parentId, string relatedTopicId, string seo_desc, string seo_title)
        {
            using (var trans = _db.Database.BeginTransaction())
            {
                try
                {
                    if (!access.ContainsKey("update_posts"))
                    {
                        throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin.");
                    }

                    var parent_topic = _db.n_parent_topic.Find(parentId);
                    if (parent_topic == null)
                    {
                        throw new Exception("Không tìm thấy nhóm chủ đề");
                    }

                    string url = AppLB.CommonFunc.ConvertNonUnicodeURL(Name);
                    if (string.IsNullOrEmpty(Id))
                    {
                        // create
                        if (_db.n_toppic.Any(t => t.Name == Name))
                        {
                            throw new Exception("Tên chủ đề đã tồn tại.");
                        }

                        n_toppic tp = new()
                        {
                            TopicId = AppFunc.NewShortId(),
                            Name = Name,
                            Order = Order,
                            Seo_desc = seo_desc,
                            Seo_title = seo_title,
                            ShowMenu = ShowMenu,
                            ShowNews = ShowNews,
                            CreatedAt = DateTime.Now,
                            CreateBy = User.Identity.Name,
                            URL = url,
                            ParentTopicId = parent_topic.ReId,
                            ParentTopicName = parent_topic.Name,
                            LangCode = parent_topic.LangCode,
                        };
                        tp.ReId = relatedTopicId ?? tp.TopicId;
                        // var topic = _db.n_toppic.Find(relatedTopicId);
                        // if (topic != null)
                        // {
                        //    tp.RelatedTopicId = topic.TopicId;
                        //    tp.RelatedTopicName = topic.Name;
                        // }
                        // if (string.IsNullOrEmpty(tp.ReId))
                        // {
                        //    tp.ReId = tp.TopicId;
                        // }
                        _db.n_toppic.Add(tp);
                    }
                    else
                    {
                        // update
                        var listTopic = _db.n_toppic.ToList();
                        var toppic = listTopic.Where(x => x.TopicId == Id).FirstOrDefault();
                        if (toppic == null)
                        {
                            throw new Exception("Không tìm thấy chủ đề");
                        }
                        else if (listTopic.Any(t => t.Name == Name && t.TopicId != toppic.TopicId))
                        {
                            throw new Exception("Tên chủ đề đã tồn tại");
                        }
                        var oldname = toppic.Name;
                        toppic.Name = Name;
                        toppic.Order = Order;
                        toppic.Seo_desc = seo_desc;
                        toppic.Seo_title = seo_title;
                        toppic.ShowMenu = ShowMenu;
                        toppic.ShowNews = ShowNews;
                        toppic.UpdateAt = DateTime.Now;
                        toppic.UpdateBy = User.Identity.Name;
                        toppic.URL = url;
                        toppic.ParentTopicId = parent_topic.ReId;
                        toppic.ParentTopicName = parent_topic.Name;
                        toppic.LangCode = parent_topic.LangCode;
                        toppic.ReId = relatedTopicId ?? toppic.RelatedTopicId ?? toppic.TopicId;
                        // var tp = listTopic.Where(x => x.TopicId == relatedTopicId).FirstOrDefault();
                        // if (tp != null)
                        // {
                        //    toppic.RelatedTopicId = tp.TopicId;
                        //    toppic.RelatedTopicName = tp.Name;
                        // }
                        // else
                        // {
                        //    toppic.RelatedTopicId = null;
                        //    toppic.RelatedTopicName = null;
                        // }
                        _db.Entry(toppic).State = System.Data.Entity.EntityState.Modified;

                        // update lại topic name cho blog
                        var list_new = _db.n_news.Where(x => x.TopicId.Contains("\"" + toppic.ReId + "\"")).ToList();
                        if (list_new != null && list_new.Count > 0)
                        {
                            foreach (var item in list_new)
                            {
                                item.ToppicName.Replace("\"" + oldname + "\"", "\"" + toppic.Name + "\"");
                                _db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                            }
                        }

                        // foreach (var item in listTopic.Where(x => x.RelatedTopicId == toppic.TopicId))
                        // {
                        //    item.RelatedTopicName = toppic.Name;
                        //    _db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                        // }
                    }

                    _db.SaveChanges();
                    trans.Commit();

                    return Json(new object[] { true, "Lưu thành công" });
                }
                catch (Exception ex)
                {
                    trans.Dispose();
                    return Json(new object[] { false, ex.Message });
                }
            }
        }

        public JsonResult EditToppic(string Id)
        {
            try
            {
                var toppic = _db.n_toppic.Find(Id);

                if (toppic == null)
                {
                    throw new Exception("Không tìm thấy chủ đề");
                }

                return Json(new object[] { true, toppic });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        /// <summary>
        /// Delete toppic.
        /// </summary>
        /// <param name="Id">Toppic Id.</param>
        /// <returns></returns>
        public JsonResult DeleteToppic(string Id)
        {
            using (var trans = _db.Database.BeginTransaction())
            {
                try
                {
                    if (!access.ContainsKey("update_posts"))
                    {
                        throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin.");
                    }

                    var toppic = _db.n_toppic.Find(Id);
                    if (toppic == null)
                    {
                        throw new Exception("Không tìm thấy chủ đề");
                    }
                    else if (toppic.IsDefault == true)
                    {
                        throw new Exception("Vui lòng không xóa chủ đề này");
                    }

                    // các bài viết nào thuộc chủ đề bị xóa thì đưa về dạng không có chủ đề
                    foreach (var item in _db.n_news.Where(n => n.TopicId.Contains("\"" + toppic.ReId + "\"")).ToList())
                    {
                        item.TopicIds = item.TopicIds.Where(i => i != toppic.ReId).ToList();
                        item.ToppicNames = item.ToppicNames.Where(i => i != toppic.Name).ToList();
                        _db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                    }

                    // update lai chu de lien quan
                    // foreach (var item in _db.n_toppic.Where(x => x.RelatedTopicId == toppic.TopicId).ToList())
                    // {
                    //    item.RelatedTopicId = null;
                    //    item.RelatedTopicName = null;
                    //    _db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                    // }

                    // delete toppic
                    _db.n_toppic.Remove(toppic);
                    _db.SaveChanges();
                    trans.Commit();

                    return Json(new object[] { true, "Xóa thành công" });
                }
                catch (Exception ex)
                {
                    trans.Dispose();
                    return Json(new object[] { false, ex.Message });
                }
            }
        }

        #endregion

        #region Tags
        public ActionResult Tags()
        {
            return View();
        }

        /// <summary>
        /// Reload list tag.
        /// </summary>
        /// <param name="tagOpt">"news" or "product"
        /// "news" => reload tag bài viết, "product" => reload tag sản phẩm.</param>
        /// <returns></returns>
        public ActionResult _partial_tags(string tagOpt)
        {
            if (tagOpt == "news")
            {
                // Load tags news
                ViewBag.TagsNews = _db.n_news_tags.Select(t => new
                {
                    t,
                    used = _db.n_news.Any(n => n.Tags_code.Contains("," + t.tag_code + ",")
                    || n.Tags_code.StartsWith(t.tag_code + ",")
                    || n.Tags_code.EndsWith("," + t.tag_code)
                    || n.Tags_code == t.tag_code),
                }).ToList().Select(item => (tag: item.t, used: item.used)).ToList();
            }
            else
            {
                // Load tags product
                ViewBag.TagsProduct = _db.tags.Select(t => new
                {
                    t,
                    used = _db.products.Any(n => n.TagID.Contains("," + t.TagCode + ",")
                    || n.TagID.StartsWith(t.TagCode + ",")
                    || n.TagID.EndsWith("," + t.TagCode)
                    || n.TagID == t.TagCode),
                }).ToList().Select(item => (tag: item.t, used: item.used)).ToList();
            }
            ViewBag.TagOpt = tagOpt;
            return PartialView("_partial_tag");
        }

        /// <summary>
        /// Create Tag.
        /// </summary>
        /// <param name="tags_name">Tag name.</param>
        /// <param name="tag_opt">"news" or "product"
        /// "news" => tạo tag cho bài viết, "product" => tạo tag cho sản phẩm.</param>
        /// <returns></returns>
        public JsonResult SaveTag(string id, string tags_name, string tag_opt, string seo_title, string seo_desc)
        {
            try
            {
                if (string.IsNullOrEmpty(tags_name))
                {
                    throw new Exception("Vui lòng nhập tên tag");
                }

                var code = CommonFunc.ConvertNonUnicodeURL(tags_name);
                if (tag_opt == "news")
                {
                    if (_db.n_news_tags.Any(t => t.tag_code == code && t.id != id))
                    {
                        throw new Exception("Tên tag bị trùng, vui lòng nhập tên khác.");
                    }
                    if (!string.IsNullOrEmpty(id))
                    {
                        var tag = _db.n_news_tags.Find(id);
                        if (tag == null)
                        {
                            throw new Exception("không tìm thấy tag");
                        }
                        var old_code = tag.tag_code;
                        var old_name = tag.tag_name;
                        tag.tag_code = code;
                        tag.tag_name = tags_name;
                        tag.seo_title = seo_title;
                        tag.seo_desc = seo_desc;
                        _db.Entry(tag).State = System.Data.Entity.EntityState.Modified;
                        _db.n_news.Where(n => n.Tags_code.Contains(old_code)).ToList().ForEach(n =>
                        {
                            if (n.Tags_code.Split(',').Contains(old_code))
                            {
                                n.Tags_code = string.Join(",", n.Tags_code?.Split(',').Select(t => (t.ToLower() == old_code.ToLower()) ? code : t));
                                n.Tags = string.Join(",", n.Tags?.Split(',').Select(t => (t.ToLower() == old_name.ToLower()) ? tags_name : t));
                            }
                            _db.Entry(n).State = System.Data.Entity.EntityState.Modified;
                        });

                        _db.SaveChanges();
                    }
                    else
                    {
                        #region TẠO TAG CHO BÀI VIẾT
                        var newsTag = new n_news_tags()
                        {
                            id = AppFunc.NewShortId(),
                            tag_code = code,
                            tag_name = tags_name,
                            seo_title = seo_title,
                            seo_desc = seo_desc,
                        };
                        _db.n_news_tags.Add(newsTag);
                        _db.SaveChanges();
                        #endregion
                    }
                }
                else
                {
                    if (_db.tags.Any(t => t.TagCode == code && t.Id != id))
                    {
                        throw new Exception("Tên tag đã có, vui lòng nhập tên khác.");
                    }
                    if (!string.IsNullOrEmpty(id))
                    {
                        var tag = _db.tags.Find(id);
                        if (tag == null)
                        {
                            throw new Exception("không tìm thấy tag");
                        }
                        var old_code = tag.TagCode;
                        var old_name = tag.TagName;
                        tag.TagCode = code;
                        tag.TagName = tags_name;
                        tag.seo_title = seo_title;
                        tag.seo_desc = seo_desc;
                        _db.Entry(tag).State = System.Data.Entity.EntityState.Modified;
                        _db.products.Where(n => n.TagID.Contains(old_code)).ToList().ForEach(n =>
                        {
                            if (n.TagID.Split(',').Contains(old_code))
                            {
                                n.TagID = string.Join(",", n.TagID?.Split(',').Select(t => (t.ToLower() == old_code.ToLower()) ? code : t));
                                n.TagName = string.Join(",", n.TagName?.Split(',').Select(t => (t.ToLower() == old_name.ToLower()) ? tags_name : t));
                            }
                            _db.Entry(n).State = System.Data.Entity.EntityState.Modified;
                        });
                        _db.SaveChanges();
                    }
                    else
                    {
                        var productTag = new tag()
                        {
                            Id = AppFunc.NewShortId(),
                            TagCode = code,
                            TagName = tags_name,
                            seo_title = seo_title,
                            seo_desc = seo_desc,
                        };
                        _db.tags.Add(productTag);
                        _db.SaveChanges();
                    }
                }

                return Json(new object[] { true, tag_opt });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }

        public JsonResult GetTag(string id, string type)
        {
            try
            {
                if (type == "news")
                {
                    var tag = _db.n_news_tags.Find(id);
                    if (tag == null)
                    {
                        throw new Exception("Không tìm thấy tag");
                    }
                    return Json(new object[] { true, tag });
                }
                else
                {
                    var tag = _db.tags.Find(id);
                    if (tag == null)
                    {
                        throw new Exception("Không tìm thấy tag");
                    }
                    return Json(new object[] { true, tag });

                }
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message, ex.ToString() });
            }

        }
        public ActionResult _Tags_partial(string tags_checked)
        {
            return PartialView("_Partial_news_tags", tags_checked ?? string.Empty);
        }

        public JsonResult RemoveTags(List<string> tags)
        {
            if (tags != null && tags.Count > 0)
            {
                var tags_remove = _db.n_news_tags.Where(t => tags.Contains(t.id)).ToList();
                _db.n_news_tags.RemoveRange(tags_remove);
                _db.SaveChanges();
                return Json(new object[] { true });
            }
            else
            {
                return Json(new object[] { false });
            }
        }

        /// <summary>
        /// Remove tag.
        /// </summary>
        /// <param name="id">Tag id.</param>
        /// <param name="tagOpt">"news" or "product"
        /// "news" => remove tag bài viết, "product" => remove tag sản phẩm.</param>
        /// <returns></returns>
        public JsonResult RemoveTag(string id, string tagOpt)
        {
            try
            {
                if (tagOpt == "news")
                {
                    #region XÓA TAG BÀI VIẾT
                    var tag_remove = _db.n_news_tags.Find(id);
                    if (tag_remove == null)
                    {
                        throw new Exception("Không tìm thấy tag");
                    }
                    _db.n_news.Where(t => t.Tags_code == tag_remove.tag_code
                    || t.Tags_code.StartsWith(tag_remove.tag_code + ",")
                    || t.Tags_code.EndsWith("," + tag_remove.tag_code)
                    || t.Tags_code.Contains("," + tag_remove.tag_code + ",")).ToList()
                    .ForEach(n =>
                    {
                        n.Tags_code = string.Join(",", n.Tags_code.Split(',').ToList().Where(t => t != tag_remove.tag_code));
                        n.Tags = string.Join(",", n.Tags.Split(',').ToList().Where(t => CommonFunc.ConvertNonUnicodeURL(t) != tag_remove.tag_code));
                        _db.Entry(n).State = System.Data.Entity.EntityState.Modified;
                    });
                    _db.n_news_tags.Remove(tag_remove);
                    #endregion
                }
                else
                {
                    #region XÓA TAG BÀI SẢN PHẨM
                    var tag_remove = _db.tags.Find(id);
                    if (tag_remove == null)
                    {
                        throw new Exception("Không tìm thấy tag");
                    }
                    _db.products.Where(t => t.TagID == tag_remove.TagCode
                    || t.TagID.StartsWith(tag_remove.TagCode + ",")
                    || t.TagID.EndsWith("," + tag_remove.TagCode)
                    || t.TagID.Contains("," + tag_remove.TagCode + ",")).ToList()
                    .ForEach(p =>
                    {
                        p.TagID = string.Join(",", p.TagID.Split(',').ToList().Where(t => t != tag_remove.TagCode));
                        // p.Tags = string.Join(",", n.Tags.Split(',').ToList().Where(t => CommonFunc.ConvertNonUnicodeURL(t) != tag_remove.tag_code));
                        _db.Entry(p).State = System.Data.Entity.EntityState.Modified;
                    });
                    _db.tags.Remove(tag_remove);
                    #endregion
                }

                _db.SaveChanges();
                return Json(new object[] { true, "Xóa thành công" });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }
        #endregion

        #region ajax function
        public JsonResult LoadParentTopic(string lang)
        {
            try
            {
                using (var db = new DBLangCustom())
                {
                    db.setLanguage(lang);
                    var parentTopic = db.n_parent_topic.Select(x => new { x.Id, x.Name }).OrderBy(x => x.Name).ToList();
                    return Json(new object[] { true, parentTopic, Session["parentId"]?.ToString() });
                }
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        public JsonResult LoadToppic(string parentTopicId)
        {
            try
            {
                var toppic = (from t in _db.n_toppic
                              where string.IsNullOrEmpty(parentTopicId)
                              || (!string.IsNullOrEmpty(parentTopicId)
                              && t.ParentTopicId == parentTopicId)
                              orderby t.Name
                              select t).ToList();

                return Json(new object[] { true, toppic });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }
        #endregion

    }
}