namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Web;
    using System.Web.Mvc;
    using PKWebShop.AppLB;
    using PKWebShop.Areas.Admin.CustomizeModel;
    using PKWebShop.Models;

    [Authorize]
    public class AppointmentController : ExpiredCheckController
    {
        private Dictionary<string, bool> access = Authority.GetAccessAuthority();
        public ActionResult Index()
        {
            if (!access.ContainsKey("update_apoiment"))
            {
                TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                return RedirectToAction("index", "home");
            }

            DateTime from = DateTime.Now.AddMonths(-2);
            ViewBag.From = new DateTime(from.Year, from.Month, 1).ToString("yyyy-MM-dd");
            ViewBag.To = DateTime.Now.ToString("yyyy-MM-dd");
            return View();
        }

        public JsonResult Load_table(DataTable_request data, DateTime? From, DateTime To, int? Status, string search)
        {
            From = DateTime.Parse($"{From:dd/MM/yyyy} 0:0:0");
            To = DateTime.Parse($"{To:dd/MM/yyyy} 23:59:59");

            var db = new WebShopEntities();
            var recordsTotal = db.customer_request.Count();
            IQueryable<customer_request> requestQuery;

            if (!string.IsNullOrEmpty(search))
            {
                var sqlCommand = CommonFunc.SearchCommand("customer_request", search, "FullName", "Email", "Phone");
                requestQuery = db.customer_request.SqlQuery(sqlCommand).Where(x => (Status == null || x.Status == Status) && x.FromDate >= From && x.FromDate <= To).AsQueryable();
            }
            else
            {
                requestQuery = db.customer_request.Where(x => (Status == null || x.Status == Status) && x.FromDate >= From && x.FromDate <= To).AsQueryable();
            }

            var filtered_count = requestQuery.Count();
            string[] orderColumns = { "FromDate", "FullName", null, "Status", null };
            var orderColumn = orderColumns[data.order?.FirstOrDefault()?.column ?? 1] ?? "FromDate";

            var list_request = requestQuery.OrderBy(orderColumn + " " + data.order?.FirstOrDefault().dir).Skip(data.start).Take(data.length).ToList();
            var html = CommonFunc.RenderRazorViewToString("_tableData", list_request, this);
            return Json(new { draw = data.draw, recordsFiltered = filtered_count, recordsTotal = recordsTotal, data = html });
        }

        public JsonResult GetData(string id)
        {
            try
            {
                WebShopEntities db = new ();
                var request = db.customer_request.Where(x => x.Id == id).ToList().Select(cus => new
                {
                    Id = cus.Id,
                    FullName = cus.FullName,
                    Email = cus.Email,
                    Phone = cus.Phone,
                    FromDate = cus.FromDate?.ToString("yyyy/MM/dd"),
                    ToDate = cus.ToDate?.ToString("yyyy/MM/dd"),
                    AdultsAmount = cus.AdultsAmount,
                    ChildrenAmount = cus.ChildrenAmount,
                    Note = cus.Note,
                    Status = cus.Status,
                    CreateBy = cus.CreateBy,
                    CreateAt = cus.CreateAt,
                    UpdateBy = cus.UpdateBy,
                    UpdateAt = cus.UpdateAt,
                }).FirstOrDefault();
                if (request == null)
                {
                    throw new Exception("Không tìm thấy đăng ký hỗ trợ");
                }

                return Json(new { status = true, data = request }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult Save([System.Web.Http.FromBody] customer_request rq)
        {
            try
            {
                if (string.IsNullOrEmpty(rq.FullName))
                {
                    throw new Exception("Họ tên không được để trống.");
                }
                else if (string.IsNullOrEmpty(rq.Phone))
                {
                    throw new Exception("Số điện thoại không được để trống.");
                }

                WebShopEntities db = new ();
                var request = db.customer_request.Where(x => x.Id == rq.Id).FirstOrDefault();
                if (request == null)
                {
                    throw new Exception("Không tìm thấy đăng ký hỗ trợ");
                }

                request.FullName = rq.FullName;
                request.Phone = rq.Phone;
                request.Email = rq.Email;
                request.FromDate = rq.FromDate;
                request.ToDate = rq.ToDate;
                request.ChildrenAmount = rq.ChildrenAmount;
                request.AdultsAmount = rq.AdultsAmount;
                request.Note = rq.Note;
                request.Status = rq.Status;
                request.UpdateAt = DateTime.Now;
                request.UpdateBy = User.Identity.Name;
                db.Entry(request).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                //TempData["success"] = "Lưu thành công!";
                return Json(new object[] { true, "Lưu thành công!" });
            }
            catch (Exception ex)
            {
                //TempData["error"] = "Error: " + ex.Message;
                return Json(new object[] { false, "Error: " + ex.Message });
            }
        }

        public JsonResult Delete(string id)
        {
            try
            {
                WebShopEntities db = new ();
                var request = db.customer_request.Where(x => x.Id == id).FirstOrDefault();
                if (request == null)
                {
                    throw new Exception("Không tìm thấy đăng ký hỗ trợ");
                }

                db.customer_request.Remove(request);
                db.SaveChanges();
                TempData["success"] = "Xóa thành công!";
                return Json(new { status = true, message = "Xóa thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
    }
}