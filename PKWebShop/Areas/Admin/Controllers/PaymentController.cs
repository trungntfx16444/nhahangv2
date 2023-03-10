namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Web.Mvc;
    using Inner.Libs.Helpful;
    using PKWebShop.AppLB;
    using PKWebShop.Areas.Admin.CustomizeModel;
    using PKWebShop.Models;

    public class PaymentController : ExpiredCheckController
    {
        // GET: Admin/OrderMan
        private WebShopEntities db = new();

        // GET
        public ActionResult Index()
        {
            DateTime from = DateTime.Now.AddMonths(-3);
            ViewBag.From = new DateTime(from.Year, 1, 1).ToString("yyyy-MM-dd");
            ViewBag.To = DateTime.Now.ToString("yyyy-MM-dd");

            return View();
        }

        public ActionResult PaymentConfigUpdate(webconfiguration info)
        {
            try
            {
                var wi = db.webconfigurations.Find(info.Id);
                if (wi != null)
                {
                    wi.vnp_TmnCode = info.vnp_TmnCode;
                    wi.vnp_HashSecret = info.vnp_HashSecret;
                    wi.vnp_Version = info.vnp_Version;
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

        public JsonResult PaymentList(DataTable_request data, DateTime? From, DateTime To, string search)
        {
            From = DateTime.Parse($"{From:dd/MM/yyyy} 00:00:00");
            To = DateTime.Parse($"{To:dd/MM/yyyy} 23:59:59");

            var recordsTotal = db.VNP_PaymentData.Count();
            IQueryable<order> orderQuery;

            if (!string.IsNullOrEmpty(search))
            {
                var sqlCommand = CommonFunc.SearchCommand("orders", search, "Id", "CustomerName", "CustomerPhone");
                orderQuery = db.orders.SqlQuery(sqlCommand).Where(x => x.CreatedAt >= From && x.CreatedAt <= To).AsQueryable();
            }
            else
            {
                orderQuery = db.orders.Where(x => x.CreatedAt >= From && x.CreatedAt <= To);
            }
      
            var recordsFiltered = orderQuery.Count();
            string[] orderColumns = { null, "CustomerName", "GrandTotal", "CreatedAt", "Status", null };
            var orderColumn = orderColumns[data.order?.FirstOrDefault()?.column ?? 1] ?? "CreatedAt";
            var listOrder = orderQuery.OrderBy($"{orderColumn} {data.order?.FirstOrDefault().dir}").Skip(data.start).Take(data.length).ToList();
            var orderId = listOrder.Select(or => or.Id).ToList();

            var paymentList = db.VNP_PaymentData.Where(pay => orderId.Contains(pay.OrderId)).OrderBy(pay => pay.UpdatedAt).ToList();
            var html = CommonFunc.RenderRazorViewToString("_DataTable", paymentList, this);
            return Json(new { data.draw, recordsFiltered, recordsTotal, data = html });
        }
    }
}