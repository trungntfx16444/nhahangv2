using Newtonsoft.Json;
using PKWebShop.AppLB;
using PKWebShop.Areas.Admin.CustomizeModel;
using PKWebShop.Models;
using PKWebShop.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web.Mvc;
using PKWebShop.Utils;

namespace PKWebShop.Areas.Admin.Controllers
{
    [Authorize]
    public class QcaoController : UploadController
    {
        WebShopEntities _db = new WebShopEntities();

        public ActionResult Index()
        {
            DateTime from = DateTime.Now.AddMonths(-6);
            ViewBag.From = new DateTime(from.Year, from.Month, 1).ToString("yyyy-MM-dd");
            return View();
        }

        public ActionResult Popup()
        {
            var popupAds = _db.popupads.FirstOrDefault();
            if (popupAds == null)
            {
                popupAds = new popupad { };
            }
            return View(popupAds);
        }

        public JsonResult Load_table(DataTable_request data, DateTime? From, string search)
        {
            From = DateTime.Parse($"{From:dd/MM/yyyy} 0:0:0");

            var langCodeDefault = SiteLang.GetDefault()?.Code ?? "vi";
            var recordsTotal = _db.popupads.Where(x => x.LangCode == langCodeDefault).Count();
            IQueryable<popupad> adsQuery;

            if (!string.IsNullOrEmpty(search))
            {
                var sqlCommand = CommonFunc.SearchCommand("popupads", search, "Title");
                adsQuery = _db.popupads.SqlQuery(sqlCommand).Where(x => x.PopupAdsFrom >= From && x.LangCode == langCodeDefault).AsQueryable();
            }
            else
            {
                adsQuery = _db.popupads.Where(x => x.PopupAdsFrom >= From && x.LangCode == langCodeDefault).AsQueryable();
            }

            var filtered_count = adsQuery.Count();
            string[] orderColumns = { "Title", "PopupAdsFrom", "Description", null, "Active", null };
            var orderColumn = orderColumns[data.order?.FirstOrDefault()?.column ?? 1] ?? "PopupAdsFrom";

            var list_ads = adsQuery.OrderBy(orderColumn + " " + data.order?.FirstOrDefault().dir).Skip(data.start).Take(data.length).ToList();
            var html = CommonFunc.RenderRazorViewToString("_tableData", list_ads, this);
            return Json(new { draw = data.draw, recordsFiltered = filtered_count, recordsTotal = recordsTotal, data = html });
        }

        public ActionResult Save(string id, string lang)
        {
            try
            {
                lang = string.IsNullOrEmpty(lang) ? SiteLang.GetDefault().Code : lang;
                ViewBag.langCode = lang;
                popupad ads = null;
                if (string.IsNullOrEmpty(id))
                {
                    ads = new popupad()
                    {
                        LangCode = "vi",
                        Active = true,
                    };
                }
                else
                {
                    ads = _db.popupads.FirstOrDefault(n => n.ReId == id && n.LangCode == lang);
                    if (ads == null)
                    {
                        throw new Exception("Không tìm thấy popup quảng cáo");
                    }
                }
                return View(ads);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("index");
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Save(popupad nm)
        {
            try
            {
                popupad saved = Ads.Save(nm);
                TempData["success"] = "Lưu thành công";

                return RedirectToAction("index");
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
                return RedirectToAction("save", new { id = nm.ReId, lang = nm.LangCode });
            }
        }

        public ActionResult Delete(string id)
        {
            try
            {
                var ads = _db.popupads.Where(x => x.ReId == id).ToList();
                if (ads.Count > 0)
                {
                    _db.popupads.RemoveRange(ads);
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, "Có lỗi xảy ra !" });
            }
            return Json(new object[] { true, "Quảng cáo đã được xoá !" });
        }
    }
}