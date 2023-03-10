namespace AdminPage.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Web;
    using System.Web.Mvc;
    using Inner.Libs.Helpful;
    using Newtonsoft.Json;
    using AdminPage.AppLB;
    using AdminPage.Models.CustomizeModels;
    using AdminPage.Enums;
    using AdminPage.Models;
    using AdminPage.Models.CustomizeModels;
    using AdminPage.Models.DTO;
    using AdminPage.Services;
    using AdminPage.Utils;

    public class InventoryController : ExpiredCheckController
    {
        private AdminEntities _db = new();
        private user cMem = Authority.GetThisUser();
        PackageInfo webLicense = new PackageServices().WebPackInfo();

        protected override void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.RouteData.Values["controller"].ToString()?.ToLower();
            var action = context.RouteData.Values["action"].ToString()?.ToLower();

            if (!webLicense.Warehouse && !action.Contains("vendor"))
            {
                context.Result = new RedirectResult("/");
                return;
            }
            else
            {
                base.OnActionExecuting(context);
            }
        }

        // GET: Admin/Inventory
        #region Inventory
        public ActionResult Index()
        {
            var default_language = SiteLang.GetDefault();
            var listCate = _db.categories.Where(x => x.LangCode == default_language.Code).OrderBy(x => x.Order).ToList();
            return View(listCate);
        }

        public JsonResult Load_table(DataTable_request data, string category, string search, string stock_status)
        {
            var default_language = SiteLang.GetDefault();
            var recordsTotal = _db.products.Where(x => x.LangCode == default_language.Code && x.IsActive != false).Count();
            IQueryable<product> orderQuery;

            if (!string.IsNullOrEmpty(search))
            {
                var slqCommand = CommonFunc.SearchCommand("products", search, "Id", "ProductName");
                orderQuery = _db.products.SqlQuery(slqCommand).Where(x => (string.IsNullOrEmpty(category) || x.CategoryId == category) && x.LangCode == default_language.Code && x.IsActive != false).AsQueryable();
            }
            else
            {
                orderQuery = _db.products.Where(x => (string.IsNullOrEmpty(category) || x.CategoryId == category) && x.LangCode == default_language.Code && x.IsActive != false);
            }
            if (!string.IsNullOrEmpty(stock_status))
            {
                switch (stock_status)
                {
                    case "empty":
                        orderQuery = orderQuery.Where(o => o.Quantity == 0); break;
                    case "low":
                        orderQuery = orderQuery.Where(o => o.WarningQuantity >= o.Quantity); break;
                    case "stocking":
                        orderQuery = orderQuery.Where(o => o.WarningQuantity == null || o.WarningQuantity < o.Quantity); break;
                }
            }
            var filtered_count = orderQuery.Count();
            string[] orderColumns = { null, "ProductName", "Quantity", "WarningQuantity", null };
            var orderColumn = orderColumns[data.order?.FirstOrDefault()?.column ?? 1] ?? "Quantity";

            var listOrder = orderQuery.OrderBy($"{orderColumn} {data.order?.FirstOrDefault().dir}").Skip(data.start).Take(data.length).ToList();
            var html = CommonFunc.RenderRazorViewToString("_Inventory", listOrder, this);

            return Json(new { draw = data.draw, recordsFiltered = filtered_count, recordsTotal = recordsTotal, data = html });
        }
        #endregion

        #region ImportTickets
        public ActionResult ImportTickets()
        {
            var db = new DBLangCustom();
            ViewBag.products = new SelectList(db.products.Select(s => new { s.ReId, s.ProductName, s.Unit }), "ReId", "ProductName", "Unit").Prepend(new SelectListItem { Text = "-- Sản phẩm --", Value = string.Empty });
            ViewBag.vendors = new SelectList(_db.vendors.Select(s => new { s.Id, s.Name }), "Id", "Name").Prepend(new SelectListItem { Text = "-- Chọn nhà cung cấp --", Value = string.Empty });
            ViewBag.IsStorageDefault = _db.webconfigurations.FirstOrDefault()?.ImportIsStorageDefault;

            var listProd = db.products.Where(x => x.IsActive != false).ToList();

            // Trạng thái thanh toán
            var listStatus = new List<(string value, string text)>();
            foreach (var item in (ImportTicket_PaymentStatus[])Enum.GetValues(typeof(ImportTicket_PaymentStatus)))
            {
                listStatus.Add((item.ToString(), item.Text()));
            }

            // Trạng thái nhận hàng
            foreach (var item in (ImportTicket_ImportStatus[])Enum.GetValues(typeof(ImportTicket_ImportStatus)))
            {
                listStatus.Add((item.ToString(), item.Text() + " hàng"));
            }

            object[] arrObj = new object[]
            {
                _db.vendors.ToList(),
                listStatus,
                listProd,
            };
            ViewBag.ArrObj = arrObj;
            return View();
        }

        public JsonResult _ImportTickets(DataTable_request data, string search, string s_vendor, string s_status)
        {
            var recordsTotal = _db.import_ticket.Count();
            var tk = _db.import_ticket.Select(i => new
            {
                import_ticket = i,
                vendor = _db.vendors.FirstOrDefault(p => i.Vendor_Id == p.Id),
            });

            tk = tk.Where(d => (s_vendor == "" || d.vendor.Id == s_vendor) && (s_status == "" || d.import_ticket.PayStatus == s_status || d.import_ticket.ImportStatus == s_status));
            if (!string.IsNullOrEmpty(search))
            {
                search = "%" + CommonFunc.ConvertNonUnicodeURL(search, false).Replace("-", "%") + "%";
                tk = tk.Where(d => DbFunctions.Like(d.import_ticket.Code, search));
            }

            var import_ticket = new import_ticket();
            var filtered_count = tk.Count();
            string[] orderColumns = { "import_ticket.Code", "vendor.Name", "import_ticket.Total", "import_ticket.ImportStatus", "import_ticket.CreatedAt", null };
            var orderColumn = orderColumns[data.order?.FirstOrDefault()?.column ?? 1] ?? "CreatedAt";
            var order = orderColumn + " " + data.order?.FirstOrDefault()?.dir;
            var list_tk = tk.OrderBy(order).Skip(data.start).Take(data.length).ToList();
            var html = CommonFunc.RenderRazorViewToString("_ImportTickets", list_tk.Select(s => (s.import_ticket, s.vendor)), this);
            return Json(new { draw = data.draw, recordsFiltered = filtered_count, recordsTotal = recordsTotal, data = html });
        }

        public JsonResult ImportTicketGet(string Id)
        {
            try
            {
                var db = new DBLangCustom();
                var it = _db.import_ticket.Find(Id);

                if (it == null)
                {
                    throw new Exception("Phiếu nhập không tồn tại!");
                }
                return Json(new object[] { true, it });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message, ex.ToString() });
            }
        }

        public ActionResult _ImportTicketDetail(string Id)
        {
            try
            {
                var db = new DBLangCustom();
                var it = _db.import_ticket.Find(Id);
                if (it == null)
                {
                    throw new Exception("Phiếu nhập không tồn tại!");
                }
                var jsondata = JsonConvert.DeserializeObject<List<submit_product>>(it.ProductDetail).ToList();
                var list_prId = jsondata.Select(d => d.Product_Id).ToList();
                var products = db.products.Where(p => list_prId.Contains(p.ReId)).ToList();
                ViewBag.details = jsondata.Join(products, s => s.Product_Id, p => p.ReId, (s, p) => (s, p)).ToList();
                ViewBag.VendorName = db.vendors.Find(it.Vendor_Id).Name;
                ViewBag.ImportStatus = Ext.EnumParse<ImportTicket_ImportStatus>(it.ImportStatus).Text();
                ViewBag.PayStatus = Ext.EnumParse<ImportTicket_PaymentStatus>(it.PayStatus).Text();
                return PartialView(it);
            }
            catch (Exception ex)
            {
                return PartialView();
            }
        }

        public ActionResult _ImportTicketQuick(List<string> Ids)
        {
            var db = new DBLangCustom();
            var products = db.products.Where(p => Ids.Contains(p.ReId)).OrderBy(p => p.VendorId).ToList();
            ViewBag.vendors = new SelectList(_db.vendors.Select(s => new { s.Id, s.Name }).ToList(), "Id", "Name").Prepend(new SelectListItem { Text = "-- Chọn nhà cung cấp --", Value = string.Empty });
            return PartialView(products);
        }

        public JsonResult ImportTicketQuickSave(ICollection<submit_product> submit_products)
        {
            try
            {
                var config = _db.webconfigurations.FirstOrDefault();
                submit_products.GroupBy(s => s.Vendor_Id).ForEach(s =>
                {
                    var it = new import_ticket
                    {
                        Id = AppFunc.NewShortId(),
                        PayStatus = ImportTicket_PaymentStatus.Unpaid.ToString(),
                        ImportStatus = ImportTicket_ImportStatus.NotYetImported.ToString(),
                        CreatedAt = DateTime.Now,
                        CreatedBy = cMem.Fullname,
                        ProductDetail = JsonConvert.SerializeObject(s.ToList()),
                        Vendor_Id = s.Key,
                        Total = s.Sum(p => p.EntryPrice * p.ImportQty),
                        IsStorage = config.ImportIsStorageDefault,
                        OrderDate = DateTime.Now,
                    };
                    string monthCode = DateTime.Now.ToString("yyMM");
                    if (!int.TryParse(_db.import_ticket.Select(i => i.Code).Where(c => c.StartsWith(monthCode)).Max()?.Substring(4), out int max))
                    {
                        max = 0;
                    }

                    it.Code = monthCode + (max + 1).ToString().PadLeft(4, '0');
                    _db.import_ticket.Add(it);
                });

                _db.SaveChanges();
                TempData["s"] = "Tạo phiếu nhập hoàn tất";
                return Json(new object[] { true, "Tạo phiếu nhập hoàn tất" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message, ex.ToString() });
            }
        }

        public JsonResult ImportTicketDelete(string Id)
        {
            try
            {
                var it = _db.import_ticket.Find(Id);
                if (it == null)
                {
                    throw new Exception("Phiếu nhập không tồn tại!");
                }
                else if (it.PayStatus != ImportTicket_PaymentStatus.Unpaid.ToString())
                {
                    throw new Exception("Không thể xóa phiếu nhập đã thanh toán!");
                }
                _db.import_ticket.Remove(it);
                _db.SaveChanges();
                return Json(new object[] { true, "Đã xóa phiếu nhập" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message, ex.ToString() });
            }
        }

        public JsonResult ImportTicketSave(import_ticket it, ICollection<submit_product> submit_product, List<string> OtherPriceName, List<decimal?> OtherPriceAmount, TimeSpan? OrderTime, TimeSpan? ReceivedTime, ImportTicket_PaymentStatus? PayStatus, ImportTicket_ImportStatus ImportStatus)
        {
            try
            {
                if (it.ReceivedDate == null)
                {
                    it.OrderDate = DateTime.Now;
                }
                else if (it.OrderDate.HasValue && OrderTime.HasValue)
                {
                    it.OrderDate = it.OrderDate.Value.Add(OrderTime.Value);
                }

                if (it.ReceivedDate.HasValue && ReceivedTime.HasValue)
                {
                    it.ReceivedDate = it.ReceivedDate.Value.Add(ReceivedTime.Value);
                }
                var addnew = string.IsNullOrEmpty(it.Id);
                var tk1 = new import_ticket();
                var config = _db.webconfigurations.FirstOrDefault();

                bool old_stored = false, new_stored = ImportStatus == ImportTicket_ImportStatus.Imported && it.IsStorage == true;
                string old_product = string.Empty, new_product = string.Empty;
                if (addnew)
                {
                    string monthCode = DateTime.Now.ToString("yyMM");
                    it.Id = AppFunc.NewShortId();
                    if (!int.TryParse(_db.import_ticket.Select(i => i.Code).Where(c => c.StartsWith(monthCode)).Max()?.GetLast(4), out int max))
                    {
                        max = 0;
                    }

                    it.Code = monthCode + (max + 1).ToString().PadLeft(4, '0');
                    it.ImportStatus = ImportStatus.ToString();
                    it.CreatedAt = DateTime.Now;
                    it.CreatedBy = cMem.Fullname;
                    if (config.ImportIsStorageDefault != it.IsStorage)
                    {
                        config.ImportIsStorageDefault = it.IsStorage;
                        _db.Entry(config).State = EntityState.Modified;
                    }
                }
                else
                {
                    tk1 = _db.import_ticket.Find(it.Id);
                    if (tk1 == null)
                    {
                        throw new Exception("Không tìm thấy nhà cung phiếu nhập!");
                    }
                    old_stored = tk1.ImportStatus == ImportTicket_ImportStatus.Imported.ToString() && tk1.IsStorage == true;
                    old_product = tk1.ProductDetail;
                    if (PayStatus >= Ext.EnumParse<ImportTicket_PaymentStatus>(tk1.PayStatus))
                    {
                        it.PayStatus = PayStatus.ToString();
                    }
                    else
                    {
                        it.PayStatus = tk1.PayStatus ?? ImportTicket_PaymentStatus.Unpaid.ToString();
                    }
                    it.ImportStatus = ImportStatus.ToString();
                    it.Code = tk1.Code;
                    it.CreatedAt = tk1.CreatedAt;
                    it.CreatedBy = tk1.CreatedBy;
                }

                // other price.
                Dictionary<string, decimal> others = new();
                for (int i = 0; i < OtherPriceName.Count; i++)
                {
                    if (!string.IsNullOrEmpty(OtherPriceName[i]))
                    {
                        others.Add(OtherPriceName[i], OtherPriceAmount?.ElementAtOrDefault(i) ?? 0);
                    }
                }
                it.Other_costs = JsonConvert.SerializeObject(others);
                it.ProductDetail = JsonConvert.SerializeObject(submit_product);
                new_product = it.ProductDetail;
                _db.import_ticket.AddOrUpdate(it);
                _db.SaveChanges();
                update_product_qty(old_stored, new_stored, old_product, new_product);
                return Json(new object[] { true, addnew ? "Tạo phiếu nhập mới hoàn tất" : "cập nhật phiếu nhập hoàn tất!", config.ImportIsStorageDefault });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message, ex.ToString() });
            }

        }

        public ActionResult ImportTicketsPrint(string Id)
        {
            var db = new DBLangCustom();
            var it = _db.import_ticket.Find(Id);
            if (it == null)
            {
                throw new Exception("Phiếu nhập không tồn tại!");
            }
            var jsondata = JsonConvert.DeserializeObject<List<submit_product>>(it.ProductDetail).ToList();
            var list_prId = jsondata.Select(d => d.Product_Id).ToList();
            var products = db.products.Where(p => list_prId.Contains(p.ReId)).ToList();
            ViewBag.details = jsondata.Join(products, s => s.Product_Id, p => p.ReId, (s, p) => (s, p)).ToList();
            ViewBag.VendorName = db.vendors.Find(it.Vendor_Id).Name;
            return View(it);
        }
        
        private void update_product_qty(bool old_stored, bool new_stored, string old_product, string new_product)
        {
            List<(string _id, int _add)> ls = new();

            var oldp = JsonConvert.DeserializeObject<List<submit_product>>(old_product)?.Select(s => (_id: s.Product_Id, _add: -s.QtyExchange)).ToList() ?? new();
            var newp = JsonConvert.DeserializeObject<List<submit_product>>(new_product)?.Select(s => (_id: s.Product_Id, _add: s.QtyExchange)).ToList() ?? new();
            if (!old_stored && new_stored)
            {
                // add số lượng mới.
                ls = newp;
            }
            else if (old_stored && !new_stored)
            {
                // trừ đi số lượng cũ.
                ls = oldp;
            }
            else if (old_stored && new_stored)
            {
                // add số lượng mới và trừ đi số lượng cũ.
                oldp.AddRange(newp);
                ls = oldp.GroupBy(p => p._id).Select(g => (_id: g.Key, _add: g.Sum(p => p._add))).ToList();
            }
            else
            {
                return;
            }
            var product_id = ls.Select(i => i._id);
            _db.products.Where(p => product_id.Contains(p.ReId)).ToList().Join(ls, p => p.ReId, l => l._id, (p, l) => new { p, l }).ForEach(item =>
            {
                item.p.Quantity += item.l._add * (item.p.ValueExchange ?? 1);
                _db.Entry(item.p).State = EntityState.Modified;
            });
            _db.SaveChanges();
            return;
        }
        #endregion

        #region Vendors
        public ActionResult Vendors()
        {
            return View();
        }

        public JsonResult _Vendors(DataTable_request rq, string search)
        {
            var recordsTotal = _db.vendors.Count();
            var qData = _db.vendors.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                search = "%" + CommonFunc.ConvertNonUnicodeURL(search, false).Replace("-", "%") + "%";
                qData = qData.Where(d =>
                    DbFunctions.Like(d.Name.Replace("đ", "d").ToLower(), search)
                    || DbFunctions.Like(d.Address.Replace("đ", "d").ToLower(), search)
                    || DbFunctions.Like(d.Email.Replace("đ", "d").ToLower(), search)
                    || DbFunctions.Like(d.Phone.Replace("đ", "d").ToLower(), search));
            }
            var import_ticket = new import_ticket();
            var filtered_count = qData.Count();
            string[] orderColumns = { "CreatedAt", "Name", "Phone", "RemainingDebt" };
            var orderColumn = orderColumns[rq.order?.FirstOrDefault()?.column ?? 1] ?? "CreatedAt";
            var order = orderColumn + " " + rq.order?.FirstOrDefault()?.dir;
            var data = qData.OrderBy(order).Skip(rq.start).Take(rq.length).ToList();
            var html = CommonFunc.RenderRazorViewToString("_Vendors", data, this);
            return Json(new { draw = rq.draw, recordsFiltered = filtered_count, recordsTotal = recordsTotal, data = html });
        }

        public JsonResult VendorGet(string Id)
        {
            try
            {
                var vendor = _db.vendors.Find(Id);
                return Json(new object[] { true, vendor });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message, ex.ToString() });
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult VendorSave(vendor vendor)
        {
            try
            {
                if (string.IsNullOrEmpty(vendor.Id))
                {
                    vendor.Id = AppFunc.NewShortId();
                    vendor.Active = true;
                    vendor.CreatedAt = DateTime.Now;
                    vendor.CreatedBy = cMem.Fullname;
                }
                else
                {
                    var vendor1 = _db.vendors.FirstOrDefault(x => x.Id == vendor.Id && x.Active == true);
                    if (vendor1 == null)
                    {
                        throw new Exception("Không tìm thấy nhà cung cấp!");
                    }

                    vendor.Active = vendor1.Active;
                    vendor.CreatedAt = vendor1.CreatedAt;
                    vendor.CreatedBy = vendor1.CreatedBy;
                }
                vendor.Logo = Request.Unvalidated["textarea_Logo"];

                _db.vendors.AddOrUpdate(vendor);
                _db.SaveChanges();
                return Json(new object[] { true, "Lưu thành công" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message, ex.ToString() });
            }

        }
        #endregion
    }

    public static class StringExtension
    {
        public static string GetLast(this string source, int tail_length)
        {
            if (tail_length >= source.Length)
            {
                return source;
            }

            return source.Substring(source.Length - tail_length);
        }
    }
}