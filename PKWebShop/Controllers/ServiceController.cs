namespace PKWebShop.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using PKWebShop.AppLB;
    using PKWebShop.Models;

    public class ServiceController : ExpiredCheckController
    {
        // GET: Service
        public ActionResult Index(string url)
        {
            DBLangCustom db = new ();
            ViewBag.topbg = CommonFunc.getTopBackground();
            ViewBag.title = db.menus.Where(m => m.URL == "/" + url).FirstOrDefault()?.Name;
            var list_service = db.services.OrderBy(o => o.Order).ToList() ?? new List<service>();
            return View(list_service);
        }

        public ActionResult Detail(string Id, string url)
        {
            DBLangCustom db = new ();
            ViewBag.topbg = CommonFunc.getTopBackground();
            var service = db.services.Where(s => s.ServiceId == Id || s.ReId == Id).FirstOrDefault();
            ViewBag.gallery = db.uploadmorefiles.Where(u => u.TableId == service.ReId && u.TableName == "services").ToList();
            ViewBag.list_services = db.services.OrderBy(o => o.Order).Take(3).ToList() ?? new List<service>();
            return View(service);
        }
    }
}