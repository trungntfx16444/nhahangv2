namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Linq;
    using System.Web.Mvc;
    using PKWebShop.Models;
    using PKWebShop.Utils;

    public class AboutUsController : ExpiredCheckController
    {
        private readonly WebShopEntities _db = new ();
        private Random rd = new ();

        // GET: Admin/AboutUs
        public ActionResult Index()
        {
            return View(_db.introduces.FirstOrDefault());
        }

        [HttpPost]
        public ActionResult Update()
        {
            try
            {
                var introduce = _db.introduces.FirstOrDefault();
                if (introduce != null)
                {
                    // update.
                    introduce.Content = Request.Unvalidated["IntrodueContent"];
                    _db.Entry(introduce).State = System.Data.Entity.EntityState.Modified;
                }
                else
                {
                    // create.
                    introduce = new introduce
                    {
                        IntroduceId = AppFunc.NewShortId(),
                        Content = Request.Unvalidated["IntrodueContent"],
                    };
                    _db.introduces.Add(introduce);
                }

                _db.SaveChanges();
                TempData["success"] = "Cập nhật thành công";
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }
            return RedirectToAction("index");
        }
    }
}