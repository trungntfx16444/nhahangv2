namespace AdminPage.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Web.Mvc;
    using AdminPage.AppLB;
    using AdminPage.Models.CustomizeModels;
    using AdminPage.Models;
    using AdminPage.Services;
    using AdminPage.Utils;

    [Authorize]
    public class BonusPointsController : ExpiredCheckController
    {
        private AdminEntities db = new();

        // GET: Admin/BonusPoints
        public ActionResult Index()
        {
            ViewBag.fromdate = Session["BonusPoints_fromdate"]?.ToString() ?? "2020-01-01";
            ViewBag.todate = Session["BonusPoints_todate"]?.ToString() ?? DateTime.Now.ToString("yyyy-MM-dd");
            ViewBag.BonusPointConfigs = db.bonuspointconfigs.ToList();
            return View(db.customer_score.OrderByDescending(x => x.FromDate).ToList());
        }
        public JsonResult Load_table(DataTable_request data, string search, string status, DateTime? fromdate, DateTime? todate)
        {
            // Session["parentId"] = parentTopicId = !string.IsNullOrEmpty(parentTopicId) ? parentTopicId : (Session["parentId"]?.ToString() ?? "");
            // db.setLanguage(language);

            var langDefault = SiteLang.GetDefault();
            ViewBag.ProductCategory = db.categories.Where(x => x.LangCode == langDefault.Code).OrderBy(x => x.CategoryName).ToList();
            var recordsTotal = db.customer_score.Count();
            var news = db.customer_score.SqlQuery(CommonFunc.SearchCommand("customer_score", search, "Name")).AsQueryable();
            news = news.Where(n => n.Active != false);

            if (!string.IsNullOrEmpty(status))
            {
                var curdate = DateTime.Now.Date;
                if (status == "1")
                {
                    news = news.Where(n => (n.FromDate == null || n.FromDate.Value.Date <= curdate)
                    && (n.ToDate == null || n.ToDate.Value.Date >= curdate));
                }
                else if (status == "0")
                {
                    news = news.Where(n => (n.FromDate != null && n.FromDate.Value.Date > curdate));
                }
                else if (status == "-1")
                {
                    news = news.Where(n => (n.ToDate != null && n.ToDate.Value.Date < curdate));
                }
            }
            if (fromdate != null)
            {
                Session["BonusPoints_fromdate"] = fromdate.Value.ToString("yyyy-MM-dd");
                news = news.Where(n => (n.CreateAt != null && n.CreateAt.Value.Date >= fromdate));
            }
            if (todate != null)
            {
                Session["BonusPoints_todate"] = todate.Value.ToString("yyyy-MM-dd");
                news = news.Where(n => (n.CreateAt != null && n.CreateAt.Value.Date <= todate));
            }
            var filtered_count = news.Count();
            string[] orderColumns = { "Name", "CreateAt" };
            var orderColumn = orderColumns.ElementAtOrDefault(data.order?.FirstOrDefault()?.column ?? 1) ?? "CreateAt";
            var order = orderColumn + " " + data.order?.FirstOrDefault()?.dir;

            var list_news = news.OrderBy(order).Skip(data.start).Take(data.length).ToList();
            var html = CommonFunc.RenderRazorViewToString("_tableData", list_news, this);

            return Json(new { draw = data.draw, recordsFiltered = filtered_count, recordsTotal = recordsTotal, data = html });
        }

        [HttpPost]
        public ActionResult Add(string id)
        {
            try
            {
                var langDefault = SiteLang.GetDefault();
                ViewBag.ListProductCategory = db.categories.Where(x => x.LangCode == langDefault.Code).OrderBy(x => x.CategoryName).ToList();
                var cus_score = db.customer_score.Find(id);
                if (cus_score != null)
                {
                    return PartialView("_ScoreModalPartial", cus_score);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.InnerException?.Message ?? ex.Message;
            }
            return PartialView("_ScoreModalPartial", new customer_score { Active = true });
        }

        [HttpPost]
        public JsonResult Save(customer_score data, List<string> products)
        {
            try
            {
                if (!string.IsNullOrEmpty(data.Id))
                {
                    // update
                    var cus_score = db.customer_score.Find(data.Id);
                    if (cus_score != null)
                    {
                        cus_score.Name = data.Name;
                        cus_score.Active = Request["Active"] == "1" ? true : false;
                        cus_score.FromDate = data.FromDate;
                        cus_score.ToDate = data.ToDate;
                        cus_score.Order_GranTotal = double.Parse(Request["Order_GranTotal"]?.Replace(",", string.Empty)?.ToString() ?? "0");
                        cus_score.Score = data.Score;
                        cus_score.Products = products != null ? string.Join("|", products) : string.Empty;
                        cus_score.MultipleOnOrder = data.MultipleOnOrder;
                        cus_score.MultipleOnCustomer = data.MultipleOnCustomer;
                        cus_score.UpdateAt = DateTime.Now;
                        cus_score.UpdateBy = User.Identity.Name;
                        db.Entry(cus_score).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        throw new Exception("Đối tượng không tồn tại");
                    }
                }
                else
                {
                    // add new
                    var cus_score = data;
                    Random rd = new();
                    cus_score.Id = AppFunc.NewShortId();
                    cus_score.Active = Request["Active"] == "1" ? true : false;
                    cus_score.Order_GranTotal = double.Parse(Request["Order_GranTotal"]?.Replace(",", string.Empty)?.ToString() ?? "0");
                    cus_score.Products = products != null ? string.Join("|", products) : string.Empty;
                    cus_score.CreateAt = DateTime.Now;
                    cus_score.CreateBy = User.Identity.Name;

                    if (Request["ToDate"]?.Length == 10 && DateTime.TryParse(Request["ToDate"], out DateTime toDate))
                    {
                        cus_score.ToDate = toDate;
                    }

                    db.customer_score.Add(cus_score);
                    db.SaveChanges();
                }

                return Json(new object[] { true, "Lưu thành công." });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, "Lưu thất bại. " + ex.Message });
            }
        }

        public JsonResult Delete(string id)
        {
            try
            {
                var cus_score = db.customer_score.Find(id);
                if (cus_score != null)
                {
                    db.customer_score.Remove(cus_score);
                    db.SaveChanges();
                }

                return Json(new object[] { true, "Xóa thành công." });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, "Xóa thất bại. " + ex.Message });
            }
        }

        public ActionResult ReLoad()
        {
            ViewBag.ProductCategory = db.categories.OrderBy(x => x.Order).ToList();
            return PartialView("_LoadDataPartial", db.customer_score.OrderByDescending(x => x.UpdateAt ?? x.CreateAt).ToList());
        }

        public JsonResult saveconfig(List<string> cfgId)
        {
            try
            {
                foreach (var c in cfgId)
                {
                    var cfg = db.bonuspointconfigs.Find(c);
                    if (cfg != null)
                    {
                        cfg.Value = float.Parse(string.IsNullOrEmpty(Request[c]) ? "0" : Request[c], CultureInfo.InvariantCulture);
                        db.Entry(cfg).State = System.Data.Entity.EntityState.Modified;
                    }
                }

                if (db.ChangeTracker.HasChanges())
                {
                    db.SaveChanges();
                }

                return Json(new object[] { true, "Lưu thành công." });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, "Lưu thất bại. " + ex.Message });
            }
        }
    }
}
