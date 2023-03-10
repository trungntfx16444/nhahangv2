namespace PKWebShop.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using PKWebShop.AppLB;
    using PKWebShop.Models;

    public class CustomerController : ExpiredCheckController
    {
        // GET: Customer
        public ActionResult Profile()
        {
            var db = new DBLangCustom();
            var cus = UserContent.getCurrentCustomer();
            if (cus == null)
            {
                return RedirectToAction("customerLogout", "home");
            }
            ViewBag.orders = db.orders.Where(o => o.CustomerId == cus.Id).OrderByDescending(o => o.CreatedAt).ToList() ?? new List<order>();
            return View(cus);
        }

        public ActionResult getOrderDetail(string id)
        {
            var db = new DBLangCustom();
            var cus = UserContent.getCurrentCustomer();
            if (cus == null)
            {
                return Redirect("/home/CustomerLogout");
            }
            var order = db.orders.FirstOrDefault(o => o.CustomerId == cus.Id && o.Id == id) ?? new order();
            order.PaymentData = db.VNP_PaymentData.Where(pay => pay.OrderId == order.Id && pay.vnp_ResponseCode != "00").ToList().Select(pay => new PaymentData(pay)).FirstOrDefault();
            var detail = db.orders_detail.Where(d => d.OrderId == order.Id)
                .Join(db.products, o => o.ProductId, p => p.ReId, (o, p) => new { o, p })
                .Join(db.categories, item => item.p.CategoryId, c => c.ReId, (item, c) => new { item.o, item.p, c.UrlCode })
                .ToList().Select(d => (d.o, d.UrlCode, d.p.Url)).ToList();
            ViewBag.order = order;

            string listParentProp = string.Join(",", detail.GroupBy(x => x.o.ParentPropertiesId).Select(x => x.Key).Distinct());
            string listChildProp = string.Join(",", detail.GroupBy(x => x.o.PropertiesId).Select(x => x.Key).Distinct());
            var listProp = (from x in db.product_properties
                            where listParentProp.Contains(x.ReId) || listChildProp.Contains(x.ReId)
                            orderby x.name
                            select x).ToList();

            ViewBag.ListProp = listProp;

            return PartialView("_CustomerOrderDetail", detail);
        }

        public JsonResult save(customer cus)
        {
            try
            {
                var db = new DBLangCustom();
                var cur_cus = UserContent.getCurrentCustomer();
                if (cus.Password == cur_cus.Password)
                {
                    if (string.IsNullOrEmpty(cus.FullName.Trim()) || string.IsNullOrEmpty(cus.Phone.Trim()) || string.IsNullOrEmpty(cus.Email.Trim())
                        || string.IsNullOrEmpty(cus.Address.Trim()) || string.IsNullOrEmpty(cus.Province) || string.IsNullOrEmpty(cus.District) || string.IsNullOrEmpty(cus.Ward))
                    {
                        throw new Exception("Vui lòng nhập đầy đủ thông tin");
                    }
                    else if (db.customers.Any(x => x.Email == cus.Email && x.Id != cur_cus.Id))
                    {
                        throw new Exception("Email đã được sử dụng");
                    }
                    cur_cus.Avatar = cus.Avatar;
                    cur_cus.FullName = cus.FullName;
                    cur_cus.Phone = cus.Phone;
                    cur_cus.Email = cus.Email;
                    cur_cus.Province = cus.Province;
                    cur_cus.District = cus.District;
                    cur_cus.Ward = cus.Ward;
                    cur_cus.Address = cus.Address;
                }
                else
                {
                    throw new Exception("Sai mật khẩu!");
                }
                db.Entry(cur_cus).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return Json(new object[] { true, "Lưu thông tin hoàn tất!" });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }
        public JsonResult changepass(string pass, string newpass, string confirm_newpass)
        {
            try
            {
                var cur_cus = UserContent.getCurrentCustomer();
                if (pass != cur_cus.Password)
                {
                    throw new Exception("Mật khẩu cũ không đúng");
                }
                if (confirm_newpass != newpass)
                {
                    throw new Exception("Mật khẩu mới nhập lại không khớp");
                }
                if (!string.IsNullOrEmpty(newpass))
                {
                    if (newpass == confirm_newpass)
                    {
                        var db = new DBLangCustom();
                        cur_cus.Password = newpass;
                        db.Entry(cur_cus).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        throw new Exception("Xác nhận mật khẩu mới không trùng khớp!");
                    }
                }
                return Json(new object[] { true, "Lưu thông tin hoàn tất!" });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }
    }
}