namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Web.Mvc;
    using PKWebShop.Models;
    using PKWebShop.Utils;

    public class SupportPolicyController : UploadController
    {
        private WebShopEntities db = new ();

        // GET: Admin/Support

        /// <summary>
        /// huơng dan dat hang.
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View(db.policies.FirstOrDefault());
        }

        [HttpPost]
        public ActionResult OrderPoSubmit()
        {
            try
            {
                UploadAttachFile("/upload/images/order", "pic", string.Empty, out string picture);
                var support = db.policies.FirstOrDefault();
                if (support == null)
                {
                    support = new policy
                    {
                        OrderPolicyImage = picture,
                        OrderPolicy = Request.Unvalidated["desc"],
                    };
                    db.policies.Add(support);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(picture) == false)
                    {
                        support.OrderPolicyImage = picture;
                    }
                    support.OrderPolicy = Request.Unvalidated["desc"];
                    db.Entry(support).State = System.Data.Entity.EntityState.Modified;
                }
                db.SaveChanges();

                TempData["success"] = "Lưu thành công";
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }
            return RedirectToAction("index");
        }

        /// <summary>
        /// Huong dan thanh toan.
        /// </summary>
        /// <returns></returns>
        public ActionResult PaymentPo()
        {
            return View(db.policies.FirstOrDefault());
        }

        [HttpPost]
        public ActionResult PaymentPoSubmit()
        {
            try
            {
                UploadAttachFile("/upload/images/payment", "pic", string.Empty, out string picture);
                var support = db.policies.FirstOrDefault();
                if (support == null)
                {
                    support = new policy
                    {
                        PaymentPolicyImage = picture,
                        PaymentPolicy = Request.Unvalidated["desc"],
                    };
                    db.policies.Add(support);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(picture) == false)
                    {
                        support.PaymentPolicyImage = picture;
                    }
                    support.PaymentPolicy = Request.Unvalidated["desc"];
                    db.Entry(support).State = System.Data.Entity.EntityState.Modified;
                }
                db.SaveChanges();

                TempData["success"] = "Lưu thành công";
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }
            return RedirectToAction("paymentpo");
        }

        /// <summary>
        /// Chinh sach khach hang.
        /// </summary>
        /// <returns></returns>
        public ActionResult CustomerPolicy()
        {
            return View(db.policies.FirstOrDefault());
        }

        [HttpPost]
        public ActionResult CusPoSubmit()
        {
            try
            {
                UploadAttachFile("/upload/images/cus", "pic", string.Empty, out string picture);
                var support = db.policies.FirstOrDefault();
                if (support == null)
                {
                    support = new policy
                    {
                        Id = AppFunc.NewShortId(),
                        CustomerPolicyImage = picture,
                        CustomerOrChangePolicy = Request.Unvalidated["desc"],
                    };
                    db.policies.Add(support);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(picture) == false)
                    {
                        support.CustomerPolicyImage = picture;
                    }
                    support.CustomerOrChangePolicy = Request.Unvalidated["desc"];
                    db.Entry(support).State = System.Data.Entity.EntityState.Modified;
                }
                db.SaveChanges();

                TempData["success"] = "Lưu thành công";
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }
            return RedirectToAction("CustomerPolicy");
        }

        /// <summary>
        /// Cach thuc giao hang, doi tra.
        /// </summary>
        /// <returns></returns>
        public ActionResult Ship()
        {
            return View(db.policies.FirstOrDefault());
        }

        [HttpPost]
        public ActionResult ShipPoSubmit()
        {
            try
            {
                UploadAttachFile("/upload/images/ship", "pic", string.Empty, out string picture);
                var support = db.policies.FirstOrDefault();
                if (support == null)
                {
                    support = new policy
                    {
                        Id = AppFunc.NewShortId(),
                        ShipPolicyImage = picture,
                        ShipPolicy = Request.Unvalidated["desc"],
                    };
                    db.policies.Add(support);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(picture) == false)
                    {
                        support.ShipPolicyImage = picture;
                    }
                    support.ShipPolicy = Request.Unvalidated["desc"];
                    db.Entry(support).State = System.Data.Entity.EntityState.Modified;
                }
                db.SaveChanges();

                TempData["success"] = "Lưu thành công";
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }
            return RedirectToAction("ship");
        }

        public JsonResult PicDelete(string t)
        {
            try
            {
                var support = db.policies.FirstOrDefault();
                switch (t)
                {
                    case "order":
                        try
                        {
                            FileInfo f = new (Server.MapPath(support.OrderPolicyImage));
                            if (f.Exists)
                            {
                                f.Delete();
                            }
                        }
                        catch (Exception)
                        {
                        }
                        support.OrderPolicyImage = null;
                        break;
                    case "payment":
                        try
                        {
                            FileInfo f = new (Server.MapPath(support.PaymentPolicyImage));
                            if (f.Exists)
                            {
                                f.Delete();
                            }
                        }
                        catch (Exception)
                        {
                        }
                        support.PaymentPolicyImage = null;
                        break;
                    case "customer":
                        try
                        {
                            FileInfo f = new (Server.MapPath(support.CustomerPolicyImage));
                            if (f.Exists)
                            {
                                f.Delete();
                            }
                        }
                        catch (Exception)
                        {
                        }
                        support.CustomerPolicyImage = null;
                        break;
                    case "ship":
                        try
                        {
                            FileInfo f = new (Server.MapPath(support.OrderPolicyImage));
                            if (f.Exists)
                            {
                                f.Delete();
                            }
                        }
                        catch (Exception)
                        {
                        }
                        support.ShipPolicyImage = null;

                        break;
                    default:
                        break;
                }
                db.Entry(support).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return Json(true);
            }
            catch (Exception)
            {
                return Json(false);
            }
        }
    }
}