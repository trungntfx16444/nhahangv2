using PKWebShop.Utils;

namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using Newtonsoft.Json;
    using PKWebShop.AppLB;
    using PKWebShop.Models;
    using PKWebShop.Models.CustomizeModels;
    using PKWebShop.Services;

    [Authorize]
    public class FeatureController : ExpiredCheckController
    {
        // GET: Admin/Feature
        private WebShopEntities _db = new();
        private Dictionary<string, bool> access = Authority.GetAccessAuthority();

        #region SECTION FEATURE
        public ActionResult Index(string lang)
        {
            using (var db = new DBLangCustom())
            {
                db.setLanguage(lang);
                ViewBag.Category = db.categories.OrderBy(x => x.Order).ToList();
                return View(db.sectionfeatures.Where(x => x.Hidden != true).OrderBy(o => o.Order).ToList());
            }
        }

        /// <summary>
        /// load thong tin sectionfeature by code.
        /// </summary>
        /// <param name="code">code.</param>
        /// <returns>json.</returns>
        public JsonResult GetFeatureByCode(string code)
        {
            try
            {
                if (!access.ContainsKey("update_features"))
                {
                    throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin.");
                }

                var result = _db.sectionfeatures.Find(code);
                if (result == null)
                {
                    throw new Exception("Thành phần không tồn tại");
                }
                return Json(new object[] { true, result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// save.
        /// </summary>
        /// <param name="data">.</param>
        /// <returns>json.</returns>
        public JsonResult Save(sectionfeature data)
        {
            try
            {
                if (!access.ContainsKey("update_features"))
                {
                    throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin.");
                }

                var feature = _db.sectionfeatures.Find(data.Code);
                feature.Title = data.Title;
                feature.SubTitle = data.SubTitle;
                feature.Description = data.Description;
                feature.Status = data.Status;
                feature.Url = data.Url;
                _db.Entry(feature).State = System.Data.Entity.EntityState.Modified;
                _db.SaveChanges();
                return Json(new object[] { true, feature }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region SECTION FEATURE DETAIL

        /// <summary>
        /// Get list section feature detail.
        /// </summary>
        /// <param name="code">section feature code.</param>
        /// <returns>Html Partial.</returns>
        public ActionResult FeatureDetail(string code, string lang)
        {
            lang = !string.IsNullOrEmpty(lang) ? lang : SiteLang.GetDefault().Code;
            try
            {
                var section = _db.sectionfeatures.Where(x => x.ReId == code && x.LangCode == lang).FirstOrDefault();
                if (section == null)
                {
                    throw new Exception("Thành phần không tồn tại");
                }
                var data = _db.sectionfeaturedetails.Where(s => s.SectionCode == section.ReId && s.LangCode == section.LangCode).OrderBy(s => s.VolumeNumber).GroupJoin(
                                _db.uploadmorefiles, fd => fd.Id, s => s.TableId, (fd, s) => new { fd, s }).ToList().Select(i => (i.fd, i.s.FirstOrDefault()?.FileName));
                ViewBag.SectionCode = code;
                return PartialView("_FeatureDetail", data);
            }
            catch (Exception ex)
            {
                ViewBag.ErrMsg = ex.Message;
                return PartialView("_FeatureDetail", null);
            }
        }

        /// <summary>
        /// Get info section feature detail.
        /// </summary>
        /// <param name="id">section feature detail Id.</param>
        /// <returns>Json.</returns>
        public JsonResult GetFeatureDetail(string id)
        {
            try
            {
                if (!access.ContainsKey("update_features"))
                {
                    throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin.");
                }

                var data = _db.sectionfeaturedetails.Find(id);
                var images = _db.uploadmorefiles.Where(u => u.TableId == data.Id && u.TableName == "sectionfeaturedetails").ToList();
                var images_partial = CommonFunc.RenderRazorViewToString("_UploadImages", images, this);
                if (data == null)
                {
                    throw new Exception("Nội dung không tồn tại");
                }
                return Json(new object[] { true, data, images_partial });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SaveDetail(sectionfeaturedetail data, List<string> picmore)
        {
            try
            {
                if (!access.ContainsKey("update_features"))
                {
                    throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin.");
                }

                var secFeature = _db.sectionfeatures.Find(data.SectionCode);
                if (secFeature == null)
                {
                    throw new Exception("Thành phần không tồn tại");
                }

                var SEO_Product = UserContent.GetUrlSite(SiteSEO.code_SEO.SanPham);
                var all_sfDetail = _db.sectionfeaturedetails.Where(x => x.SectionCode == secFeature.ReId && x.LangCode == secFeature.LangCode).OrderBy(x => x.VolumeNumber);
                if (string.IsNullOrEmpty(data.Id))
                {
                    // Add new
                    var sfDetail = new sectionfeaturedetail
                    {
                        Id = AppFunc.NewShortId(),
                        SectionCode = secFeature.ReId,
                        SectionName = secFeature.Title,
                        Title = data.Title,
                        SubTitle = data.SubTitle,
                        Description = Request.Unvalidated["Description"],
                        Picture = data.Picture,
                        URL = data.URL,
                        LangCode = data.LangCode ?? SiteLang.GetDefault().Code,
                        VolumeNumber = data.VolumeNumber ?? 1,
                        CreateAt = DateTime.Now,
                    };
                    if (sfDetail.Description?.Contains("sanpham|") == true)
                    {
                        var contents = sfDetail.Description?.Replace("sanpham|", string.Empty).Split('|');
                        sfDetail.URL = $"/{SEO_Product.url}?{contents[0]}=1";
                        sfDetail.Description = string.Empty;
                    }
                    else if (sfDetail.Description?.Contains("danhmuc|") == true)
                    {
                        var contents = sfDetail.Description?.Replace("danhmuc|", string.Empty).Split('|');
                        sfDetail.URL = $"/{contents[0]}";
                        sfDetail.Description = string.Empty;
                    }
                    else if (string.IsNullOrEmpty(sfDetail.URL))
                    {
                        sfDetail.URL = "#";
                    }
                    _db.sectionfeaturedetails.Add(sfDetail);
                    data.Id = sfDetail.Id;

                    // Update lại vị trí - Hàm đệ quy UpdateOrder()
                    UpdateOrder(_db, all_sfDetail, sfDetail.Id, sfDetail.VolumeNumber);
                }
                else
                {
                    // Update
                    var sfDetail = _db.sectionfeaturedetails.Find(data.Id);
                    if (sfDetail == null)
                    {
                        throw new Exception("Nội dung không tồn tại");
                    }
                    var volumeNumber_old = sfDetail.VolumeNumber;

                    sfDetail.Title = data.Title;
                    sfDetail.SubTitle = data.SubTitle;
                    sfDetail.Description = Request.Unvalidated["Description"];
                    sfDetail.Picture = data.Picture;
                    sfDetail.URL = data.URL;
                    sfDetail.VolumeNumber = data.VolumeNumber ?? 1;
                    
                    sfDetail.LangCode = data.LangCode ?? SiteLang.GetDefault().Code;
                    if (sfDetail.Description?.Contains("sanpham|") == true)
                    {
                        var contents = sfDetail.Description?.Replace("sanpham|", string.Empty).Split('|');
                        sfDetail.URL = $"/{SEO_Product.url}?{contents[0]}=1";
                        sfDetail.Description = string.Empty;
                    }
                    else if (sfDetail.Description?.Contains("danhmuc|") == true)
                    {
                        var contents = sfDetail.Description?.Replace("danhmuc|", string.Empty).Split('|');
                        sfDetail.URL = $"/{contents[0]}";
                        sfDetail.Description = string.Empty;
                    }
                    else if (string.IsNullOrEmpty(sfDetail.URL))
                    {
                        sfDetail.URL = "#";
                    }
                    _db.Entry(sfDetail).State = System.Data.Entity.EntityState.Modified;
                    data.Id = sfDetail.Id;

                    var sf_detail = all_sfDetail.FirstOrDefault(x => x.VolumeNumber == sfDetail.VolumeNumber && x.Id != sfDetail.Id);
                    if (sf_detail != null)
                    {
                        sf_detail.VolumeNumber = volumeNumber_old;
                        _db.Entry(sf_detail).State = System.Data.Entity.EntityState.Modified;
                    }
                }

                var more_picture = _db.uploadmorefiles.Where(x => x.TableId == data.Id && x.TableName == "sectionfeaturedetails").ToList();
                if (more_picture.Any())
                {
                    _db.uploadmorefiles.RemoveRange(more_picture);
                }
                if (picmore?.Count > 0)
                {
                    foreach (var item in picmore)
                    {
                        var pic = new uploadmorefile
                        {
                            UploadId = AppFunc.NewShortId(),
                            TableId = data.Id,
                            TableName = "sectionfeaturedetails",
                            FileName = item,
                        };
                        _db.uploadmorefiles.Add(pic);
                    }
                }
                _db.SaveChanges();
                return Json(new object[] { true, "Lưu thành công" });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }

        public JsonResult ChangeOrder(string sectionCode, string id, string lang, bool isPlus = true)
        {
            using (var db = new DBLangCustom())
            {
                
                try
                {
                    db.setLanguage(lang);
                    if (!string.IsNullOrEmpty(sectionCode))
                    {
                        #region CHANGE ORDER FEATURE
                        var sf = db.sectionfeatures.FirstOrDefault(x => x.Code == sectionCode);
                        if (sf == null)
                        {
                            throw new Exception("Nội dung không tồn tại.");
                        }
                        else if (sf.Order == 1 && isPlus == false)
                        {
                            return Json(new object[] { true, string.Empty });
                        }

                        var all_sf = db.sectionfeatures.OrderBy(x => x.Order);
                        if (isPlus == true)
                        {
                            sf.Order++;
                            var sf_ = all_sf.FirstOrDefault(x => x.Order == sf.Order && x.Code != sectionCode);
                            if (sf_ != null)
                            {
                                sf_.Order--;
                                db.Entry(sf_).State = System.Data.Entity.EntityState.Modified;
                            }
                        }
                        else
                        {
                            sf.Order--;
                            var sf_ = all_sf.FirstOrDefault(x => x.Order == sf.Order && x.Code != sectionCode);
                            if (sf_ != null)
                            {
                                sf_.Order++;
                                db.Entry(sf_).State = System.Data.Entity.EntityState.Modified;
                            }
                        }
                        db.Entry(sf).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                        TempData["success"] = "Cập nhật vị trí thành công";
                        return Json(new object[] { true, "Cập nhật vị trí thành công" });
                        #endregion
                    }
                    else if (!string.IsNullOrEmpty(id))
                    {
                        #region CHANGE ORDER FEATURE DETAIL
                        var sfDetail = db.sectionfeaturedetails.FirstOrDefault(x => x.Id == id);
                        if (sfDetail == null)
                        {
                            throw new Exception("Nội dung không tồn tại.");
                        }
                        else if (sfDetail.VolumeNumber == 1 && isPlus == false)
                        {
                            return Json(new object[] { true, string.Empty, string.Empty });
                        }

                        var all_sfDetail = db.sectionfeaturedetails.Where(x => x.SectionCode == sfDetail.SectionCode).OrderBy(x => x.VolumeNumber);
                        if (isPlus == true)
                        {
                            sfDetail.VolumeNumber++;
                            var sf_detail = all_sfDetail.FirstOrDefault(x => x.VolumeNumber == sfDetail.VolumeNumber && x.Id != id);
                            if (sf_detail != null)
                            {
                                sf_detail.VolumeNumber--;
                                db.Entry(sf_detail).State = System.Data.Entity.EntityState.Modified;
                            }
                        }
                        else
                        {
                            sfDetail.VolumeNumber--;
                            var sf_detail = all_sfDetail.FirstOrDefault(x => x.VolumeNumber == sfDetail.VolumeNumber && x.Id != id);
                            if (sf_detail != null)
                            {
                                sf_detail.VolumeNumber++;
                                db.Entry(sf_detail).State = System.Data.Entity.EntityState.Modified;
                            }
                        }
                        db.Entry(sfDetail).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                        return Json(new object[] { true, "Cập nhật vị trí thành công", sfDetail.SectionCode });
                        #endregion
                    }
                    return Json(new object[] { true, string.Empty });
                }
                catch (Exception ex)
                {
                    return Json(new object[] { false, ex.Message?.Replace("\n", ". ") });
                }
            }
        }

        public void UpdateOrder(WebShopEntities db, IQueryable<sectionfeaturedetail> all_sfDetail, string id, int? order)
        {
            var sf_detail = all_sfDetail.FirstOrDefault(x => x.VolumeNumber == order && x.Id != id);
            if (sf_detail != null)
            {
                sf_detail.VolumeNumber++;
                db.Entry(sf_detail).State = System.Data.Entity.EntityState.Modified;
                UpdateOrder(db, all_sfDetail, sf_detail.Id, sf_detail.VolumeNumber);
            }
        }

        /// <summary>
        /// Delete feature detail.
        /// </summary>
        /// <param name="id">feature detail Id.</param>
        /// <returns>Json.</returns>
        public JsonResult DeleteDetail(string id)
        {
            try
            {
                if (!access.ContainsKey("update_features"))
                {
                    throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin.");
                }

                var sfDetail = _db.sectionfeaturedetails.Find(id);
                if (sfDetail == null)
                {
                    throw new Exception("Nội dung không tồn tại");
                }
                _db.sectionfeaturedetails.Remove(sfDetail);
                _db.SaveChanges();
                return Json(new object[] { true, "Xóa thành công" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        public ActionResult Detail(string lang)
        {
            string dichvu = UserContent.Web_Feature.trangchu_dichvu.ToString();
            string tintuc = UserContent.Web_Feature.trangchu_tintuc.ToString();
            using (var db = new DBLangCustom())
            {
                db.setLanguage(lang);
                var data = db.sectionfeatures.Where(s => s.Status == true && s.ReId != dichvu && s.ReId != tintuc).OrderBy(s => s.Code).ToList();
                return View(data);
            }
        }
    }
}