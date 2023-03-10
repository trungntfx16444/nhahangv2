namespace AdminPage.Controllers
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using System.Web.Mvc;
    using AdminPage.AppLB;
    using AdminPage.Models;
    using AdminPage.Utils;
    using Inner.Libs.Helpful;

    [Authorize]
    public class CustomerManController : ExpiredCheckController
    {
        // GET: Admin/Order
        private AdminEntities db = new();
        private Dictionary<string, bool> access = Authority.GetAccessAuthority();

        public ActionResult Index()
        {
            if (!access.ContainsKey("update_customer"))
            {
                TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                return RedirectToAction("index", "home");
            }

            return View("index");
        }

        #region CUSTOMER BIG UPDATE 29/06/2021 BY ONG-LOC
        public ActionResult LoadListCustomer(string searchText)
        {
            try
            {
                List<CustomerModelCustomize> lstcus = new();
                List<dataQuery> getdb = new();
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    string slqCommand = CommonFunc.SearchCommand("customers", searchText, "FullName", "Phone");

                    getdb = (from c in db.customers.SqlQuery(slqCommand)
                             join o in db.orders.GroupBy(o => o.CustomerId) on c.Id equals o.Key into j_gr
                             from gr in j_gr.DefaultIfEmpty()
                             select new dataQuery
                             {
                                 cus = c,
                                 count = gr?.Count(),
                                 compl = gr?.Count(o => o.Status == "3"),
                                 TotalAmount = gr?.Where(o => o.Status == "3")?.Sum(o => o.GrandTotal),
                             }).ToList();
                }
                else
                {
                    getdb = (from c in db.customers
                             join o in db.orders.GroupBy(o => o.CustomerId).AsQueryable() on c.Id equals o.Key into j_gr
                             from gr in j_gr.DefaultIfEmpty()
                             select new dataQuery
                             {
                                 cus = c,
                                 count = gr.Count(),
                                 compl = gr.Count(o => o.Status == "3"),
                                 TotalAmount = gr.Where(o => o.Status == "3").Sum(o => o.GrandTotal),
                             }).ToList();
                }

                lstcus = getdb.Select(d =>
                new CustomerModelCustomize()
                {
                    Id = d.cus.Id.ToString(),
                    FullName = d.cus.FullName,
                    Phone = d.cus.Phone,
                    Address = d.cus.Address,
                    Province = string.IsNullOrEmpty(d.cus.Province) ? string.Empty : db.vn_province.Where(x => x.provinceid == d.cus.Province).Select(x => x.type + " " + x.name).FirstOrDefault(),
                    District = string.IsNullOrEmpty(d.cus.District) ? string.Empty : db.vn_district.Where(x => x.districtid == d.cus.District).Select(x => x.type + " " + x.name).FirstOrDefault(),
                    Ward = string.IsNullOrEmpty(d.cus.Ward) ? string.Empty : db.vn_ward.Where(x => x.wardid == d.cus.Ward).Select(x => x.type + " " + x.name).FirstOrDefault(),
                    CreateAt = d.cus.CreateAt?.ToString("dd/MM/yyyy"),
                    NumberOfOrder = d.count ?? 0,
                    NumberOfOrder_completed = d.compl ?? 0,
                    TotalAmount = (d.TotalAmount ?? 0).ToString("#,##0") + "đ",
                    BonusPoint = d.cus.BonusPoints,
                    Email = d.cus.Email,
                    Active = d.cus.Active ?? false,
                    Role = d.cus.Role,
                }).ToList();

                return PartialView("_CustomersDataTable", lstcus);
            }
            catch (Exception ex)
            {
                ViewBag.LoadCusError = ex.Message.Replace("\r\n", " ");
                return PartialView("_CustomersDataTable", new List<CustomerModelCustomize>());
            }
        }

        public JsonResult LoadCustomer(string id)
        {
            try
            {
                var customer = db.customers.Find(id);
                if (customer == null)
                {
                    throw new Exception("Khách hàng không tồn tại");
                }
                return Json(new object[] { true, customer });
            }
            catch (Exception ex)
            {
                return Json(new object[] { true, ex.Message });
            }
        }

        public JsonResult SaveCustomer(customer data, bool? isPartner)
        {
            try
            {
                if (string.IsNullOrEmpty(data.FullName) || string.IsNullOrEmpty(data.Email))
                {
                    throw new Exception("Vui lòng điền đầy đủ thông tin");
                }


                if (db.customers.Any(x => x.Email == data.Email && x.Id != data.Id))
                {
                    throw new Exception("Email này đã được sử dụng cho tài khoản khác");
                }
                var cusExist = new customer();
                if (!string.IsNullOrEmpty(data.Id))
                {
                    cusExist = db.customers.Find(data.Id);
                    if (cusExist == null)
                    {
                        throw new Exception("Khách hàng không tồn tại");
                    }
                }
                else
                {
                    cusExist.Id = AppFunc.NewShortId();
                    cusExist.Password = data.Password;
                    if (string.IsNullOrEmpty(data.Password))
                    {
                        throw new Exception("Bạn chưa điền mật khẩu");
                    }
                }
                cusExist.FullName = data.FullName;
                cusExist.Phone = data.Phone;
                cusExist.Email = data.Email;
                cusExist.Province = data.Province;
                cusExist.District = data.District;
                cusExist.Ward = data.Ward;
                cusExist.Address = data.Address;
                cusExist.Role = isPartner == true ? UserContent.CustomerRole.Partner.Text() : UserContent.CustomerRole.Member.Text();
                cusExist.Active = data.Active ?? false;
                cusExist.UpdateAt = DateTime.Now;
                cusExist.UpdateBY = Authority.GetThisUser()?.Fullname;
                cusExist.CreateAt ??= DateTime.Now;
                cusExist.CreateBy ??= Authority.GetThisUser().Fullname;
                db.customers.AddOrUpdate(cusExist);
                db.SaveChanges();
                return Json(new object[] { true, "Lưu thành công." });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        public JsonResult DeleteCustomer(string Id)
        {
            using (var tranS = db.Database.BeginTransaction())
            {
                try
                {
                    if (!access.ContainsKey("update_customer"))
                    {
                        throw new Exception("Bạn không có quyền xóa. Vui lòng liên hệ Admin.");
                    }

                    var cus = db.customers.Find(Id);
                    if (cus == null)
                    {
                        throw new Exception("Khách hàng không tồn tại");
                    }

                    var lstOrder = db.orders.Where(o => o.CustomerId == Id).ToList();
                    if (lstOrder.Count > 0)
                    {
                        foreach (var o in lstOrder)
                        {
                            var lstOrderDetail = db.orders_detail.Where(od => od.OrderId == o.Id).ToList();
                            if (lstOrderDetail != null && lstOrderDetail.Count > 0)
                            {
                                // xóa order detail
                                db.orders_detail.RemoveRange(lstOrderDetail);
                            }

                            // xóa order
                            db.orders.Remove(o);
                        }
                    }
                    db.customers.Remove(cus);
                    db.SaveChanges();
                    tranS.Commit();
                    return Json(new object[] { true, "Xóa thành công" });
                }
                catch (Exception ex)
                {
                    tranS.Dispose();
                    return Json(new object[] { false, ex.Message });
                }
            }
        }

        // Danh sách hóa đơn của từng khách hàng
        public ActionResult GetListOrder(string Id)
        {
            List<OrderModelCustomer> listOrder = new();
            try
            {
                var lstorder = db.orders.Where(o => o.CustomerId == Id).OrderByDescending(x => x.CreatedAt).ToList();
                if (lstorder != null && lstorder.Count > 0)
                {
                    var listSatus = UserContent.OrderStatus();
                    foreach (var item in lstorder)
                    {
                        OrderModelCustomer order = new()
                        {
                            Id = item.Id,
                            GrandTotal = item.GrandTotal?.ToString("#,###"),
                            CreatedAt = item.CreatedAt?.ToString("dd/MM/yyyy"),
                            Status = listSatus.Where(x => x.Key == item.Status).FirstOrDefault().Value,
                        };
                        listOrder.Add(order);
                    }
                }
                return PartialView("_ListOrder", listOrder);
            }
            catch (Exception ex)
            {
                ViewBag.LoadOrdersError = ex.Message;
                return PartialView("_ListOrder", listOrder);
            }
        }

        public JsonResult ResetPassword(string Id, string newpass, bool sendemail = false)
        {
            try
            {
                var curMem = Authority.GetThisUser();
                if (curMem.RoleLevel != (int)UserContent.UserRole.Admin)
                {
                    throw new Exception("Chỉ admin mới có quyền reset mật khẩu");
                }

                var customer = db.customers.FirstOrDefault(x => x.Id == Id);
                if (customer == null)
                {
                    throw new Exception("Khách hàng không tồn tại");
                }

                customer.Password = newpass;
                db.Entry(customer).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                string msg_success = $"Mật khẩu của khách hàng {customer.FullName} đã được thay đổi.<br/> Mật khẩu mới: <b>{customer.Password}</b>";
                if (sendemail)
                {
                    if (!string.IsNullOrEmpty(customer.Email))
                    {
                        #region SendEmail
                        string subject = $"[{ConfigurationManager.AppSettings["Domain"]}] Mật khẩu của bạn đã được thay đổi";
                        string body =
                            "<table cellpadding='20' cellspacing='0' style='width: 600px;margin-left: auto;margin-right: auto;border: 1px solid #dedede; border-radius: 3px;'><thead><tr><td style='padding: 12px 48px; background-color: #0193de; border-bottom: 0; font-weight: bold; border-radius: 3px 3px 0 0;'><h1 style='font-size:22px;font-weight:300;line-height:150%;margin:0;text-align:left;color:#ffffff;background-color:inherit;text-align:center;'>Thông tin tài khoản</h1></td></tr></thead><tbody><tr><td valign='top' style='padding:32px 48px 32px'><div style='color:#636363;font-family:&quot;Helvetica Neue&quot;,Helvetica,Roboto,Arial,sans-serif;font-size:14px;line-height:150%;text-align:left'>" +
                            "<p>Xin chào " + customer.FullName + ",</p>" +
                            "<p>Mật khẩu của bạn đã được reset.</p>" +
                            "<p>Dưới đây là thông tin tài khoản của bạn:</p>" +
                            "<p><b>Tên đăng nhập:</b> " + customer.Phone?.Replace(".", string.Empty).Replace("-", string.Empty) + "</p>" +
                            "<p><b>Mật khẩu mới:</b> " + customer.Password + "</p>" +
                            "</div></td></tr></tbody><tfoot style='background-color: #eff1f3;'><tr><td style='text-align: center;padding: 10px; font-size: 12px;font-family:&quot;Helvetica Neue&quot;,Helvetica,Roboto,Arial,sans-serif;'> Đây là email liên hệ của Khách hàng từ website: " + ConfigurationManager.AppSettings["Domain"] + "</td></tr></tfoot></table>";

                        var result = SendEmail.Send(customer.Email, subject, body, db.webconfigurations.FirstOrDefault()?.ContactEmail ?? string.Empty);
                        if (string.IsNullOrEmpty(result))
                        {
                            msg_success += $" đã được gửi tới email khách hàng {customer.Email ?? string.Empty}";
                        }
                        else
                        {
                            msg_success += ". Vui lòng lưu lại và gửi cho khách hàng.";
                        }
                        #endregion
                    }
                    else
                    {
                        msg_success += ". Vui lòng lưu lại và gửi cho khách hàng.";
                    }
                }
                return Json(new object[] { true, msg_success });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }
        #endregion

        #region Model customize

        public class CustomerModelCustomize
        {
            public string Id { get; set; }

            public string FullName { get; set; }

            public string UserName { get; set; }

            public string Password { get; set; }

            public string Phone { get; set; }

            public string Province { get; set; }

            public string District { get; set; }

            public string Ward { get; set; }

            public string Address { get; set; }

            public string Email { get; set; }

            public string Gender { get; set; }

            public string LastLogin { get; set; }

            public string Role { get; set; }

            public bool? Active { get; set; }

            public string CreateAt { get; set; }

            public int? NumberOfOrder { get; set; }
            public int NumberOfOrder_completed { get; internal set; }
            public string TotalAmount { get; internal set; }
            public int BonusPoint { get; internal set; }
        }

        public class OrderModelCustomer
        {
            public string Id { get; set; }

            public string CustomerId { get; set; }

            public string CustomerName { get; set; }

            public string CustomerPhone { get; set; }

            public string CustomerEmail { get; set; }

            public string ShippingAddress { get; set; }

            public double? SubTotal { get; set; }

            public double? TaxPercent { get; set; }

            public double? Shipping { get; set; }

            public string GrandTotal { get; set; }

            public string CreatedAt { get; set; }

            public string IP { get; set; }

            public string Browser { get; set; }

            public string Comment { get; set; }

            public string Complete { get; set; }

            public bool? Cancel { get; set; }

            public DateTime? UpdateAt { get; set; }

            public string UpdateBy { get; set; }

            public string Customer_Province { get; set; }

            public string Customer_District { get; set; }

            public string Customer_Ward { get; set; }

            public string Customer_Address { get; set; }

            public string Shipping_Province { get; set; }

            public string Shipping_District { get; set; }

            public string Shipping_Ward { get; set; }

            public string Customer_Comment { get; set; }

            public string Shipping_Name { get; set; }

            public string Shipping_Phone { get; set; }

            public string Status { get; set; }

            public string PaymentMethod { get; set; }

            public double? InitialActivationFee { get; set; }
        }

        public class dataQuery
        {
            public customer cus { get; set; }
            public int? count { get; set; }
            public int? compl { get; set; }
            public double? TotalAmount { get; set; }
        }
        #endregion
    }
}