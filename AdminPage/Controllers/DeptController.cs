namespace AdminPage.Controllers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Web.Mvc;
    using AdminPage.AppLB;
    using AdminPage.Models.CustomizeModels;
    using AdminPage.Services;
    using AdminPage.Models;
    using AdminPage.Utils;
    using ExpenseService = Services.ExpenseService;

    public class DeptController : ExpiredCheckController
    {
        private ReceiptService _receipts = new ReceiptService();
        public ActionResult Receipt()
        {
            DateTime from = DateTime.Now.AddMonths(-3);
            ViewBag.From = new DateTime(from.Year, 1, 1).ToString("yyyy-MM-dd");
            ViewBag.To = DateTime.Now.ToString("yyyy-MM-dd");
            return View();
        }

        public ActionResult ReceiptList(DataTable_request data, string cond, DateTime? from = null, DateTime? to = null)
        {
            var rs = _receipts.Get(data, cond, out int[] dt, from, to);
            return Json(new
            {
                draw = dt[0],
                recordsFiltered = dt[1],
                recordsTotal = dt[2],
                data = AppFunc.RenderViewToString(ControllerContext, "Receipt/List", rs, true),
                report = _receipts.ReportInfo(rs, cond, from, to),
            });
        }
        public ActionResult _Detail(string Id, string Type)
        {
            
            AdminEntities db = new AdminEntities();
            var expense = db.receipts.FirstOrDefault(x => x.Id == Id);
            ViewBag.Type = Type;
            return PartialView("_Detail", expense);
        }
        public ActionResult ReceiptSave(receipt item)
        {
            try
            {
                _receipts.Save(item);
                return Json(new object[] { true, string.IsNullOrEmpty(item.Id) ? "Đã tạo phiếu thu hòan tất" : "Đã cập nhật phiếu thu" });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
          
        }
        public JsonResult DelReceipt(string Id)
        {
            try
            {
                AdminEntities db = new AdminEntities();
                var Receipt = db.receipts.FirstOrDefault(x => x.Id == Id);
                if (Receipt == null)
                {
                    throw new Exception("Không tìm thấy phiếu thu");
                }
                db.receipts.Remove(Receipt);
                db.SaveChanges();
                return Json(new object[] { true, "Xóa phiếu thu thành công." });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message?.Replace("\r\n", string.Empty) });
            }
        }

        #region EXPENSES
        public ActionResult Expense(string search = "")
        {
            DateTime from = DateTime.Now.AddMonths(-3);
            ViewBag.From = new DateTime(from.Year, 1, 1).ToString("yyyy-MM-dd");
            ViewBag.To = DateTime.Now.ToString("yyyy-MM-dd");
            ViewBag.Search = search;

            AdminEntities db = new AdminEntities();
            ViewBag.vendors = db.vendors.ToList();

            // report
            var importTicketTotal = db.import_ticket.Where(x => x.ImportStatus == "Imported").Sum(x => x.Total) ?? 0;
            var expenseTotal = db.expenses.Where(x => x.Status == "1").Sum(x => x.Total) ?? 0;
            ViewBag.ImportTicketTotal = importTicketTotal;
            ViewBag.ExpenseTotal = expenseTotal;

            return View();
        }

        public JsonResult Load_table(DataTable_request data, DateTime? From, DateTime To, string search, string s_vendor, string s_status)
        {
            From = DateTime.Parse($"{From:dd/MM/yyyy} 0:0:0");
            To = DateTime.Parse($"{To:dd/MM/yyyy} 23:59:59");

            var listExpense = new ExpenseService().LoadExpense(data, From, To, search, s_vendor, s_status,  out int[] dt, out string err_msg);
            var html = CommonFunc.RenderRazorViewToString("_ExpenseDataTable", listExpense, this);
            return Json(new { draw = dt[0], recordsFiltered = dt[1], recordsTotal = dt[2], data = html });
        }

        public JsonResult GetExpense(string id)
        {
            try
            {
                AdminEntities db = new AdminEntities();
                var expense = db.expenses.FirstOrDefault(x => x.Id == id);
                if (expense == null)
                {
                    throw new Exception("Không tìm thấy chi phí");
                }
                return Json(new object[] { true, expense });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message?.Replace("\r\n", string.Empty) });
            }
        }

        public JsonResult SaveExpense(expens data)
        {
            try
            {
                if (string.IsNullOrEmpty(data.Title))
                {
                    throw new Exception("Vui lòng nhập tiêu đề phiếu chi");
                }
                var rs = new ExpenseService().SaveExpense(data, Request["Total"], Request["expenseName"], Request["expensePrice"]);
                return Json(new object[] { true, "Lưu thành công." });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message.Replace("\r\n", string.Empty) });
            }
        }

        public JsonResult DelExpense(string id)
        {
            try
            {
                AdminEntities db = new AdminEntities();
                var expense = db.expenses.FirstOrDefault(x => x.Id == id);
                if (expense == null)
                {
                    throw new Exception("Không tìm thấy chi phí");
                }
                db.expenses.Remove(expense);
                db.SaveChanges();
                return Json(new object[] { true, "Xóa phiếu chi thành công." });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message?.Replace("\r\n", string.Empty) });
            }
        }

        public ActionResult _ExpenseDetail(string Id)
        {
            try
            {
                AdminEntities db = new AdminEntities();
                var exp = db.expenses.FirstOrDefault(x => x.Id == Id);
                if (exp == null)
                {
                    throw new Exception("Phiếu chi không tồn tại");
                }
                return PartialView(exp);
            }
            catch (Exception ex)
            {
                return PartialView();
            }
        }

        public ActionResult ExpensePrint(string Id)
        {
            AdminEntities db = new AdminEntities();
            var exp = db.expenses.FirstOrDefault(x => x.Id == Id);
            if (exp == null)
            {
                throw new Exception("Phiếu chi không tồn tại");
            }
            return View(exp);
        }
        #endregion

    }
}