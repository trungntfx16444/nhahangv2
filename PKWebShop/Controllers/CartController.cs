namespace ClientPage.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Web.Mvc;
    using System.Threading.Tasks;
    using PKWebShop.Models;
    using System.Security.Policy;
    using Ubiety.Dns.Core;
    using PKWebShop.Controllers;
    using PKWebShop.AppLB;
    using ClientPage.DataAsset;
    using PKWebShop.Utils;
    using PKWebShop.Services;
    using PKWebShop.DataAsset;
    using Newtonsoft.Json.Linq;

    public class CartController : ExpiredCheckController
    {
        // GET: Cart
        private const string GIFTCODE = "gift_code";
        private const string POINTRATE = "PointRate";
        private readonly DBLangCustom db = new();
        private customer curCus = UserContent.getCurrentCustomer();

        public ActionResult Index()
        {
            return View(getDataIndex());
        }

        public ActionResult _Index()
        {
            return PartialView(getDataIndex());
        }

        public List<cart_detail> getDataIndex()
        {
            //var ck_giftcode = CookiesEncryption.Decrypt(Uri.UnescapeDataString(Request.Cookies[GIFTCODE]?.Value ?? string.Empty));
            //string giftcode = string.Empty;
            //if (ck_giftcode != null && ck_giftcode.Contains("|"))
            //{
            //    string isApplyCode = ck_giftcode.Split('|').LastOrDefault();
            //    giftcode = ck_giftcode.Replace($"|{isApplyCode}", string.Empty);
            //}

            var cus = UserContent.getCurrentCustomer();
            ViewBag.cus = cus;
            ViewBag.Note = db.sectionfeaturedetails.FirstOrDefault(s => s.SectionCode == "Ghichu_giohang")?.Description;
            ViewBag.provinces = db.vn_province.OrderByDescending(x => x.type).ThenBy(x => x.name).ToList();
            ViewBag.districts = db.vn_district.OrderByDescending(x => x.type).ThenBy(x => x.name).ToList();
            ViewBag.PointRate = db.BonusPointConfig.FirstOrDefault()?.Value ?? 0;
            return getCartProducts(true);
        }

        //public string GetGuestCustomerId()
        //{
        //    HttpCookie cookie = Request.Cookies["g_cus_id"] ?? new HttpCookie("g_cus_id");
        //    cookie.Value ??= Guid.NewGuid().ToString();
        //    cookie.Expires = DateTime.UtcNow.AddDays(30);
        //    Response.Cookies.Add(cookie);
        //    return cookie.Value;
        //}
        public class option_select
        {
            public string Id { get; set; }
            public int Quantity { get; set; }
        }
        public JsonResult AddToCart(string productId, int? Quantity, string option_1, string option_2, List<option_select> Options)
        {
            using (var da = new DA_CartDetail(curCus?.Id))
            {
                if (Options != null)
                {
                    foreach (var opt in Options.Where(q => q.Quantity > 0))
                    {
                        da.AddToCart(productId, opt.Quantity, "Product", option_1, opt.Id);
                    }
                }
                else
                {
                    da.AddToCart(productId, Quantity, "Product", option_1, option_2);
                }
                //List<cart_detail> list = da.GetCart().OrderBy(x => x.CreatedAt).ToList();
                var cart_detail = CommonFunc.RenderRazorViewToString("_CartView", getCartProducts(), this);
                return Json(new object[] { true, "Đã cập nhật giỏ.", cart_detail });
            }
        }
        public JsonResult RemoveFromCart(int? Id)
        {
            using (var da = new DA_CartDetail(curCus?.Id))
            {
                da.RemoveFromCart(Id);
                //List<cart_detail> list = da.GetCart().OrderBy(x => x.CreatedAt).ToList();
                var cart_detail = CommonFunc.RenderRazorViewToString("_CartView", getCartProducts(), this);
                return Json(new object[] { true, "Đã xóa sản phẩm khỏi giỏ.", cart_detail });
            }
        }

        public JsonResult CheckProduct(string prodReId)
        {
            try
            {
                var prod = db.products.FirstOrDefault(x => x.ReId == prodReId);
                if (prod == null)
                {
                    throw new Exception("Sản phẩm không tồn tại");
                }

                string propReId = string.Empty;
                string firstOptPrice_ReId = string.Empty;
                if (!string.IsNullOrEmpty(prod.list_parent_properties))
                {
                    var list_parentProperties = string.Join(",", (from prop in db.product_properties
                                                                  where prod.list_parent_properties.Contains(prop.ReId)
                                                                  orderby prop.name
                                                                  select prop.ReId).ToList());

                    foreach (var item in list_parentProperties.Split(','))
                    {
                        if (item != string.Empty)
                        {
                            var parentPropId = db.product_properties.FirstOrDefault(x => x.ReId == item)?.id ?? string.Empty;
                            propReId += (from prop in db.product_properties
                                         where prod.list_properties.Contains(prop.ReId)
                                         && prop.parrent_properties_id == parentPropId && prop.is_parrent == false
                                         orderby prop.name
                                         select prop.ReId).FirstOrDefault() + "|";
                        }
                    }

                    //var firstOptPrice = db.ProductOptionPrice.Where(x => x.ProductId == prodReId && x.IsActive != false && x.LangCode == prod.LangCode).OrderBy(x => x.Price).FirstOrDefault();
                    //if (firstOptPrice != null)
                    //{
                    //    firstOptPrice_ReId = firstOptPrice.ReId;
                    //    prod.ProductName += $" ({firstOptPrice.LabelName})";
                    //    prod.Price = firstOptPrice.Price ?? 0;
                    //}
                    //else
                    //{
                    //    prod.Price = prod.SalePrice ?? prod.Price;
                    //}
                    prod.Price = prod.SalePrice ?? prod.Price;
                }
                else
                {
                    prod.Price = (prod.SalePrice > 0 && prod.SalePrice < prod.Price) ? prod.SalePrice : prod.Price;
                }

                return Json(new object[] { true, prod, propReId, firstOptPrice_ReId });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        public ActionResult _CartView()
        {
            return PartialView("_CartView", getCartProducts());
        }

        public List<cart_detail> getCartProducts(bool includeDiscount = false)
        {
            var model = new List<cart_detail>();

            using (var da = new DA_CartDetail(curCus?.Id))
            {
                model = da.GetCart(includeDiscount).OrderBy(x => x.CreatedAt).ToList();
            }

            var ck_propReId = model.Select(x => x.Properties?.Split('|') ?? new string[0]).SelectMany(x => x).Distinct().ToArray();
            var listProp = db.product_properties.Where(x => ck_propReId.Contains(x.ReId)).ToList();
            ViewBag.ListProp = listProp;
            var listShopIds = model.Select(s => s.ShopId).Distinct().ToList();
            var district_id = CookiesEncryption.Decrypt(Request.Cookies["ship_district"]?.Value ?? string.Empty);
            if (string.IsNullOrEmpty(district_id))
            {
                district_id = curCus?.District ?? string.Empty;
            }

            ViewBag.selected_district = db.vn_district.Find(district_id);

            //if (ViewBag.giftcode is gift_code giftCode)
            //{
            //    if (giftCode.GrandTotalFrom > (decimal)model.Sum(p => p.Price * p.Quantity))
            //    {
            //        TempData["giftcode_e"] = $"Mã giảm giá chỉ áp dụng cho đơn hàng từ {giftCode.GrandTotalFrom ?? 0:0,###} {Constant.CURRENCY_SIGN}";
            //        TempData["giftcode_s"] = null;
            //    }
            //}

            return model;
        }

        public ActionResult CheckOut()
        {
            var cus = UserContent.getCurrentCustomer();
            if (cus == null)
            {
                return Redirect("/home/login?urlback=/cart/checkout");
            }

            var db = new DBLangCustom();
            ViewBag.vn_district = db.vn_district.OrderByDescending(x => x.type).ThenBy(x => x.name).ToList();
            ViewBag.vn_province = db.vn_province.OrderByDescending(x => x.type).ThenBy(x => x.name).ToList();
            ViewBag.cus = cus;
            return View(getDataIndex());
        }

        public ActionResult _CheckOut()
        {
            var cus = UserContent.getCurrentCustomer() ?? new customer();
            var ck_giftcode = CookiesEncryption.Decrypt(Uri.UnescapeDataString(Request.Cookies[GIFTCODE]?.Value ?? string.Empty));
            string giftcode = string.Empty;
            string isApplyCode = string.Empty;
            if (ck_giftcode != null && ck_giftcode.Contains("|"))
            {
                isApplyCode = ck_giftcode.Split('|').LastOrDefault();
                giftcode = ck_giftcode.Replace($"|{isApplyCode}", string.Empty);
            }

            if (!string.IsNullOrEmpty(giftcode))
            {
                var gift_code = db.gift_code.FirstOrDefault(g => g.code == giftcode && (g.end_date ?? DateTime.MaxValue) >= DateTime.Now && (g.start_date ?? DateTime.MinValue) <= DateTime.Now && g.active == true);
                if (gift_code == null || (gift_code != null && db.orders.Any(o => o.CustomerId == cus.Id && o.GiftCode == giftcode)))
                {
                    Response.Cookies[GIFTCODE].Expires = DateTime.Now.AddDays(-1);
                }
                else
                {
                    if (isApplyCode == "1")
                    {
                        ViewBag.giftcode = gift_code;
                    }
                }
            }

            ViewBag.free_ship = db.shipping_config.FirstOrDefault(s => s.Active && s.Type == "free")?.ShippingFee ?? 0;
            var product = getCartProducts();
            if (!string.IsNullOrEmpty(TempData["giftcode_e"]?.ToString()))
            {
                Response.Cookies[GIFTCODE].Expires = DateTime.Now.AddDays(-1);
                ViewBag.giftcode = null;
            }

            return PartialView(product);
        }

        public ActionResult CheckOut_submit(order order)
        {
            var redirectTo = string.Empty;
            DBLangCustom db = new();

            List<cart_detail> orders_Details = new();
            var CartCusId = curCus?.Id ?? new DA_CartDetail().cusId;
            using (var da = new DA_CartDetail(CartCusId))
            {
                orders_Details = da.GetCart(true).ToList();
            }

            try
            {
                order.Shipping_Phone = CommonFunc.CleanPhoneNumber(order.Shipping_Phone);
                var giftcode = CheckGiftCode(order.Shipping_Phone, out string err_msg);
                if (!string.IsNullOrEmpty(err_msg))
                {
                    TempData["giftcode_e"] = err_msg;
                    return Redirect("/gio-hang");
                }

                var cus = UserContent.getCurrentCustomer();
                if (cus == null)
                {
                    cus = new CustomerService().Register(new customer
                    {
                        AccountType = "other",
                        Active = true,
                        Province = order.Shipping_Province,
                        District = order.Shipping_District,
                        Ward = order.Shipping_Ward,
                        Address = order.ShippingAddress,
                        CreateAt = DateTime.Now,
                        FullName = order.Shipping_Name,
                        Phone = order.Shipping_Phone,
                        Email = Request["Shipping_Email"].ToString(),
                        CreateBy = "SYSTEM",
                        Id = AppFunc.NewShortId(),
                    });

                    if (cus == null)
                    {
                        throw new Exception("Tài khoản đăng ký bằng sđt này đã bị khóa, vui lòng dùng sđt khác");
                    }

                    // DA_Customer DA_cus = new DA_Customer(new WebShopEntities());
                    //DA_cus.CustomerLogin(cus, out string errMsg);
                }

                //var listReId = orders_Details.Select(x => x.ProductId).ToArray();

                //var dbLang = new DBLangCustom();
                //var listItem = (from p in dbLang.products.Where(p => listReId.Contains(p.ReId)).ToList()
                //                join ck_item in orders_Details on p.ReId equals ck_item.ProductId
                //                select new { ck_item, p }).ToList().Select(item =>
                //                {
                //                    item.ck_item.Price = item.p.SalePrice ?? item.p.Price;
                //                    return item.ck_item;
                //                }).ToList();

                var ck_PointRate = int.Parse(CookiesEncryption.Decrypt(Uri.UnescapeDataString(Request.Cookies[POINTRATE]?.Value ?? "0")));

                // CREATE ORDER
                order created_order = new DA_Order().CreateOrder(cus.Id, order, orders_Details, DateTime.Now, giftcode, ck_PointRate);
                using (var da = new DA_CartDetail(CartCusId))
                {
                    da.RemoveAllFromCart();
                }

                var info = UserContent.GetWebInfomation();
                new Task(() =>
                {
                    using (var db = new WebShopEntities())
                    {
                        string subject = $"[{ConfigurationManager.AppSettings["Domain"]}] Khách hàng {cus.FullName} đã đặt một đơn hàng";
                        string link_ne = $"{ConfigurationManager.AppSettings["admin_site"]}/login?ReturnUrl=%2forderman%2fvieworder%2f{created_order.Id}";
                        string body =
                                $@"<table cellpadding='20' cellspacing='0' style='width: 600px;margin-left: auto;margin-right: auto;border: 1px solid #dedede; border-radius: 3px;'><thead><tr><td style='padding: 12px 48px; background-color: #0193de; border-bottom: 0; font-weight: bold; border-radius: 3px 3px 0 0;'><h1 style='font-size:22px;font-weight:300;line-height:150%;margin:0;text-align:left;color:#ffffff;background-color:inherit;text-align:center;'>Thông Tin Đặt Hàng</h1></td></tr></thead><tbody><tr><td valign='top' style='padding:32px 48px 32px'><div style='color:#636363;font-family:&quot;Helvetica Neue&quot;,Helvetica,Roboto,Arial,sans-serif;font-size:14px;line-height:150%;text-align:left'>
                            <p>Xin chào {info.CompanyName},</p>
                            <p>Bạn nhận được một đơn hàng giá trị {created_order.GrandTotal ?? 0:0,###} {Constant.CURRENCY_SIGN} từ Khách hàng {order.Shipping_Name} tại website: {ConfigurationManager.AppSettings["Domain"]}</p>
                            <p style='margin: 0 0;'>Vui lòng click vào <a href='{link_ne}' target='_blank'>link này</a> để vào xem chi tiết đơn hàng.</p>
                            </div></td></tr></tbody><tfoot style='background-color: #eff1f3;'><tr><td style='text-align: center;padding: 10px; font-size: 12px;font-family:&quot;Helvetica Neue&quot;,Helvetica,Roboto,Arial,sans-serif;'> Đây là email đặt hàng từ website: {ConfigurationManager.AppSettings["Domain"]}</td></tr></tfoot></table>";

                        var result = SendEmail.Send(info.ContactEmail, subject, body, string.Empty, string.Empty, null);
                    }
                    new PaymentServices().SendInvoice(ControllerContext, created_order.Id);
                }).Start();

                Response.Cookies["ship_district"].Expires = DateTime.Now.AddDays(-1);
                redirectTo = $"../thank-you?Id={created_order.Id}&cartclear=1";
                TempData["s"] = "Đặt hàng thành công. Chúng tôi sẽ sớm liên lạc với bạn.";

                // Pay
                var webLicense = new PackageServices().WebPackInfo();
                if (Request["PaymentMethod"] == "vnpay" && webLicense.PaymentOnline)
                {
                    return VnpayPayment(created_order.Id);
                }
                return Redirect(redirectTo);
            }
            catch (Exception ex)
            {
                TempData["e"] = $"Đặt hàng không thành công. {ex.Message}.";
                var inner = ex.InnerException?.Message.Split('|');
                TempData["e_id"] = inner.ElementAtOrDefault(0);
                TempData["qty"] = inner.ElementAtOrDefault(1);
                return Redirect("/gio-hang");
            }
        }

        public ActionResult AddGiftCode(string code, string shopid)
        {
            try
            {
                var gc = db.gift_code.FirstOrDefault(g => g.code == code);
                if (gc == null)
                {
                    throw new Exception("Mã giảm giá không đúng.");
                }

                using (var da = new DA_CartDetail(curCus?.Id))
                {
                    da.AddToCart(gc.code, 1, "Discount");
                    TempData["s"] = "Áp dụng mã giảm giá thành công.";
                }
            }
            catch (Exception ex)
            {
                TempData["e"] = ex.Message;
            }
            return PartialView("_index", getDataIndex());
        }

        public string CheckGiftCode(string shippingPhone, out string err_msg)
        {
            err_msg = string.Empty;
            var ck_giftcode = CookiesEncryption.Decrypt(Uri.UnescapeDataString(Request.Cookies[GIFTCODE]?.Value ?? string.Empty));
            string giftcode = string.Empty;
            string isApplyCode = string.Empty;

            if (ck_giftcode != null && ck_giftcode.Contains("|"))
            {
                isApplyCode = ck_giftcode.Split('|').LastOrDefault();
                giftcode = isApplyCode == "1" ? ck_giftcode.Replace($"|{isApplyCode}", string.Empty) : string.Empty;
                if (!string.IsNullOrEmpty(giftcode))
                {
                    var cusExist = db.customers.FirstOrDefault(x => x.Phone == shippingPhone);
                    if (cusExist != null && db.orders.Any(o => o.CustomerId == cusExist.Id && o.GiftCode == giftcode))
                    {
                        Response.Cookies[GIFTCODE].Expires = DateTime.Now.AddDays(-1);
                        err_msg = $"Mã '{giftcode?.ToUpper()}' đã được bạn sử dụng ở một đơn hàng khác";
                    }
                }
            }
            List<object?> maybeListOfMaybeItems = new List<object?> { 1, 2, 3 };

            // inferred as IEnumerable<object?>
            var listOfMaybeItems = maybeListOfMaybeItems!;

            // no warning given, because ! supresses nullability warnings
            List<object> listOfItems = maybeListOfMaybeItems!;

            // warning about the generic type change, since this line does not have !
            List<object> listOfItems2 = listOfMaybeItems;
            return giftcode;
        }

        public ActionResult VnpayPayment(string orderId)
        {
            using DBLangCustom _db = new DBLangCustom();
            string type_ = Request["type"] ?? string.Empty;
            order order = _db.orders.Find(orderId)!;
            // Pay
            var rs = new PaymentServices().Pay(order!, _db.LangCode, Url.Action("VnpayCallback", new { type = type_ }), out string paymentUrl);
            if (string.IsNullOrEmpty(paymentUrl) == false)
            {
                Response.Redirect(paymentUrl);
            }
            return Redirect($"../customer/profile?cartclear=1#{order.Id}");
        }

        public ActionResult VnpayCallback(string type)
        {
            try
            {
                var grouporder_id = new PaymentServices().Callback();
                TempData["info-vnpay"] = "Thanh toán thành công.";
                VnpayNotiPayment(grouporder_id);
                new PaymentServices().SendInvoice(ControllerContext, grouporder_id, type);
            }
            catch (Exception e)
            {
                TempData["err-vnpay"] = $"Thanh toán không thành công do: {e.Message}";
                TempData["ex_log"] = $"{e}";
            }

            return Redirect($"../customer/profile");
        }

        public ActionResult VnpayNotiPayment(string OrderId)
        {
            var order = db.orders.Find(OrderId);
            var rs = new PaymentServices().NotiPayment(ControllerContext, order);
            return Json(new object[] { rs });
        }

        public ActionResult selectShippingDistrict(string district)
        {
            Response.Cookies["ship_district"].Value = CookiesEncryption.Encrypt(district);
            Response.Cookies["ship_district"].Expires = DateTime.UtcNow.AddDays(7);
            return Redirect("index");
        }

        public JsonResult getShippingFee(string id = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = CookiesEncryption.Decrypt(Request.Cookies["ship_district"]?.Value ?? string.Empty);
            }
            else
            {
                Response.Cookies["ship_district"].Value = CookiesEncryption.Encrypt(id);
                Response.Cookies["ship_district"].Expires = DateTime.UtcNow.AddDays(7);
            }

            if (!string.IsNullOrEmpty(id))
            {
                using (var da = new DA_CartDetail(curCus?.Id))
                {
                    var rs = da.GetShippingFee(id);
                    return Json(new object[] { true, rs.Sum(c => c.ShippingFee) });
                }
            }

            return Json(new object[] { true, 0 });
        }

        public ActionResult UpdateCartDetailQty(int id, int qty)
        {
            try
            {
                using (var da = new DA_CartDetail(curCus?.Id))
                {
                    var rs = da.SetQty(id, qty);
                }
            }
            catch (Exception ex)
            {
                TempData["e"] = ex.Message;
                return PartialView("_index", getDataIndex());
            }

            return PartialView("_index", getDataIndex());
        }
        public JsonResult LoadWards(string id)
        {
            var db = new DBLangCustom();
            return Json(db.vn_ward.Where(i => i.districtid == id).OrderBy(w => w.name).AsEnumerable().Select(w => new object[] { w.wardid, w.type + " " + w.name }).ToList() ?? new List<object[]>());
        }
    }
}