namespace PKWebShop.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using PKWebShop.AppLB;
    using PKWebShop.Models;
    public class UtilitiesController : Controller
    {
        // GET: Utilities
        public ActionResult Index()
        {
            var db = new DBLangCustom();

            var ss = UserContent.Web_Feature.Tienich_chitiet.ToString();
            var ultilities = db.sectionfeaturedetails.Where(s => s.SectionCode == ss).ToList() ?? new List<sectionfeaturedetail>();

            string path = HttpContext.Request.Url.AbsolutePath;
            ViewBag.Title = db.menus.Where(m => m.URL == path).FirstOrDefault()?.Name ?? "Tiện ích";
            ViewBag.topbg = CommonFunc.getTopBackground();
            return View(ultilities);
        }
    }
}