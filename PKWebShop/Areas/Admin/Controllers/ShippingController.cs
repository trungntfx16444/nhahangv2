using PKWebShop.Utils;

namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using Newtonsoft.Json;
    using PKWebShop.Models;

    public class ShippingController : ExpiredCheckController
    {
        private WebShopEntities _db = new ();

        // GET: Admin/Shipping
        public ActionResult shippingFee()
        {
            ViewBag.tinhs = Session["tinhs"] = Session["tinhs"] ?? _db.vn_province.OrderBy(p => p.name).ToList();
            var selected = _db.shipping_config.Where(s => s.Active == true).Select(s => s.ProvineId).Distinct().ToList();
            ViewBag.fee_default = _db.shipping_config.FirstOrDefault(s => s.Active == true && string.IsNullOrEmpty(s.ProvineId) && s.Type != "free")?.ShippingFee ?? 0;
            ViewBag.free_ship = _db.shipping_config.FirstOrDefault(s => s.Active == true && s.Type == "free")?.ShippingFee ?? 0;
            return View(selected);
        }

        public JsonResult SelectProvince(List<string> selected)
        {
            try
            {
                var shipfee_default = _db.shipping_config.FirstOrDefault(x => x.Type == "default")?.ShippingFee ?? 0;

                selected ??= new List<string>();
                var provinces = _db.vn_province.Where(p => selected.Contains(p.name)).AsEnumerable().Select(p => new { Id = p.provinceid, Type = p.type, Name = p.name }).OrderByDescending(p => p.Type).ThenBy(p => p.Name).ToList();
                var pIds = provinces.Select(p => p.Id).ToList();
                var exists = _db.shipping_config.Where(s => pIds.Contains(s.ProvineId)).ToList();
                foreach (var e in exists.Where(e => e.Active != true))
                {
                    e.Active = true;
                    e.ShippingFee = e.ShippingFee > 0 ? e.ShippingFee : shipfee_default;
                    _db.Entry(e).State = System.Data.Entity.EntityState.Modified;
                }

                var deactives = _db.shipping_config.Where(s => !pIds.Contains(s.ProvineId) && !string.IsNullOrEmpty(s.ProvineId) && s.Active == true).ToList();
                foreach (var d in deactives)
                {
                    d.Active = false;
                    _db.Entry(d).State = System.Data.Entity.EntityState.Modified;
                }

                foreach (var p in provinces.Where(p => exists.All(e => e.ProvineId != p.Id)))
                {
                    var newp = new shipping_config
                    {
                        Id = AppFunc.NewShortId(),
                        ProvineName = p.Name,
                        ProvineId = p.Id,
                        Active = true,
                        ShippingFee = shipfee_default,
                        DictrictData = string.Empty,
                        Type = "other",
                    };
                    _db.shipping_config.Add(newp);
                }
                _db.SaveChanges();
                return Json(new object[] { true, "Đã cập nhật", provinces });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message, e.ToString() });
            }
        }

        public JsonResult LoadProvince(string id)
        {
            var noithanh = _db.shipping_config.FirstOrDefault(s => s.ProvineId == id && s.Type == "noithanh") ?? new shipping_config();
            var ngoaithanh = _db.shipping_config.FirstOrDefault(s => s.ProvineId == id && s.Type != "noithanh") ?? new shipping_config();
            var district = _db.vn_district.Where(d => d.provinceid == id).OrderBy(d => d.name).ToList();
            return Json(new object[] { true, "Hoàn tất", district, JsonConvert.DeserializeObject<List<string>>(noithanh.DictrictData ?? string.Empty), noithanh.ShippingFee, ngoaithanh.ShippingFee });
        }

        public JsonResult SaveShippingConfig(string ProvinceId, List<string> HuyenNoiThanh, decimal? noithanh_fee, decimal? ngoaithanh_fee)
        {
            try
            {
                var noithanh = _db.shipping_config.FirstOrDefault(s => s.ProvineId == ProvinceId && s.Type == "noithanh") ?? new shipping_config();
                noithanh.ProvineId = ProvinceId;
                noithanh.ProvineName = _db.vn_province.Find(ProvinceId).name;
                noithanh.ShippingFee = noithanh_fee ?? 0;
                noithanh.Active = true;
                noithanh.DictrictData = JsonConvert.SerializeObject(HuyenNoiThanh);
                if (string.IsNullOrEmpty(noithanh.Id))
                {
                    noithanh.Id = AppFunc.NewShortId();
                    noithanh.Type = "noithanh";
                    _db.shipping_config.Add(noithanh);
                }
                else
                {
                    _db.Entry(noithanh).State = System.Data.Entity.EntityState.Modified;
                }

                var ngoaithanh = _db.shipping_config.FirstOrDefault(s => s.ProvineId == ProvinceId && s.Type != "noithanh") ?? new shipping_config();
                ngoaithanh.ShippingFee = ngoaithanh_fee ?? 0;
                ngoaithanh.Type = "other";
                _db.Entry(ngoaithanh).State = System.Data.Entity.EntityState.Modified;

                _db.SaveChanges();
                return Json(new object[] { true, "Lưu hoàn tất" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message, ex.ToString() });
            }
        }

        public JsonResult SaveShippingDefault(decimal? fee)
        {
            try
            {
                var @default = _db.shipping_config.FirstOrDefault(s => string.IsNullOrEmpty(s.ProvineId) && s.Type != "free") ?? new shipping_config();
                @default.ShippingFee = fee ?? 0;
                @default.Active = true;
                @default.Type = "default";
                if (string.IsNullOrEmpty(@default.Id))
                {
                    @default.Id = AppFunc.NewShortId();
                    _db.shipping_config.Add(@default);
                }
                else
                {
                    _db.Entry(@default).State = System.Data.Entity.EntityState.Modified;
                }
                _db.SaveChanges();
                return Json(new object[] { true, "Lưu hoàn tất" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message, ex.ToString() });
            }
        }
        
        public JsonResult SaveShippingFree(decimal? fee)
        {
            try
            {
                var @default = _db.shipping_config.FirstOrDefault(s => s.Type == "free") ?? new shipping_config();
                @default.ShippingFee = fee ?? 0;
                @default.Active = true;
                @default.Type = "free";
                if (string.IsNullOrEmpty(@default.Id))
                {
                    @default.Id = AppFunc.NewShortId();
                    _db.shipping_config.Add(@default);
                }
                else
                {
                    _db.Entry(@default).State = System.Data.Entity.EntityState.Modified;
                }
                _db.SaveChanges();
                return Json(new object[] { true, "Lưu hoàn tất" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message, ex.ToString() });
            }
        }
    }
}