namespace AdminPage.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Web.Mvc;
    using Newtonsoft.Json;
    using AdminPage.AppLB;
    using AdminPage.Models.CustomizeModels;
    using AdminPage.DataAsset;
    using AdminPage.Models;
    using AdminPage.Services;
    using Inner.Libs.Helpful;
    using AdminPage.Utils;

    [Authorize]
    public class OrderManController : ExpiredCheckController
    {
        // GET: Admin/OrderMan
        private AdminEntities db = new();
        private Dictionary<string, bool> access = Authority.GetAccessAuthority();
        private const string SessionOrderDetail = "ORDER_DETAIL";
        private const string SessionOrderDetail_discountPercent = "SessionOrderDetail_discountPercent";
        private const string SessionOrderDetail_discountAmount = "SessionOrderDetail_discountAmount";

        public ActionResult Index()
        {
            if (!access.ContainsKey("update_order"))
            {
                TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                return RedirectToAction("index", "home");
            }

            DateTime from = DateTime.Now.AddMonths(-3);
            ViewBag.From = new DateTime(from.Year, 1, 1).ToString("yyyy-MM-dd");
            ViewBag.To = DateTime.Now.ToString("yyyy-MM-dd");
            ViewBag.CurrentRole = Authority.GetThisUser(false)?.RoleLevel;
            ViewBag.new_orders = db.orders?.Count(x => x.Status == "0" || x.Status == "1");
            ViewBag.total_orders = db.orders?.Count();
            ViewBag.new_sales = db.orders?.Where(x => x.Status == "0" || x.Status == "1").Select(x => ((double?)x.GrandTotal)).Sum() ?? 0;
            ViewBag.total_sales = db.orders?.Sum(x => x.GrandTotal) ?? 0;
            return View();
        }

        #region ORDER
        public JsonResult Load_table(DataTable_request data, DateTime? From, DateTime? To, string search, string Status, string Paytype)
        {
            From = DateTime.Parse($"{From:dd/MM/yyyy} 0:0:0");
            To = DateTime.Parse($"{To:dd/MM/yyyy} 23:59:59");
            Session["OrderFilter_Status"] = Status;
            Session["OrderFilter_Paytype"] = Paytype;
            var recordsTotal = db.orders.Count();
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

            if (!string.IsNullOrEmpty(Status))
            {
                orderQuery = orderQuery.Where(o => o.Status == Status);
            }
            if (!string.IsNullOrEmpty(Paytype))
            {
                orderQuery = orderQuery.Where(o => o.PaymentMethod == Paytype);
            }

            var filtered_count = orderQuery.Count();
            string[] orderColumns = { null, "CustomerName", "GrandTotal", "CreatedAt", "Status", null };
            var orderColumn = orderColumns[data.order?.FirstOrDefault()?.column ?? 1] ?? "CreatedAt";

            var listOrder = orderQuery.OrderBy($"{orderColumn} {data.order?.FirstOrDefault().dir}").Skip(data.start).Take(data.length).ToList();
            listOrder.ForEach(order =>
            {
                order.PaymentData = db.vnp_paymentdata.Where(pay => pay.OrderId == order.Id).ToList().Select(pay => new PaymentData(pay)).FirstOrDefault() ?? new PaymentData();
            });
            var list_cusId = listOrder.Select(x => x.CustomerId).ToList();
            ViewBag.ListPartnerId = db.customers.Where(x => list_cusId.Any(id => id == x.Id) && x.Role == "Partner").Select(x => x.Id);
            var html = CommonFunc.RenderRazorViewToString("_OrderDataTable", listOrder, this);

            // report

            return Json(new { draw = data.draw, recordsFiltered = filtered_count, recordsTotal = recordsTotal, data = html });
        }

        public ActionResult Save(string id)
        {
            if (!access.ContainsKey("update_order"))
            {
                TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin";
                return RedirectToAction("index", "home");
            }

            try
            {
                var default_language = SiteLang.GetDefault();
                var listCate = db.categories.Where(x => x.LangCode == default_language.Code).OrderBy(x => x.Order).ToList();
                ViewBag.ListCate = listCate;

                var order_ = new order() { Status = "0" };
                var listProductItem = new List<orders_detail>();
                var listProp = new List<product_properties>();
                ViewBag.Title = "Tạo đơn hàng";

                if (!string.IsNullOrEmpty(id))
                {
                    order_ = db.orders.Find(id);
                    if (order_ == null)
                    {
                        throw new Exception("Không tìm thấy đơn hàng");
                    }
                    var cus = db.customers.Find(order_.CustomerId);
                    ViewBag.shippingAddress = cus.Address + cus.Ward + cus.District + cus.Province != order_.ShippingAddress + order_.Shipping_Ward + order_.Shipping_District + order_.Shipping_Province;
                    ViewBag.Title = $"Cập nhật đơn hàng #{order_.Id}";

                    var listDetail = db.orders_detail.Where(x => x.OrderId == order_.Id).ToList();
                    if (listDetail != null && listDetail.Count > 0)
                    {
                        listProductItem = listDetail;

                        string listParentProp = string.Join(",", listProductItem.GroupBy(x => x.ParentPropertiesId).Select(x => x.Key).Distinct());
                        string listChildProp = string.Join(",", listProductItem.GroupBy(x => x.PropertiesId).Select(x => x.Key).Distinct());
                        listProp = (from x in db.product_properties
                                    where (listParentProp.Contains(x.ReId) && x.LangCode == default_language.Code)
                                    || (listChildProp.Contains(x.ReId) && x.LangCode == default_language.Code)
                                    orderby x.name
                                    select x).ToList();
                    }

                    // GIFT CODE
                    var giftCode = db.gift_code.Where(x => x.code == order_.GiftCode).FirstOrDefault();
                    ViewBag.GiftCode = giftCode;
                    if ((order_.DiscountAmount == null || order_.DiscountAmount == 0) && giftCode != null)
                    {
                        if (giftCode.percent != null && giftCode.percent > 0)
                        {
                            order_.DiscountAmount = order_.SubTotal * (giftCode.percent / 100);
                        }
                        else if (giftCode.price != null && giftCode.price > 0)
                        {
                            order_.DiscountAmount = (double)giftCode.price;
                        }
                    }
                }

                ViewBag.ListProductItem = listProductItem;
                ViewBag.ListStatus = UserContent.OrderStatus();
                ViewBag.ListProp = listProp;
                ViewBag.FreeShip = db.shipping_config.FirstOrDefault(s => s.Active && s.Type == "free")?.ShippingFee ?? 0;
                Session[SessionOrderDetail_discountPercent] = order_.DiscountPercent;
                Session[SessionOrderDetail_discountAmount] = order_.DiscountAmount;
                Session[SessionOrderDetail] = listProductItem;
                return View(order_);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("index");
            }
        }

        [HttpPost]
        public ActionResult SaveOrder(order data)
        {
            if (!access.ContainsKey("update_order"))
            {
                TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                return RedirectToAction("index", "home");
            }

            using (var tranS = db.Database.BeginTransaction())
            {
                try
                {
                    var curMem = Authority.GetThisUser();
                    var cus = db.customers.Find(data.CustomerId);
                    if (cus == null)
                    {
                        throw new Exception("Vui lòng chọn khách hàng");
                    }

                    bool check_ship = string.IsNullOrEmpty(Request["check-ship"]) ? true : false;
                    if (check_ship == false && (string.IsNullOrEmpty(data.ShippingAddress) || string.IsNullOrEmpty(data.Shipping_Ward) || string.IsNullOrEmpty(data.Shipping_District) || string.IsNullOrEmpty(data.Shipping_Province)))
                    {
                        throw new Exception("Vui lòng chọn địa chỉ giao hàng");
                    }

                    var listProductItem = new List<orders_detail>();
                    if (Session[SessionOrderDetail] != null)
                    {
                        listProductItem = Session[SessionOrderDetail] as List<orders_detail>;
                    }

                    if (listProductItem == null || listProductItem.Count() == 0)
                    {
                        throw new Exception("Vui lòng chọn sản phẩm cho đơn hàng");
                    }

                    #region UPDATE ADDRESS CHO CUSTOMER
                    cus.Address = string.IsNullOrEmpty(cus.Address) ? data.ShippingAddress : cus.Address;
                    cus.Ward = string.IsNullOrEmpty(cus.Ward) ? data.Shipping_Ward : cus.Ward;
                    cus.District = string.IsNullOrEmpty(cus.District) ? data.Shipping_District : cus.District;
                    cus.Province = string.IsNullOrEmpty(cus.Province) ? data.Shipping_Province : cus.Province;
                    db.Entry(cus).State = System.Data.Entity.EntityState.Modified;
                    #endregion

                    string msg_success = string.Empty;
                    var freeShip = double.Parse(string.IsNullOrEmpty(Request["FreeShip"]) ? "0" : Request["FreeShip"].ToString());
                    var shippingFee = double.Parse(string.IsNullOrEmpty(Request["Shipping"]) ? "0" : Request["Shipping"].ToString().Replace(".", string.Empty));
                    if (Request["discount_type"]?.ToString() == "%")
                    {
                        data.DiscountPercent = double.Parse(string.IsNullOrEmpty(Request["DiscountAmount"]) ? "0" : Request["DiscountAmount"].ToString());
                        data.DiscountAmount = null;
                    }
                    else
                    {
                        data.DiscountAmount = double.Parse(string.IsNullOrEmpty(Request["DiscountAmount"]) ? "0" : Request["DiscountAmount"].ToString().Replace(".", string.Empty));
                        data.DiscountPercent = null;
                    }
                    if (string.IsNullOrEmpty(data.Id))
                    {
                        #region CREATE ORDER
                        var newOrder = new order()
                        {
                            Id = AppFunc.NewShortId(),
                            CustomerId = cus.Id,
                            CustomerName = cus.FullName,
                            CustomerPhone = cus.Phone,
                            CustomerEmail = cus.Email,
                            Customer_Address = cus.Address,
                            Customer_Ward = cus.Ward,
                            Customer_District = cus.District,
                            Customer_Province = cus.Province,
                            Status = data.Status,
                            PaymentMethod = data.PaymentMethod,
                            ShippingAddress = data.ShippingAddress,
                            Shipping_Province = data.Shipping_Province,
                            Shipping_District = data.Shipping_District,
                            Shipping_Ward = data.Shipping_Ward,
                            Shipping = shippingFee,
                            DiscountAmount = data.DiscountAmount,
                            DiscountPercent = data.DiscountPercent,
                            Customer_Comment = Request.Unvalidated["Customer_Comment"],
                            SubTotal = 0,
                            GrandTotal = 0,
                            CreatedAt = DateTime.Now,
                            CreateBy = curMem.UserName,
                        };

                        if (check_ship == true)
                        {
                            newOrder.ShippingAddress = cus.Address;
                            newOrder.Shipping_Province = cus.Province;
                            newOrder.Shipping_District = cus.District;
                            newOrder.Shipping_Ward = cus.Ward;
                        }

                        // cập nhật sản phẩm trong chi tiết đơn hàng
                        var listid = listProductItem.Select(d => d.ProductId).Distinct();
                        var products = db.products.Where(p => listid.Contains(p.ReId)).ToList();
                        foreach (var item in listProductItem)
                        {
                            item.OrderId = newOrder.Id;
                            item.CreateAt = DateTime.Now;
                            db.orders_detail.Add(item);
                            products.Where(p => p.ReId == item.ProductId).ToList().ForEach(p =>
                            {
                                if (p.Quantity == 0)
                                {
                                    throw new Exception($"Số lượng tồn kho của sản phẩm {p.ProductName} không đủ");
                                }
                                if (p.Quantity > 0)
                                {
                                    p.Quantity -= item.Quantity ?? 1;
                                    if (p.Quantity <= 0)
                                    {
                                        throw new Exception($"Số lượng tồn kho của sản phẩm {p.ProductName} không đủ");
                                    }
                                    db.Entry(p).State = System.Data.Entity.EntityState.Modified;
                                }
                                db.Entry(p).State = System.Data.Entity.EntityState.Modified;
                            });
                        }



                        newOrder.SubTotal = listProductItem.Sum(x => x.Quantity * x.Price);
                        newOrder.GrandTotal = newOrder.SubTotal - (newOrder.SubTotal * (newOrder.DiscountPercent ?? 0) / 100) - (newOrder.DiscountAmount ?? 0);
                        if (freeShip <= 0 || newOrder.GrandTotal < freeShip)
                        {
                            newOrder.GrandTotal += newOrder.Shipping;
                        }

                        newOrder.BonusPointsGet = new DA_Order().BonusPonts_calculation(newOrder, out List<string> bonusIds, listProductItem);
                        newOrder.BonusList = JsonConvert.SerializeObject(bonusIds);

                        if ((newOrder.Status == "3") != (newOrder.BonusPointsIsGot == true))
                        {
                            newOrder.BonusPointsIsGot = !(newOrder.BonusPointsIsGot == true);
                            if (newOrder.BonusPointsIsGot == true)
                            {
                                cus.BonusPoints += newOrder.BonusPointsGet;
                            }
                            else
                            {
                                cus.BonusPoints -= newOrder.BonusPointsGet;
                            }

                            db.Entry(cus).State = System.Data.Entity.EntityState.Modified;
                        }

                        if (newOrder.Status == "3")
                        {
                            string langCode_Default = SiteLang.GetDefault().Code;
                            foreach (var item in db.products.Where(x => x.LangCode == langCode_Default).ToList())
                            {
                                var prod_items = listProductItem.Where(x => x.ProductId == item.ReId).ToList();
                                if (prod_items != null && prod_items.Count > 0)
                                {
                                    item.Sold = (item.Sold ?? 0) + prod_items.Sum(x => x.Quantity);
                                    db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                                }
                            }
                        }

                        db.orders.Add(newOrder);
                        #endregion
                        msg_success = "Tạo đơn hàng thành công";
                    }
                    else
                    {
                        var orderExist = db.orders.Find(data.Id);
                        if (orderExist == null)
                        {
                            throw new Exception("Không tìm thấy đơn hàng");
                        }

                        orderExist.CustomerId = cus?.Id;
                        orderExist.CustomerName = cus?.FullName;
                        orderExist.CustomerPhone = cus?.Phone;
                        orderExist.CustomerEmail = cus?.Email;
                        orderExist.Customer_Address = cus?.Address;
                        var old_status = orderExist.Status; var new_status = data.Status;
                        orderExist.Status = data.Status;
                        if (data.Status == "2" || data.Status == "3")
                        {
                            new ReceiptService(db).NewFromOrder(orderExist.Id);
                        }
                        orderExist.PaymentMethod = data.PaymentMethod;
                        orderExist.Shipping = shippingFee;
                        orderExist.DiscountAmount = data.DiscountAmount;
                        orderExist.DiscountPercent = data.DiscountPercent;
                        orderExist.Customer_Comment = Request.Unvalidated["Customer_Comment"];
                        orderExist.UpdateAt = DateTime.Now;
                        orderExist.UpdateBy = curMem.UserName;

                        if (check_ship == true)
                        {
                            orderExist.ShippingAddress = cus.Address;
                            orderExist.Shipping_Province = cus.Province;
                            orderExist.Shipping_District = cus.District;
                            orderExist.Shipping_Ward = cus.Ward;
                        }
                        else
                        {
                            orderExist.ShippingAddress = data.ShippingAddress;
                            orderExist.Shipping_Province = data.Shipping_Province;
                            orderExist.Shipping_District = data.Shipping_District;
                            orderExist.Shipping_Ward = data.Shipping_Ward;
                        }

                        // cập nhật sản phẩm trong chi tiết đơn hàng
                        var lstOrderDetail_Database = db.orders_detail.Where(x => x.OrderId == orderExist.Id).ToList();
                        List<(string id, int old, int now)> qtys = new();
                        foreach (var item in lstOrderDetail_Database)
                        {
                            item.OldQuantity = item.Quantity ?? 0;
                            if (!listProductItem.Any(x => x.Id == item.Id))
                            {
                                item.Quantity = 0;
                                db.orders_detail.Remove(item);
                            }
                            else if (listProductItem.Any(x => x.Id == item.Id))
                            {
                                var proItem = listProductItem.Where(x => x.Id == item.Id).FirstOrDefault();
                                item.ProductName = proItem.ProductName;
                                item.Quantity = proItem.Quantity;
                                item.Price = proItem.Price;
                                item.ParentPropertiesId = proItem.ParentPropertiesId;
                                item.PropertiesId = proItem.PropertiesId;
                                item.PriceOptId = proItem.PriceOptId;
                                db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                            }
                        }
                        //thêm sản phẩm mới vào order
                        var new_details = listProductItem.Where(item => !lstOrderDetail_Database.Any(x => x.Id == item.Id));
                        foreach (var item in new_details)
                        {
                            item.OrderId = orderExist.Id;
                            item.CreateAt = DateTime.Now;
                            db.orders_detail.Add(item);
                        }
                        lstOrderDetail_Database.AddRange(new_details);
                        //Cập nhật lại số lượng trong kho
                        foreach (var p in lstOrderDetail_Database)
                        {
                            var option = db.product_option_value.FirstOrDefault(o => o.productId == p.ProductId && o.option1 == p.PriceOptId1 && o.option2 == p.PriceOptId2);
                            if (option == null)
                            {
                                continue;
                            }
                            if (old_status != "-1")
                            {
                                option.qty += p.OldQuantity;
                            }
                            if (new_status != "-1")
                            {
                                option.qty -= p.Quantity ?? 0;
                            }
                            if (option.qty < 0)
                            {
                                throw new Exception("Số lượng sản phẩm " + p.ProductName + " chỉ còn " + (option.qty + p.Quantity ?? 0));
                            }
                        }


                        orderExist.SubTotal = lstOrderDetail_Database.Sum(x => x.Quantity * x.Price);
                        orderExist.GrandTotal = (orderExist.SubTotal ?? 0) - (orderExist.SubTotal * (orderExist.DiscountPercent ?? 0) / 100) - (orderExist.DiscountAmount ?? 0) - (double)(orderExist.BonusPointsDiscount ?? 0);
                        if (freeShip <= 0 || orderExist.GrandTotal < freeShip)
                        {
                            orderExist.GrandTotal += orderExist.Shipping;
                        }

                        if ((orderExist.Status == "3") != (orderExist.BonusPointsIsGot == true))
                        {
                            orderExist.BonusPointsIsGot = !(orderExist.BonusPointsIsGot == true);
                            if (orderExist.BonusPointsIsGot == true)
                            {
                                cus.BonusPoints += orderExist.BonusPointsGet;
                            }
                            else
                            {
                                cus.BonusPoints -= orderExist.BonusPointsGet;
                            }

                            db.Entry(cus).State = System.Data.Entity.EntityState.Modified;
                        }

                        if (orderExist.Status == "3")
                        {
                            string langCode_Default = SiteLang.GetDefault().Code;
                            foreach (var item in db.products.Where(x => x.LangCode == langCode_Default).ToList())
                            {
                                var prod_items = lstOrderDetail_Database.Where(x => x.ProductId == item.ReId).ToList();
                                if (prod_items != null && prod_items.Count > 0)
                                {
                                    item.Sold = (item.Sold ?? 0) + prod_items.Sum(x => x.Quantity);
                                    db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                                }
                            }
                        }

                        db.Entry(orderExist).State = System.Data.Entity.EntityState.Modified;
                        msg_success = "Cập nhật đơn hàng thành công";
                    }

                    db.SaveChanges();
                    tranS.Commit();

                    TempData["success"] = msg_success;
                    Session.Remove(SessionOrderDetail);
                    return RedirectToAction("index");
                }
                catch (Exception ex)
                {
                    tranS.Dispose();
                    TempData["error"] = ex.Message;
                    return RedirectToAction("save", new { id = data.Id });
                }
            }
        }
        public ActionResult ChangeStatus(string id, string status)
        {
            if (!access.ContainsKey("update_order"))
            {
                TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                return RedirectToAction("index", "home");
            }

            using (var tranS = db.Database.BeginTransaction())
            {
                try
                {
                    var curMem = Authority.GetThisUser();

                    string msg_success = string.Empty;

                    var orderExist = db.orders.Find(id);
                    var listProductItem = db.orders_detail.Where(d => d.OrderId == id).ToList();
                    if (orderExist == null)
                    {
                        throw new Exception("Không tìm thấy đơn hàng");
                    }


                    var old_status = orderExist.Status; var new_status = status;
                    orderExist.Status = status;
                    if (status == "2" || status == "3")
                    {
                        new ReceiptService(db).NewFromOrder(orderExist.Id);
                    }
                    orderExist.UpdateAt = DateTime.Now;
                    orderExist.UpdateBy = curMem.UserName;

                    // cập nhật sản phẩm trong chi tiết đơn hàng
                    var lstOrderDetail_Database = db.orders_detail.Where(x => x.OrderId == orderExist.Id).ToList();
                    List<(string id, int old, int now)> qtys = lstOrderDetail_Database.Select(x => (x.Id, x.Quantity ?? 0, x.Quantity ?? 0)).ToList();
                    var listid = lstOrderDetail_Database.Select(x => x.ProductId).ToList();
                    var products = db.products.Where(p => listid.Contains(p.ReId)).ToList();
                    foreach (var p in lstOrderDetail_Database)
                    {
                        var option = db.product_option_value.FirstOrDefault(o => o.productId == p.ProductId && o.option1 == p.PriceOptId1 && o.option2 == p.PriceOptId2);
                        if (option == null)
                        {
                            continue;
                        }
                        if (old_status != "-1")
                        {
                            option.qty += p.Quantity ?? 0;
                        }
                        if (new_status != "-1")
                        {
                            option.qty -= p.Quantity ?? 0;
                        }
                        if (option.qty < 0)
                        {
                            throw new Exception("Số lượng sản phẩm " + p.ProductName + " chỉ còn " + (option.qty + p.Quantity ?? 0));
                        }
                    }

                    if ((orderExist.Status == "3") != (orderExist.BonusPointsIsGot == true))
                    {
                        var cus = db.customers.Find(orderExist.CustomerId);
                        orderExist.BonusPointsIsGot = !(orderExist.BonusPointsIsGot == true);
                        if (orderExist.BonusPointsIsGot == true)
                        {
                            cus.BonusPoints += orderExist.BonusPointsGet;
                        }
                        else
                        {
                            cus.BonusPoints -= orderExist.BonusPointsGet;
                        }

                        db.Entry(cus).State = System.Data.Entity.EntityState.Modified;
                    }

                    if (orderExist.Status == "3")
                    {
                        string langCode_Default = SiteLang.GetDefault().Code;

                        foreach (var item in db.products.Where(x => x.LangCode == langCode_Default).ToList())
                        {
                            var prod_items = listProductItem.Where(x => x.ProductId == item.ReId).ToList();
                            if (prod_items != null && prod_items.Count > 0)
                            {
                                item.Sold = (item.Sold ?? 0) + prod_items.Sum(x => x.Quantity);
                                db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                            }
                        }
                    }

                    db.Entry(orderExist).State = System.Data.Entity.EntityState.Modified;
                    msg_success = "Cập nhật trạng thái thành công";


                    db.SaveChanges();
                    tranS.Commit();

                    TempData["success"] = msg_success;
                    Session.Remove(SessionOrderDetail);
                    return RedirectToAction("ViewOrder", new { id });
                }
                catch (Exception ex)
                {
                    tranS.Dispose();
                    TempData["error"] = ex.Message;
                    return RedirectToAction("ViewOrder", new { id });
                }
            }
        }

        public ActionResult DeleteOrder(string Id)
        {
            if (!access.ContainsKey("update_order"))
            {
                TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                return RedirectToAction("index", "home");
            }

            using (var trans = db.Database.BeginTransaction())
            {
                try
                {
                    var order_ = db.orders.Find(Id);
                    if (order_ != null)
                    {
                        var lstProductItem = db.orders_detail.Where(x => x.OrderId == order_.Id).ToList();
                        if (lstProductItem != null && lstProductItem.Count() > 0)
                        {
                            db.orders_detail.RemoveRange(lstProductItem);
                        }
                        db.orders.Remove(order_);
                        db.SaveChanges();

                        trans.Commit();
                        TempData["success"] = "Xóa đơn hàng thành công";
                    }
                }
                catch (Exception ex)
                {
                    trans.Dispose();
                    TempData["error"] = ex.Message;
                }
                return RedirectToAction("index");
            }
        }

        public JsonResult checkgiftcode(string code)
        {
            try
            {
                var gift_Code = db.gift_code.FirstOrDefault(g => g.code == code);
                if (gift_Code == null)
                {
                    throw new Exception("Không tìm thấy mã giảm giá này");
                }
                if (gift_Code.start_date > DateTime.Now.Date)
                {
                    throw new Exception("Mã giảm giá này chỉ có hiệu lực từ ngày " + gift_Code.start_date?.ToString("dd/MM/yyyy"));
                }
                if (gift_Code.end_date < DateTime.Now.Date)
                {
                    throw new Exception("Mã giảm giá này đã hết hạn từ ngày " + gift_Code.end_date?.ToString("dd/MM/yyyy"));
                }
                Session[SessionOrderDetail_discountPercent] = (double?)gift_Code.percent;
                Session[SessionOrderDetail_discountAmount] = (double?)gift_Code.price;
                return Json(new object[] { true, "Đã áp dụng mã giảm giá", gift_Code });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }

        }

        public ActionResult ViewOrder(string id)
        {
            try
            {
                var order = db.orders.Find(id);
                if (order != null)
                {
                    var cus = db.customers.Find(order.CustomerId);
                    if (cus == null)
                    {
                        throw new Exception("Không tìm thấy khách hàng.");
                    }

                    ViewBag.Title = $"Đơn hàng #{id}";
                    var language_default = SiteLang.GetDefault();

                    var listProductItem = db.orders_detail.Where(x => x.OrderId == id).ToList();
                    string listParentProp = string.Join(",", listProductItem.GroupBy(x => x.ParentPropertiesId).Select(x => x.Key).Distinct());
                    string listChildProp = string.Join(",", listProductItem.GroupBy(x => x.PropertiesId).Select(x => x.Key).Distinct());

                    var listProp = (from x in db.product_properties
                                    where (listParentProp.Contains(x.ReId) && x.LangCode == language_default.Code)
                                    || (listChildProp.Contains(x.ReId) && x.LangCode == language_default.Code)
                                    orderby x.name
                                    select x).ToList();

                    ViewBag.ListProp = listProp;
                    ViewBag.ListOrderDetail = listProductItem;

                    string cusAddress = $"{cus.Address}, ";
                    if (!string.IsNullOrEmpty(cus.Ward))
                    {
                        var ward = db.vn_ward.Find(cus.Ward);
                        cusAddress += $"{ward.type} {ward.name}, ";
                    }
                    if (!string.IsNullOrEmpty(cus.District))
                    {
                        var district = db.vn_district.Find(cus.District);
                        cusAddress += $"{district.type} {district.name}, ";
                    }
                    if (!string.IsNullOrEmpty(cus.Province))
                    {
                        var province = db.vn_province.Find(cus.Province);
                        cusAddress += $"{province.type} {province.name}, ";
                    }
                    ViewBag.CusAddress = cusAddress;

                    string shipping = $"{order.ShippingAddress}, ";
                    if (!string.IsNullOrEmpty(order.Shipping_Ward))
                    {
                        var ward = db.vn_ward.Find(order.Shipping_Ward);
                        shipping += $"{ward.type} {ward.name}, ";
                    }
                    if (!string.IsNullOrEmpty(order.Shipping_District))
                    {
                        var district = db.vn_district.Find(order.Shipping_District);
                        shipping += $"{district.type} {district.name}, ";
                    }
                    if (!string.IsNullOrEmpty(order.Shipping_Province))
                    {
                        var province = db.vn_province.Find(order.Shipping_Province);
                        shipping += $"{province.type} {province.name}, ";
                    }
                    ViewBag.Shipping = shipping;
                    order.PaymentData = db.vnp_paymentdata.Where(pay => pay.OrderId == order.Id).ToList().Select(pay => new PaymentData(pay)).FirstOrDefault() ?? new PaymentData();
                    return View(order);
                }
                else
                {
                    throw new Exception("Đơn hàng không tồn tại.");
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("index");
            }
        }
        #endregion

        #region PRODUCT
        public ActionResult LoadProduct(string cateReId)
        {
            try
            {
                var default_language = SiteLang.GetDefault();
                var listProduct = db.products.Where(x => x.IsActive != false && x.LangCode == default_language.Code);
                if (!string.IsNullOrEmpty(cateReId))
                {
                    //var listCate = Products.GetAllChildCategory(cateReId);
                    listProduct = listProduct.Where(x => x.CategoryId.Contains(cateReId));
                }
                return PartialView("_ListProductPartial", listProduct.OrderBy(x => x.ProductName).ToList());
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return PartialView("_ListProductPartial", new List<product>());
            }
        }

        // Select item order
        public ActionResult GetProductDetail(string proReId, string orderDetail)
        {
            try
            {
                var languageDefault = SiteLang.GetDefault();
                var proDetail = db.products.Where(x => x.ReId == proReId && x.IsActive != false && x.LangCode == languageDefault.Code).FirstOrDefault();
                proDetail.Quantity = (proDetail.Quantity == null || proDetail.Quantity == -1) ? 1 : proDetail.Quantity;

                string propExist = string.Empty, priceOptExist = string.Empty;
                var listProperties = new List<product_properties>();
                if (!string.IsNullOrEmpty(proDetail.list_parent_properties) && !string.IsNullOrEmpty(proDetail.list_properties))
                {
                    listProperties = (from x in db.product_properties
                                      where (proDetail.list_parent_properties.Contains(x.ReId) && x.active != false && x.LangCode == languageDefault.Code)
                                      || (proDetail.list_properties.Contains(x.ReId) && x.active != false && x.LangCode == languageDefault.Code)
                                      orderby x.name
                                      select x).ToList();
                }

                if (!string.IsNullOrEmpty(orderDetail))
                {
                    var listProductItem = new List<orders_detail>();
                    if (Session[SessionOrderDetail] != null)
                    {
                        listProductItem = Session[SessionOrderDetail] as List<orders_detail>;
                    }

                    var proItem = listProductItem.Find(x => x.Id == orderDetail);
                    if (proItem == null)
                    {
                        throw new Exception("Không tìm thấy sản phẩm");
                    }
                    proDetail.Price = proItem.Price;
                    proDetail.Quantity = proItem.Quantity;

                    if (!string.IsNullOrEmpty(proItem.PriceOptId))
                    {
                        priceOptExist = proItem.PriceOptId;
                    }

                    if (!string.IsNullOrEmpty(proItem.ParentPropertiesId))
                    {
                        propExist = proItem.PropertiesId;
                    }
                }

                var arrObj = new object[]
                {
                    listProperties,
                    priceOptExist,
                    propExist,
                    orderDetail,
                    db.productoptionprices.Where(x => x.ProductId == proReId && x.IsActive == true && x.LangCode == languageDefault.Code).ToList(),
                };
                ViewBag.ArrObj = arrObj;

                return PartialView("_ProductPopupPartial", proDetail);
            }
            catch (Exception ex)
            {
                ViewBag.ErrMsg = ex.Message;
                return PartialView("_ProductPopupPartial", new product());
            }
        }

        // Add item order
        public ActionResult AddItemOrder(string proReId, string _price, int? _quantity, string _orderDetail, string price_optReId, string DiscountAmount, string discount_type)
        {
            price_optReId = price_optReId ?? string.Empty;
            var listProductItem = new List<orders_detail>();
            if (Session[SessionOrderDetail] != null)
            {
                listProductItem = Session[SessionOrderDetail] as List<orders_detail>;
            }

            try
            {
                var language_default = SiteLang.GetDefault();
                var pro = db.products.FirstOrDefault(x => x.ReId == proReId && x.LangCode == language_default.Code);
                if (pro == null)
                {
                    throw new Exception("Không tìm thấy sản phẩm");
                }

                var childProp = string.IsNullOrEmpty(pro.list_parent_properties) ? string.Empty : string.Join(",", pro.list_parent_properties.Split(',').Select(x => Request[$"{x}"]));
                var proItem_exist = listProductItem.FirstOrDefault(x => x.ProductId == proReId && x.PriceOptId == price_optReId && x.PropertiesId == childProp);
                if (string.IsNullOrEmpty(_orderDetail))
                {
                    #region ========= Add Item =========
                    if (proItem_exist == null)
                    {
                        var proItem = new orders_detail()
                        {
                            Id = AppFunc.NewShortId(),
                            ProductId = pro.ReId,
                            ProductName = pro.ProductName,
                            Price = double.Parse(string.IsNullOrEmpty(_price) ? "0" : _price.Replace(".", string.Empty)),
                            Quantity = (_quantity == null || _quantity <= 0) ? 1 : _quantity,
                            ParentPropertiesId = pro.list_parent_properties ?? string.Empty,
                            PropertiesId = childProp,
                            PriceOptId = price_optReId,
                        };

                        if (!string.IsNullOrEmpty(price_optReId))
                        {
                            proItem.ProductName += $" ({db.productoptionprices.FirstOrDefault(x => x.ReId == price_optReId && x.LangCode == language_default.Code)?.LabelName})";
                        }
                        listProductItem.Add(proItem);
                    }
                    else
                    {
                        proItem_exist.Price = double.Parse(string.IsNullOrEmpty(_price) ? "0" : _price.Replace(".", string.Empty));
                        proItem_exist.Quantity += (_quantity == null || _quantity <= 0) ? 1 : _quantity;
                    }
                    #endregion
                }
                else
                {
                    #region ========= Update Item =========
                    var proItemUpdate = listProductItem.FirstOrDefault(x => x.Id == _orderDetail);
                    if (proItemUpdate == null)
                    {
                        throw new Exception("Không tìm thấy sản phẩm");
                    }

                    if (proItem_exist == null)
                    {
                        proItemUpdate.ProductName = pro.ProductName;
                        proItemUpdate.Price = double.Parse(string.IsNullOrEmpty(_price) ? "0" : _price.Replace(".", string.Empty));
                        proItemUpdate.Quantity = (_quantity == null || _quantity <= 0) ? 1 : _quantity;
                        proItemUpdate.PropertiesId = childProp;
                        proItemUpdate.PriceOptId = price_optReId;
                        if (!string.IsNullOrEmpty(price_optReId))
                        {
                            proItemUpdate.ProductName += $" ({db.productoptionprices.FirstOrDefault(x => x.ReId == price_optReId && x.LangCode == language_default.Code)?.LabelName})";
                        }
                    }
                    else
                    {
                        proItem_exist.Price = double.Parse(string.IsNullOrEmpty(_price) ? "0" : _price.Replace(".", string.Empty));
                        if (proItem_exist.Id == proItemUpdate.Id)
                        {
                            proItem_exist.Quantity = (_quantity == null || _quantity <= 0) ? 1 : _quantity;
                        }
                        else
                        {
                            proItem_exist.Quantity += (_quantity == null || _quantity <= 0) ? 1 : _quantity;
                            listProductItem.Remove(proItemUpdate);
                        }
                    }
                    #endregion
                }

                string listParentProp = string.Join(",", listProductItem.GroupBy(x => x.ParentPropertiesId).Select(x => x.Key).Distinct());
                string listChildProp = string.Join(",", listProductItem.GroupBy(x => x.PropertiesId).Select(x => x.Key).Distinct());

                var listProp = (from x in db.product_properties
                                where (listParentProp.Contains(x.ReId) && x.LangCode == language_default.Code)
                                || (listChildProp.Contains(x.ReId) && x.LangCode == language_default.Code)
                                orderby x.name
                                select x).ToList();

                ViewBag.ListProp = listProp;
                ViewBag.FreeShip = db.shipping_config.FirstOrDefault(s => s.Active && s.Type == "free")?.ShippingFee ?? 0;
                ViewBag.discountPercent = Session[SessionOrderDetail_discountPercent] as double?;
                ViewBag.discountAmount = Session[SessionOrderDetail_discountAmount] as double?;
                Session[SessionOrderDetail] = listProductItem;
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.ListProp = new List<product_properties>();
            }
            return PartialView("_ProductItemPartial", listProductItem);
        }

        // Remove item order
        public JsonResult RemoveItemOrder(string orderDetail)
        {
            try
            {
                var listProductItem = new List<orders_detail>();
                if (Session[SessionOrderDetail] != null)
                {
                    listProductItem = Session[SessionOrderDetail] as List<orders_detail>;
                }

                var proItem = listProductItem.Where(x => x.Id == orderDetail).FirstOrDefault();
                if (proItem != null)
                {
                    listProductItem.Remove(proItem);

                    var subTotal = listProductItem.Sum(x => x.Quantity * x.Price) ?? 0;
                    Session[SessionOrderDetail] = listProductItem;
                    return Json(new object[] { true, subTotal }, JsonRequestBehavior.AllowGet);
                }

                return Json(new object[] { true, "-1" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region CUSTOMER
        public ActionResult SearchCustomer(string search)
        {
            try
            {
                var list_cus = new List<customer>();
                if (!string.IsNullOrEmpty(search))
                {
                    var sqlCommand = CommonFunc.SearchCommand("customers", search, "FullName", "Phone");
                    list_cus = db.customers.SqlQuery(sqlCommand).Where(x => x.Active != false).ToList();
                }
                else
                {
                    list_cus = db.customers.Where(x => x.Active != false).ToList();
                }
                return PartialView("_SearchCustomerPartial", list_cus);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.InnerException?.Message ?? ex.Message;
                return PartialView("_SearchCustomerPartial", null);
            }
        }

        public JsonResult GetCustomerInfo(string id)
        {
            try
            {
                var cus = db.customers.Find(id);
                string ward = db.vn_ward.Where(x => x.wardid == cus.Ward).Select(x => x.type + " " + x.name).FirstOrDefault() ?? string.Empty;
                string district = db.vn_district.Where(x => x.districtid == cus.District).Select(x => x.type + " " + x.name).FirstOrDefault() ?? string.Empty;
                string province = db.vn_province.Where(x => x.provinceid == cus.Province).Select(x => x.type + " " + x.name).FirstOrDefault() ?? string.Empty;

                string address = cus.Address + (ward == string.Empty ? string.Empty : $", {ward}") + (district == string.Empty ? string.Empty : $", {district}") + (province == string.Empty ? string.Empty : $", {province}");
                return Json(new object[] { true, cus, address });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }
        #endregion

    }
}