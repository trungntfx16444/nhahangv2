namespace AdminPage.Controllers
{
    using System;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Web.Mvc;
    using AdminPage.AppLB;
    using AdminPage.Models.CustomizeModels;
    using AdminPage.Models;
    using AdminPage.Services;

    public class GiftCodeController : ExpiredCheckController
    {
        private AdminEntities db = new();

        // GET: Admin/GiftCode
        public ActionResult Index()
        {
            ViewBag.fromdate = Session["BonusPoints_fromdate"]?.ToString() ?? "2020-01-01";
            ViewBag.todate = Session["BonusPoints_todate"]?.ToString() ?? DateTime.Now.ToString("yyyy-MM-dd");
            return View();
        }
        public JsonResult Load_table(DataTable_request data, string search, string status, DateTime? fromdate, DateTime? todate)
        {
            // Session["parentId"] = parentTopicId = !string.IsNullOrEmpty(parentTopicId) ? parentTopicId : (Session["parentId"]?.ToString() ?? "");
            // db.setLanguage(language);

            var langDefault = SiteLang.GetDefault();
            ViewBag.ProductCategory = db.categories.Where(x => x.LangCode == langDefault.Code).OrderBy(x => x.CategoryName).ToList();
            var recordsTotal = db.gift_code.Count();
            var news = db.gift_code.SqlQuery(CommonFunc.SearchCommand("gift_code", search, "name", "code")).AsQueryable();
            // news = news.Where(n => n.active != false);

            if (!string.IsNullOrEmpty(status))
            {
                var curdate = DateTime.Now.Date;
                if (status == "1")
                {
                    news = news.Where(n => (n.start_date == null || n.start_date.Value.Date <= curdate)
                    && (n.end_date == null || n.end_date.Value.Date >= curdate));
                }
                else if (status == "0")
                {
                    news = news.Where(n => (n.start_date != null && n.start_date.Value.Date > curdate));
                }
                else if (status == "-1")
                {
                    news = news.Where(n => (n.end_date != null && n.end_date.Value.Date < curdate));
                }
            }
            if (fromdate != null)
            {
                Session["BonusPoints_fromdate"] = fromdate.Value.ToString("yyyy-MM-dd");
                news = news.Where(n => (n.start_date == null || n.start_date.Value.Date >= fromdate));
            }
            if (todate != null)
            {
                Session["BonusPoints_todate"] = todate.Value.ToString("yyyy-MM-dd");
                news = news.Where(n => (n.start_date != null && n.start_date.Value.Date <= todate));
            }

            var filtered_count = news.Count();
            string[] orderColumns = { "name", "start_date", "price", "GrandTotalFrom" };
            var orderColumn = orderColumns.ElementAtOrDefault(data.order?.FirstOrDefault()?.column ?? 1) ?? "start_date";
            var order = orderColumn + " " + data.order?.FirstOrDefault()?.dir;

            var list_news = news.OrderBy(order).Skip(data.start).Take(data.length).ToList();
            var html = CommonFunc.RenderRazorViewToString("_tableData", list_news, this);

            return Json(new { draw = data.draw, recordsFiltered = filtered_count, recordsTotal = recordsTotal, data = html });
        }
        public ActionResult Partial_list()
        {
            var model = db.gift_code.ToList();
            return PartialView("_partial_list_gift_code", model);
        }

        public JsonResult Save(gift_code Gift_Code)
        {
            try
            {
                string msg_success = string.Empty;
                var discountType = Request["gc_type"];
                if (Request["gc_type"] == "percent")
                {
                    Gift_Code.percent = Math.Min(Math.Max(Gift_Code.percent ?? 0, 0), 100);
                    Gift_Code.price = 0;
                }
                else
                {
                    Gift_Code.percent = 0;
                    Gift_Code.price = decimal.Parse(string.IsNullOrEmpty(Request["price"]) ? "0" : Request["price"].Replace(".", string.Empty));
                }
                Gift_Code.GrandTotalFrom = decimal.Parse(string.IsNullOrEmpty(Request["GrandTotalFrom"]) ? "0" : Request["GrandTotalFrom"].Replace(".", string.Empty));

                if (Request["Editing"] == "0")
                {
                    if (db.gift_code.Any(g => g.code == Gift_Code.code))
                    {
                        throw new Exception($"Mã giảm giá '{Gift_Code.code}' đã tồn tại");
                    }
                    db.gift_code.Add(Gift_Code);
                    msg_success = "Tạo mã giảm giá thành công";
                }
                else
                {
                    var edit = db.gift_code.Find(Gift_Code.code);
                    if (edit == null)
                    {
                        throw new Exception($"Mã giảm giá '{Gift_Code.code}' không tồn tại");
                    }

                    edit.name = Gift_Code.name;
                    edit.start_date = Gift_Code.start_date;
                    edit.end_date = Gift_Code.end_date;
                    edit.price = Gift_Code.price;
                    edit.percent = Gift_Code.percent;
                    edit.active = Gift_Code.active;
                    edit.GrandTotalFrom = Gift_Code.GrandTotalFrom;
                    db.Entry(edit).State = System.Data.Entity.EntityState.Modified;
                    msg_success = "Cập nhật giảm giá thành công";
                }
                db.SaveChanges();
                return Json(new object[] { true, msg_success });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }

        public JsonResult getGiftCode(string code)
        {
            try
            {
                var gc = db.gift_code.Find(code);
                if (gc == null)
                {
                    throw new Exception($"Không tìm thấy mã giảm giá '{code}'");
                }
                return Json(new object[]
                {
                    true,
                    gc,
                    gc.start_date.HasValue ? gc.start_date.Value.ToString("yyyy-MM-dd") : string.Empty,
                    gc.end_date.HasValue ? gc.end_date.Value.ToString("yyyy-MM-dd") : string.Empty,
                });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }

        public JsonResult DeleteGiftCode(string code)
        {
            try
            {
                var gc = db.gift_code.Find(code);
                if (gc == null)
                {
                    throw new Exception($"Không tìm thấy mã giảm giá '{code}'");
                }
                else if (db.orders.Any(x => x.GiftCode == code))
                {
                    throw new Exception("Vui lòng tắt kích hoạt cho mã giảm giá này thay vì xóa");
                }
                db.gift_code.Remove(gc);
                db.SaveChanges();
                return Json(new object[] { true, "Xóa thành công" });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }
    }
}