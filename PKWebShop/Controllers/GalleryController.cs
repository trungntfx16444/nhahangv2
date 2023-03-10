namespace PKWebShop.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using PKWebShop.AppLB;
    using PKWebShop.Models;

    public class GalleryController : ExpiredCheckController
    {
        // GET: Gallery
        public ActionResult Index()
        {
            var db = new DBLangCustom();
            string scode = UserContent.Web_Feature.Thuvien_hinhanh.ToString();
            var section = db.sectionfeatures.FirstOrDefault(s => s.ReId == scode);
            var data = db.sectionfeaturedetails.Where(d => d.SectionCode == scode).OrderBy(o => o.VolumeNumber).ToList() ?? new List<sectionfeaturedetail>();
            ViewBag.topbg = CommonFunc.getTopBackground();
            ViewBag.Title = section.Title;
            return View(data);
        }
    }
}