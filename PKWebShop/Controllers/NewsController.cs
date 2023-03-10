namespace PKWebShop.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using Microsoft.EntityFrameworkCore.Internal;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using PKWebShop.AppLB;
    using PKWebShop.Models;
    using PKWebShop.Models.CustomizeModels;

    public class NewsController : ExpiredCheckController
    {
        public ActionResult Index(string Url, string search, string topic, string tag, int page = 1)
        {
            try
            {
                Url = Url?.ToLower();
                
                // lấy dưa liệu từ db
                var db = new DBLangCustom();

                // lấy list new từ db
                var listNews = db.n_news;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    // chuyển đổi giá tri tìm kiếm thành chữ không dấu đồng thời thay thế các dấu - bằng % sau đó chuyển thành chữ thường
                    search = CommonFunc.ConvertNonUnicodeURL(search, false).Replace("-", "%").ToLower();

                    // tìm kiếm các bì viết dựa trên từ khóa search
                    listNews = new WebShopEntities().n_news.SqlQuery(CommonFunc.SearchCommand("n_news", search, "Name") + " and LangCode = '" + db.LangCode + "'").AsQueryable();
                }

                // lấy topic bài biết dựa trên url truyền vào 
                var ptopic = db.n_parent_topic.Where(t => t.URL == Url).FirstOrDefault();
                
                // lấy ra danh sách bài viết dự trên prentTopicid của đối tương ptopic
                listNews = listNews.Where(n => n.ParentTopicId == ptopic.ReId);

                // nếu topic != null
                if (!string.IsNullOrEmpty(topic))
                {
                    // tìm  danh sách bài viết dựa trên url ở đay là topic truyền vào
                    var topic1 = db.n_toppic.FirstOrDefault(t => t.URL == topic);

                    // tìm danh sách các bài viết dựa trên topic id
                    listNews = listNews.Where(n => n.TopicId.Contains("\"" + topic1.ReId + "\""));

                    // sau đó chuyền danh sách topic vào viewbag
                    ViewBag.topic = topic1;
                }
                
                // truyền các danh sách tag dựa trên các topic id topic lấy từ trên
                ViewBag.tags = db.n_news.Where(n => n.ParentTopicId == ptopic.ReId).Select(x => x.Tags).Join(",").Split(',').Distinct().ToList() ?? new List<string>();
                
                // nếu tag != null 
                if (!string.IsNullOrEmpty(tag))
                {
                    // covert chữ có dấu sang ko dấu là và đổi dấu % thành - (có dạng san-pham-1)
                    tag = CommonFunc.ConvertNonUnicodeURL(tag);

                    // lấy tag từ tagcode truyền vào
                    var _tag = db.n_news_tags.FirstOrDefault(t => t.tag_code == tag);

                    // nếu tag có tồn tại
                    if (_tag != null)
                    {
                        // các giá trị tag vào viewbag
                        ViewBag.tag = _tag;
                        ViewBag.Title = _tag.seo_title;
                        ViewBag.SEO_Desc = _tag.seo_desc;
                    }
                    // lấy danh sách bài viết có tag_code = tagcode ở trên
                    listNews = listNews.Where(n => n.Tags_code.Contains(tag));
                }
                //truyền ptopic vào viewbag
                ViewBag.ptopic = ptopic;

                // paged
                int rpp = 6;
                
                // lấy số lượng bài viết
                int totalRecords = listNews.Count();
                
                // tính các thông số để phân trang
                CommonFunc.PagedList(totalRecords, ref page, rpp, out int pagecount, out int skip);

                // truyền vào viewbag các thông số trên
                ViewData["page"] = page;
                ViewData["pagecount"] = pagecount;
                ViewBag.UrlPage = Url;
                ViewBag.data = ViewData;

                // sắp xếp lại list new
                var order_listNews = listNews.OrderBy(n => n.Order == null).ThenBy(n => n.Order).ThenByDescending(o => o.CreatedAt);

                // truyền list topic vào viewbag nếu ko có thì khởi tạo list n_topic để tránh lỗi null
                ViewBag.topics = db.n_toppic.Where(p => p.ParentTopicId == ptopic.ReId).ToList() ?? new List<n_toppic>();

                // SEO Meta
                var webinfo = db.webconfigurations.FirstOrDefault();
                if (!string.IsNullOrEmpty(webinfo.SEO_Meta))
                {
                    var list_SEO = JsonConvert.DeserializeObject<List<SiteSEO.SEO_Meta>>(webinfo.SEO_Meta);
                    var SEO_tintuc = list_SEO.FirstOrDefault(x => x.code == SiteSEO.code_SEO.TinTuc);
                    ViewBag.SEO_tintuc = SEO_tintuc;
                }
                
                ViewBag.WebInfo = webinfo;

                // truyền danh sách bài viết với các thông số để phân trang
                return View(order_listNews.Skip(skip).Take(rpp).ToList());
            }
            catch (Exception e)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Overview(string Url)
        {
            var db = new DBLangCustom();
            var p_top = db.n_parent_topic.Where(t => t.URL == Url).FirstOrDefault();
            var group_news = from news in db.n_news group news by news.TopicId into g select g;
            var group_topic = (from t in db.n_toppic.Where(p => p_top.ReId == p.ParentTopicId)
                               let nn = t.Order != null
                               orderby nn descending, t.Order
                               join g_news in group_news
                               on t.TopicId equals g_news.Key
                               select new { topic = t, news = g_news.ToList() }).ToList()
                               .Select(g => (topic: g.topic, news: g.news.OrderByDescending(n => n.Order).ThenByDescending(n => n.CreatedAt).Take(4))).ToList();
            ViewBag.topics = db.n_toppic.Where(p => p.ParentTopicId == p_top.ReId).ToList() ?? new List<n_toppic>();
            ViewBag.tags = db.n_news_tags.OrderBy(t => t.tag_name).ToList() ?? new List<n_news_tags>();
            return View(group_topic);
        }

        public ActionResult Page(n_toppic TopicId = null)
        {
            var db = new DBLangCustom();
            var list_service = db.n_news.Where(n => n.ParentTopicId == "3").ToList() ?? new List<n_news>();
            return View(list_service);
        }

        public ActionResult PlacesList()
        {
            return View();
        }

        public ActionResult Detail(string url, string detailUrl, string Id)
        {
            try
            {
                DBLangCustom db = new();
                var news = db.n_news.Where(n => n.UrlCode == detailUrl && n.ReId == Id).FirstOrDefault();

                if (news == null)
                {
                    var ptopic = db.n_parent_topic.Where(t => t.URL == url).FirstOrDefault();
                    news = db.n_news.FirstOrDefault(n => n.ParentTopicId == ptopic.ReId);
                    return Redirect(url + "/" + news.UrlCode);
                }
                else
                {
                    // ViewBag.topbg = CommonFunc.getTopBackground();
                    var ptopic = db.n_parent_topic.FirstOrDefault(p => p.ReId == news.ParentTopicId);
                    ViewBag.ptopic = ptopic;
                    url = ptopic.URL;

                    // ViewBag.ListTag = db.n_news_tags.ToList();
                    ViewBag.pTitle = db.menus.Where(m => m.URL == "/" + url).FirstOrDefault()?.Name ?? ptopic?.Name ?? "Tin tức";
                    ViewBag.UrlPage = url;

                    // ViewBag.UrlItem = detailUrl;
                    ViewBag.RelateNews = db.n_news.Where(n => n.ParentTopicId == ptopic.ReId && n.NewsId != news.NewsId).OrderByDescending(o => o.Order).Take(3).ToList() ?? new List<n_news>();
                    news.ViewCount = (news.ViewCount ?? 0) + 1;
                    db.Entry(news).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    ViewBag.topics = db.n_toppic.Where(p => p.ParentTopicId == ptopic.ReId).ToList() ?? new List<n_toppic>();
                    ViewBag.tags = db.n_news.Where(n => n.ParentTopicId == ptopic.ReId).Select(x => x.Tags).Join(",").Split(',').Distinct().ToList() ?? new List<string>();
                    
                    news.Decription = GetTitleIdForDesc(news.Decription);
                    ViewBag.og_image = news.Picture;
                    ViewBag.Title = news.Name;

                    return View(news);
                }

                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                return Redirect("/notfound");
            }
        }

        public string GetTitleIdForDesc(string desc)
        {
            try
            {
                if (string.IsNullOrEmpty(desc))
                {
                    return string.Empty;
                }

                desc = desc.Replace("<h2", "<h2 id='ct_'").Replace("<h3", "<h3 id='ct_'");
                int n = 0;
                int i = desc.IndexOf("ct_'");
                while (i != -1)
                {
                    desc = desc.Insert(i + 3, n++.ToString());
                    i = desc.IndexOf("ct_'");
                }
                return desc;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #region Fixed detail pages
        public ActionResult Pricing()
        {
            var db = new DBLangCustom();
            var news = (from n in db.n_news
                        where n.ParentTopicId == "2"
                        && string.IsNullOrEmpty(n.TopicId) == true
                        && n.Active == true
                        orderby n.CreatedAt descending
                        select n).FirstOrDefault();
            news.ParentTopicName = string.Empty;

            string dichvu = UserContent.Web_Feature.trangchu_dichvu.ToString();
            ViewBag.dichvu = db.sectionfeatures.Where(s => s.Code == dichvu && s.Status == true).FirstOrDefault();
            ViewBag.trangkhac_topbg = (from f in db.sectionfeaturedetails
                                       where (f.URL ?? string.Empty).Contains("bang-gia")
                                       select f).FirstOrDefault();
            return View("Detail", news);
        }

        public ActionResult About()
        {
            var db = new DBLangCustom();
            var news = (from n in db.n_news
                        where n.ParentTopicId == "1"
                        && string.IsNullOrEmpty(n.TopicId) == true
                        && n.Active == true
                        orderby n.CreatedAt descending
                        select n).FirstOrDefault();
            news.ParentTopicName = string.Empty;

            string dichvu = UserContent.Web_Feature.trangchu_dichvu.ToString();
            ViewBag.dichvu = db.sectionfeatures.Where(s => s.Code == dichvu && s.Status == true).FirstOrDefault();
            ViewBag.trangkhac_topbg = (from f in db.sectionfeaturedetails
                                       where (f.URL ?? string.Empty).Contains("gioi-thieu")
                                       select f).FirstOrDefault();
            return View("Detail", news);
        }
        #endregion
    }
}