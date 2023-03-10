namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Linq;
    using System.Web.Mvc;
    using PKWebShop.Models;
    using PKWebShop.Utils;

    public class CustomerController : UploadController
    {
        private WebShopEntities db = new ();

        // GET: Admin/Services
        public ActionResult Index()
        {
            return View(db.loyalcustomers.OrderBy(s => s.Order).ToList());
        }

        /// <summary>
        /// Create/Edit loyalcustomer.
        /// </summary>
        /// <param name="id">Loyalcustomer Id.</param>
        /// <returns></returns>
        public ActionResult Save(long? id)
        {
            if (id > 0)
            {
                return View(db.loyalcustomers.Find(id));
            }
            else
            {
                return View(new loyalcustomer());
            }
        }

        [HttpPost]
        [ActionName("Save")]
        public ActionResult SaveSubmit(loyalcustomer cm)
        {
            UploadAttachFile("/upload/images/customer", "pic0", string.Empty, out string logoPicture);
            if (!string.IsNullOrEmpty(cm.Id))
            {
                // update
                try
                {
                    var c = db.loyalcustomers.Find(cm.Id);
                    c.CustomerName = cm.CustomerName;
                    c.URL = cm.URL;
                    c.Order = cm.Order;
                    if (string.IsNullOrWhiteSpace(logoPicture) == false)
                    {
                        c.LogoPicture = logoPicture;
                    }
                    db.Entry(c).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    TempData["success"] = "Cập nhật thành công";
                    return RedirectToAction("save", new { id = cm.Id });
                }
                catch (Exception e)
                {
                    TempData["error"] = e.Message;
                    return RedirectToAction("save", new { id = cm.Id });
                }
            }
            else
            {
                // add new
                try
                {
                    cm.Id = AppFunc.NewShortId();
                    cm.LogoPicture = logoPicture;
                    db.loyalcustomers.Add(cm);
                    db.SaveChanges();
                    TempData["success"] = "Thêm thành công";
                    return RedirectToAction("Index");
                }
                catch (Exception e)
                {
                    TempData["error"] = e.Message;
                    return RedirectToAction("Save");
                }
            }
        }

        /// <summary>
        /// Delete loyalcustomer.
        /// </summary>
        /// <param name="id">Loyalcustomer Id.</param>
        /// <returns></returns>
        public ActionResult Delete(long id)
        {
            try
            {
                var c = db.loyalcustomers.Find(id);
                if (c != null)
                {
                    db.loyalcustomers.Remove(c);
                    db.SaveChanges();
                    TempData["success"] = "Xóa thành công";
                }
                else
                {
                    throw new Exception("Không tìm thấy khách hàng này");
                }
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }

            return RedirectToAction("index");
        }

        public JsonResult PicDelete(long id)
        {
            try
            {
                var c = db.loyalcustomers.Find(id);
                if (c != null && !string.IsNullOrWhiteSpace(c.LogoPicture))
                {
                    // Loc Inactive 20190528
                    // try
                    // {
                    //    FileInfo f = new FileInfo(Server.MapPath(c.LogoPicture));
                    //    if (f.Exists)
                    //    {
                    //        f.Delete();
                    //    }
                    // }
                    // catch (Exception)
                    // {

                    // }
                    c.LogoPicture = null;
                    db.Entry(c).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }

                return Json(true);
            }
            catch (Exception)
            {
                return Json(false);
            }
        }
    }
}