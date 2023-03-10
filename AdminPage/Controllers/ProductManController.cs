namespace AdminPage.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Web.Mvc;
    using Newtonsoft.Json;
    using AdminPage.AppLB;
    using AdminPage.Models.CustomizeModels;
    using AdminPage.Models;
    using AdminPage.Services;
    using AdminPage.Utils;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using K4os.Compression.LZ4.Internal;

    [Authorize]
    public class ProductManController : UploadController
    {
        // GET: Admin/ProductMan
        AdminEntities _db = new AdminEntities();
        const string session_price = "session_product_price";
        private Dictionary<string, bool> access = Authority.GetAccessAuthority();
        private SiteLang.Language default_language = SiteLang.GetDefault();

        public ActionResult Index()
        {
            var listCate = _db.categories.Where(x => x.LangCode == default_language.Code).OrderBy(x => x.Order).ToList();
            return View(listCate);
        }

        public JsonResult Load_table(DataTable_request data, string category, string search, string stock_status)
        {
            var recordsTotal = _db.products.Where(x => x.LangCode == default_language.Code && x.IsActive != false).Count();
            IQueryable<product> orderQuery = _db.products.Where(x => x.LangCode == default_language.Code && x.IsActive != false);

            //var listCate = _db.categories.Where(c => c.Id == category || c.ReId == category).Select(c => c.Id).AsEnumerable<string>();

            if (!string.IsNullOrEmpty(search))
            {
                var slqCommand = CommonFunc.SearchCommand("products", search, "Id", "ProductName");
                orderQuery = _db.products.SqlQuery(slqCommand).Where(x => x.LangCode == default_language.Code && x.IsActive != false).AsQueryable();
            }
            if (!string.IsNullOrEmpty(category))
            {
                //var cateId = _db.categories.FirstOrDefault(c => c.ReId == category)?.Id;
                //var listCate = Products.GetAllChildCategory(cateId);
                orderQuery = orderQuery.Where(x => x.CategoryId.Contains(category));
            }
            if (!string.IsNullOrEmpty(stock_status))
            {
                switch (stock_status)
                {
                    case "empty":
                        orderQuery = orderQuery.Where(o => o.Quantity == 0); break;
                    case "low":
                        orderQuery = orderQuery.Where(o => o.WarningQuantity >= o.Quantity); break;
                    case "stocking":
                        orderQuery = orderQuery.Where(o => o.WarningQuantity == null || o.WarningQuantity < o.Quantity); break;
                }
            }
            var filtered_count = orderQuery.Count();
            string[] orderColumns = { "Id", "ProductName", null, "Price", "ShowHomePage", null };
            var orderColumn = orderColumns[data.order?.FirstOrDefault()?.column ?? 1] ?? "Price";

            var listOrder = orderQuery.OrderBy($"{orderColumn} {data.order?.FirstOrDefault().dir}").Skip(data.start).Take(data.length).ToList();
            var html = CommonFunc.RenderRazorViewToString("_ProductDataTable", listOrder, this);

            return Json(new { draw = data.draw, recordsFiltered = filtered_count, recordsTotal = recordsTotal, data = html });
        }

        public JsonResult LoadCategoryByLangCode(string langCode, string cateReId)
        {
            try
            {
                var listCate = _db.categories.Where(x => x.LangCode == langCode).OrderBy(x => x.Order).ToList();
                return Json(new object[] { true, listCate, cateReId });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, null, ex.Message });
            }
        }

        public ActionResult LoadProductProperties(string[] parent_properties, string[] properties, string langCode)
        {
            var listProp = new List<product_properties>();
            try
            {
                if (parent_properties != null && parent_properties.Count() > 0)
                {
                    langCode = string.IsNullOrEmpty(langCode) ? default_language.Code : langCode;
                    listProp = (from x in _db.product_properties
                                where (parent_properties.Contains(x.ReId) && x.is_parrent == true && x.LangCode == langCode)
                                || (properties.Contains(x.ReId) && x.is_parrent == false && x.LangCode == langCode)
                                orderby x.name
                                select x).ToList();
                }
            }
            catch (Exception ex)
            {
                ViewBag.PropErr = ex.Message;
            }
            return PartialView("_PropertyPartial", listProp);
        }

        #region PRODUCT
        public ActionResult Save(string id, string lang)
        {
            try
            {
                Session.Remove(session_price);
                if (!access.ContainsKey("update_product"))
                {
                    throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin");
                }

                var listLang = SiteLang.GetListLangs();
                ViewBag.listLangs = listLang;

                if (string.IsNullOrEmpty(lang))
                {
                    lang = listLang.FirstOrDefault()?.Code;
                }

                ViewBag.Title = "Thêm sản phẩm";
                var pro = new product() { IsActive = true, LangCode = lang, Sellable = true };
                if (!string.IsNullOrEmpty(id))
                {
                    ViewBag.Title = "Cập nhật sản phẩm";
                    pro = _db.products.Where(x => x.ReId == id && x.IsActive != false && x.LangCode == lang).FirstOrDefault();
                    if (pro == null)
                    {
                        throw new Exception("Không tìm thấy sản phẩm");
                    }
                    ViewBag.uploadFiles = _db.uploadmorefiles.Where(f => f.TableId == pro.ReId && f.TableName.Equals("products")).ToList();
                }

                ViewBag.ListProperty = _db.product_properties.Where(x => x.is_parrent == true && x.active == true && x.LangCode == lang).OrderBy(x => x.name).ToList();

                ViewBag.tags_auto = JsonConvert.SerializeObject(_db.tags.Select(x => x.TagName).ToList());
                ViewBag.vendors = new SelectList(_db.vendors.Where(x => x.Active == true).OrderBy(x => x.Name).Select(s => new { s.Id, s.Name }), "Id", "Name", pro.VendorId).Prepend(new SelectListItem { Text = "-- Chọn nhà cung cấp --", Value = string.Empty });
                LoadOptionPrice(id);
                LoadProperties(id);
                return View(pro);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("index");
            }
        }

        public ActionResult _Save(string id, string lang)
        {
            Session.Remove(session_price);
            lang = string.IsNullOrEmpty(lang) ? default_language.Code : lang;
            var pro = new product() { IsActive = true, LangCode = lang };
            if (!string.IsNullOrEmpty(id))
            {
                pro = _db.products.Where(x => x.ReId == id && x.IsActive != false && x.LangCode == (lang ?? default_language.Code)).FirstOrDefault();
                ViewBag.uploadFiles = pro != null ? _db.uploadmorefiles.Where(f => f.TableId == pro.ReId && f.TableName.Equals("products")).ToList() : new List<uploadmorefile>();
            }

            ViewBag.ListProperty = _db.product_properties.Where(x => x.is_parrent == true && x.active == true && x.LangCode == lang).OrderBy(x => x.name).ToList();

            ViewBag.tags_auto = JsonConvert.SerializeObject(_db.tags.Select(x => x.TagName).ToList());
            ViewBag.vendors = new SelectList(_db.vendors.Where(x => x.Active == true).OrderBy(x => x.Name).Select(s => new { s.Id, s.Name }), "Id", "Name", pro.VendorId).Prepend(new SelectListItem { Text = "-- Chọn nhà cung cấp --", Value = string.Empty });
            LoadOptionPrice(id);
            LoadProperties(id);
            return PartialView("_save", pro);
        }
        public void LoadOptionPrice(string ProductId)
        {
            var ListOpt = _db.product_option.Where(p => p.ProductId == ProductId && string.IsNullOrEmpty(p.parentId))
                .Select(p => new { parent = p, listoption = _db.product_option.Where(o => o.parentId == p.id).ToList() }).ToList()
                .Select(x => { x.parent.ListOption = x.listoption; return x.parent; }).ToList();
            ViewBag.ListOpt = ListOpt;
            var options_ids = ListOpt.FirstOrDefault()?.ListOption?.Select(x => x.id).ToList() ?? new();
            ViewBag.ListOptPrice = _db.product_option_value.Where(p => options_ids.Any(id => p.option1 == id)).ToList();
        }
        public void LoadProperties(string ProductId)
        {
            ViewBag.propertiy_imgs = JsonConvert.SerializeObject(_db.uploadmorefiles.Where(x => x.TableId.StartsWith(ProductId + "|") && x.TableName == "Product_property")
                .Select(x => new { id = x.TableId.Replace(ProductId + "|", string.Empty), value = x.FileName })) ?? "[]";
        }
        [HttpPost]
        public ActionResult Save(product p, List<string> picmore, List<product_option> optionPrices, product_option_value[] optionPricesData, string list_pic_properties)
        {
            if (!access.ContainsKey("update_product"))
            {
                TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                return RedirectToAction("index");
            }
            try
            {
                if (string.IsNullOrEmpty(p.ProductName))
                {
                    throw new Exception("Tên sản phẩm không được trống");
                }

                p.Price = string.IsNullOrEmpty(Request["Price"]) ? 0 : double.Parse(Request["Price"].Replace(".", string.Empty));
                p.SalePrice = string.IsNullOrEmpty(Request["SalePrice"]) ? 0 : int.Parse(Request["SalePrice"].Replace(".", string.Empty));
                p.ShortDescription = Request.Unvalidated["ShortDesc"];
                p.Description = Request.Unvalidated["desc"];
                p.CategoryId = Request["CategoryId"];
                //p.SEO_Title = string.IsNullOrEmpty(p.SEO_Title) ? p.ProductName : p.SEO_Title;
                //p.SEO_Desc = string.IsNullOrEmpty(p.SEO_Desc) ? p.ShortDescription : p.SEO_Desc;

                // CREATE OR UPDATE PRODUCT
                var product = Services.Products.SaveProduct(p, Request["id12char"]);



                #region UPLOAD MORE PICTURE
                var more_picture = _db.uploadmorefiles.Where(x => x.TableId == product.ReId && x.TableName == "products").ToList();
                if (more_picture.Count() > 0)
                {
                    _db.uploadmorefiles.RemoveRange(more_picture);
                    _db.SaveChanges();
                }
                if (picmore?.Count > 0)
                {
                    foreach (var item in picmore)
                    {
                        var pic = new uploadmorefile
                        {
                            UploadId = AppFunc.NewShortId(),
                            TableId = product.ReId,
                            TableName = "products",
                            FileName = item,//CommonFunc.converttojpg(item),
                        };
                        _db.uploadmorefiles.Add(pic);
                        //CommonFunc.create_mini_images(pic.FileName);
                    }
                }

                if (more_picture.Count > 0 || picmore?.Count > 0)
                {
                    _db.SaveChanges();
                }
                #endregion

                #region OPTION PRICE
                Services.Products.UpdatePropertyImgs(product.ReId, product.LangCode, list_pic_properties);
                Services.Products.AddOptionPrice(product.ReId, optionPrices, optionPricesData);

                #endregion
                TempData["success"] = "Lưu thành công";
                return RedirectToAction("detail", new { id = product.ReId, lang = p.LangCode });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("save", new { id = p.ReId, lang = p.LangCode });
            }
        }

        public ActionResult Detail(string id, string lang)
        {
            try
            {
                lang = string.IsNullOrEmpty(lang) ? default_language.Code : lang;
                var pro = _db.products.Where(x => x.ReId == id && x.IsActive != false && x.LangCode == lang).FirstOrDefault();
                if (pro == null)
                {
                    throw new Exception("Không tìm thấy sản phẩm.");
                }
                ViewBag.ListLangs = SiteLang.GetListLangs();
                ViewBag.uploadFiles = _db.uploadmorefiles.Where(f => f.TableId == pro.ReId && f.TableName == "products").ToList();
                ViewBag.ListPriceOpt = _db.productoptionprices.Where(x => x.ProductId == pro.ReId && x.LangCode == lang && x.IsActive != false).ToList();
                LoadOptionPrice(id);
                ViewBag.propertiy_imgs = _db.uploadmorefiles.Where(x => x.TableId.StartsWith(id + "|") && x.TableName == "Product_property")
                    .ToList();
                string[] parent_properties = pro.list_parent_properties?.Split(',') ?? new string[0];
                string[] properties = pro.list_properties?.Split(',') ?? new string[0] ;
                if (parent_properties != null && parent_properties.Count() > 0)
                {
                    ViewBag.listProp = (from x in _db.product_properties
                                        where (parent_properties.Contains(x.ReId) && x.is_parrent == true && x.LangCode == pro.LangCode)
                                        || (properties.Contains(x.ReId) && x.is_parrent == false && x.LangCode == pro.LangCode)
                                        orderby x.name
                                        select x).ToList();
                }
                return View(pro);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("index");
            }
        }

        public ActionResult Delete(string Id)
        {
            if (!access.ContainsKey("update_product"))
            {
                TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                return RedirectToAction("index");
            }

            try
            {
                var list_product = _db.products.Where(x => x.ReId == Id).ToList();
                if (list_product != null || list_product.Count > 0)
                {
                    foreach (var item in list_product)
                    {
                        item.IsActive = false;
                        _db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                    }
                    _db.SaveChanges();
                    TempData["success"] = "Xóa thành công.";
                }
                else
                {
                    throw new Exception("Không tìm thấy sản phẩm.");
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
            return RedirectToAction("index");
        }

        #endregion

        #region CATEGORY UPGRADE - LOC UPDATE 22/07/2021
        public ActionResult Category(string langCode)
        {
            try
            {
                var listLanguage = SiteLang.GetListLangs();
                langCode = string.IsNullOrEmpty(langCode) ? listLanguage.FirstOrDefault()?.Code : langCode;
                var listCateByLang = _db.categories.Where(x => x.LangCode == langCode).OrderBy(x => x.Order).ToList();
                var listVendor = _db.vendors.Where(x => x.Active == true).ToList();
                ViewBag.LangCode = langCode;
                ViewBag.ListLang = listLanguage;
                ViewBag.ListVendor = listVendor;
                return View(listCateByLang);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("index", "home");
            }
        }
        public ActionResult Category2(string langCode)
        {
            try
            {
                var listLanguage = SiteLang.GetListLangs();
                langCode = string.IsNullOrEmpty(langCode) ? listLanguage.FirstOrDefault()?.Code : langCode;
                var listCateByLang = _db.categories.Where(x => x.LangCode == langCode).OrderBy(x => x.Order).ToList();
                var listVendor = _db.vendors.Where(x => x.Active == true).ToList();
                ViewBag.LangCode = langCode;
                ViewBag.ListLang = listLanguage;
                ViewBag.ListVendor = listVendor;
                return View(listCateByLang);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("index", "home");
            }
        }

        public JsonResult LoadCateParent(string langCode)
        {
            try
            {
                var listLanguage = SiteLang.GetListLangs();
                if (string.IsNullOrEmpty(langCode))
                {
                    langCode = listLanguage.FirstOrDefault()?.Code;
                }
                var listcate = _db.categories.Where(x => (x.Level == 1 || x.Level == 2) && x.LangCode == langCode).OrderBy(x => x.Order).Select(x => new { x.ReId, x.CategoryName, x.ParentId, x.Level }).ToList();
                return Json(new object[] { true, listcate });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        public JsonResult GetCateDetail(string reId, string langCode)
        {
            try
            {
                if (string.IsNullOrEmpty(reId))
                {
                    throw new Exception("Không tìm thấy danh mục này");
                }

                langCode = string.IsNullOrEmpty(langCode) ? SiteLang.GetDefault()?.Code : langCode;
                var cate = _db.categories.FirstOrDefault(x => x.ReId == reId && x.LangCode == langCode);
                if (cate == null)
                {
                    throw new Exception("Không tìm thấy danh mục này");
                }

                var images = _db.uploadmorefiles.Where(u => u.TableId == cate.Id && u.TableName == "categories").ToList();
                var images_partial = CommonFunc.RenderRazorViewToString("_UploadImages", images, this);

                return Json(new object[] { true, cate, images_partial });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }
        Random R = new();
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SaveCate(category data, string[] picmore)
        {
            using (var trans = _db.Database.BeginTransaction())
            {
                
                try
                {
                    if (!access.ContainsKey("update_product"))
                    {
                        throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin");
                    }
                    if (data.Order != null)
                    {
                        int i = 1;
                        _db.categories.Where(x => x.ParentId == data.ParentId && x.ReId != data.ReId && x.LangCode == data.LangCode).OrderBy(x => x.Order).ToList().ForEach(x =>
                        {
                            if (i == data.Order) i++;
                            if (x.Order != i)
                            {
                                x.Order = i;
                                _db.Entry(x).State = System.Data.Entity.EntityState.Modified;
                            };
                            i++;
                        });
                        if (data.Order > i)
                        {
                            data.Order = i;
                        }
                    }




                    data.LangCode = string.IsNullOrEmpty(data.LangCode) ? SiteLang.GetDefault().Code : data.LangCode;
                    string cateId = string.Empty;
                    if (string.IsNullOrEmpty(data.ReId))
                    {
                        #region ADD NEW
                        string new_reId = AppFunc.NewShortId();
                        foreach (var item in SiteLang.GetListLangs())
                        {
                            var result = CommonFunc.Translate(data.CategoryName, null, data.LangCode, item.Code).FirstOrDefault();
                            if (result.Key != string.Empty)
                            {
                                throw new Exception(result.Key);
                            }

                            var cate = new category
                            {
                                Id = AppFunc.NewShortId(),
                                CategoryName = result.Value.First(),
                                SubTitle = data.SubTitle,
                                Description = Request.Unvalidated["Description"],
                                VendorId = Request["VendorId"],
                                Level = 1,
                                Order = data.Order == null ? ((_db.categories?.Max(x => x.Order) ?? 0) + 1) : data.Order,
                                MetaTitle = data.MetaTitle,
                                MetaDesc = data.MetaDesc,
                                MetaKeyWord = data.MetaKeyWord,
                                Active = data.Active ?? false,
                                Sellable = data.Sellable ?? false,
                                LangCode = item.Code,
                                ReId = new_reId,
                            };
                            cateId = cate.Id;
                            if (!string.IsNullOrEmpty(data.ParentId))
                            {
                                var cateParent = _db.categories.FirstOrDefault(x => x.ReId == data.ParentId && x.LangCode == item.Code);
                                if (cateParent != null)
                                {
                                    cate.Level = cateParent.Level + 1;
                                    cate.ParentId = cateParent.ReId;
                                    cate.ParentName = cateParent.CategoryName;
                                }
                            }

                            cate.UrlCode = CommonFunc.ConvertNonUnicodeURL(cate.CategoryName);
                            _db.categories.Add(cate);
                        }
                        #endregion
                        TempData["success"] = "Thêm danh mục thành công";
                    }
                    else
                    {
                        #region UPDATE
                        var cate = _db.categories.FirstOrDefault(x => x.ReId == data.ReId && x.LangCode == data.LangCode);
                        if (cate == null)
                        {
                            throw new Exception("Không tìm thấy danh mục");
                        }
                        cateId = cate.Id;
                        cate.CategoryName = data.CategoryName;
                        cate.SubTitle = data.SubTitle;
                        cate.Description = Request.Unvalidated["Description"];
                        cate.VendorId = Request["VendorId"];
                        cate.Level = 1;
                        cate.Order = data.Order;
                        cate.MetaTitle = data.MetaTitle;
                        cate.MetaDesc = data.MetaDesc;
                        cate.MetaKeyWord = data.MetaKeyWord;
                        cate.Active = data.Active ?? false;
                        cate.Sellable = data.Sellable ?? false;
                        cate.ParentId = null;
                        cate.ParentName = null;
                        cate.UrlCode = CommonFunc.ConvertNonUnicodeURL(cate.CategoryName);

                        if (!string.IsNullOrEmpty(data.ParentId))
                        {
                            var cateParent = _db.categories.FirstOrDefault(x => x.ReId == data.ParentId && x.LangCode == data.LangCode);
                            if (cateParent != null)
                            {
                                cate.Level = cateParent.Level + 1;
                                cate.ParentId = cateParent.ReId;
                                cate.ParentName = cateParent.CategoryName;
                            }
                        }
                        _db.Entry(cate).State = System.Data.Entity.EntityState.Modified;

                        // update lai category cho product
                        foreach (var item in _db.products.Where(x => x.CategoryId == cate.ReId && x.IsActive != false).ToList())
                        {
                            item.CategoryName = _db.categories.FirstOrDefault(x => x.ReId == cate.ReId && x.LangCode == item.LangCode)?.CategoryName;
                            item.Sellable = cate.Sellable ?? false;
                            _db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                        }
                        #endregion
                        TempData["success"] = "Cập nhật danh mục thành công";
                    }

                    

                    UploadMoreFiles(cateId, "categories", picmore);
                    _db.SaveChanges();
                    trans.Commit();

                    // cap nhat lai session
                    UserContent.GetWebCategoryProduct(true);
                }
                catch (Exception ex)
                {
                    trans.Dispose();
                    TempData["error"] = ex.Message;
                }
                return Redirect("/productman/category?langcode=" + data.LangCode);
            }
        }

        public JsonResult DeleteCate(string reId, string langCode)
        {
            using (var tran = _db.Database.BeginTransaction())
            {
                try
                {
                    var cateDel = _db.categories.Where(x => x.ReId == reId).ToList();
                    if (cateDel == null || cateDel.Count == 0)
                    {
                        throw new Exception("Danh mục sản phẩm không tồn tại");
                    }

                    //if (_db.categories.Any(x => x.ParentId == cateDel.Id))
                    //{
                    //    throw new Exception("Vui lòng xóa các danh mục con trước khi xóa danh mục cha");
                    //}

                    var listCateDel = new List<category>();
                    foreach (var item in cateDel)
                    {
                        listCateDel.Add(item);
                        var childs = _db.categories.Where(x => x.ParentId == item.ReId).ToList();
                        if (childs != null && childs.Count > 0)
                        {
                            foreach (var child in childs)
                            {
                                listCateDel.Add(child);
                                var last_childs = _db.categories.Where(x => x.ParentId == child.ReId).ToList();
                                if (last_childs != null && last_childs.Count > 0)
                                {
                                    listCateDel.AddRange(last_childs);
                                }
                            }
                        }
                    }

                    var listCateDel_ReId = listCateDel.Select(c => c.ReId);
                    foreach (var item in _db.products.Where(x => listCateDel_ReId.Contains(x.CategoryId)).ToList())
                    {
                        item.CategoryId = string.Empty;
                        item.CategoryName = string.Empty;
                        _db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                    }

                    _db.categories.RemoveRange(listCateDel);
                    _db.SaveChanges();
                    tran.Commit();

                    // cap nhat lai session
                    //UserContent.GetWebCategoryProduct(true);
                    TempData["success"] = "Xóa thành công";
                    return Json(new object[] { true, "Xóa thành công" });
                }
                catch (Exception ex)
                {
                    tran.Dispose();
                    return Json(new object[] { false, ex.Message });
                }
            }
        }

        public JsonResult SaveSort(List<string> list_id)
        {
            using (var trans = _db.Database.BeginTransaction())
            {
                try
                {
                    if (!access.ContainsKey("update_product"))
                    {
                        throw new Exception("Bạn không có quyền thay đổi. Vui lòng liên hệ Admin.");
                    }

                    var cates = _db.categories.ToList();
                    category cate_parentLv1 = null;
                    category cate_parentLv2 = null;

                    foreach (var item in SiteLang.GetListLangs())
                    {
                        for (int i = 0; i < list_id.Count; i++)
                        {
                            var cate = cates.FirstOrDefault(x => x.ReId == list_id[i].Split('|')[1] && x.LangCode == item.Code);
                            if (cate == null)
                            {
                                throw new Exception("Không tìm thấy Id" + list_id[i].Split('|')[1]);
                            }

                            if (list_id[i].Contains("1|"))
                            {
                                cate.ParentId = null;
                                cate.ParentName = null;
                                cate.Level = 1;
                                cate_parentLv1 = cate;
                            }
                            else if (list_id[i].Contains("2|"))
                            {
                                cate.ParentId = cate_parentLv1.ReId;
                                cate.ParentName = cate_parentLv1.CategoryName;
                                cate.Level = cate_parentLv1.Level + 1;
                                cate_parentLv2 = cate;
                            }
                            else if (list_id[i].Contains("3|"))
                            {
                                cate.ParentId = cate_parentLv2.ReId;
                                cate.ParentName = cate_parentLv2.CategoryName;
                                cate.Level = cate_parentLv2.Level + 1;
                            }
                            cate.Order = i;
                            _db.Entry(cate).State = System.Data.Entity.EntityState.Modified;
                        }
                    }
                    _db.SaveChanges();
                    trans.Commit();
                    TempData["success"] = "Lưu vị trí thành công";
                    return Json(new object[] { true, "Lưu vị trí thành công" });
                }
                catch (Exception e)
                {
                    trans.Dispose();
                    return Json(new object[] { false, e.Message });
                }
            }
        }
        #endregion

        #region PROPERTY

        public JsonResult GetProperty(string propertyReId, string langCode)
        {
            try
            {
                langCode = string.IsNullOrEmpty(langCode) ? default_language.Code : langCode;
                var prop = _db.product_properties.Where(x => x.ReId == propertyReId && x.is_parrent == true && x.LangCode == langCode).FirstOrDefault();
                if (prop == null)
                {
                    throw new Exception("Thuộc tính không tồn tại");
                }
                return Json(new object[] { true, prop });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        public JsonResult SaveProperty(string properti_ReId, string properti_name, string value_name, string langCode)
        {
            try
            {
                if (string.IsNullOrEmpty(properti_name))
                {
                    throw new Exception("Vui lòng nhập tên thuộc tính");
                }

                langCode = string.IsNullOrEmpty(langCode) ? default_language.Code : langCode;
                product_properties property = null, value = null;
                product_properties propertyRequest = null, valueRequest = null;
                if (string.IsNullOrEmpty(properti_ReId))
                {
                    var new_parentReId = AppFunc.NewShortId();
                    var new_ReId = AppFunc.NewShortId();

                    foreach (var item in SiteLang.GetListLangs())
                    {
                        var result = CommonFunc.Translate(properti_name, null, langCode, item.Code).FirstOrDefault();
                        if (result.Key != string.Empty)
                        {
                            throw new Exception(result.Key);
                        }

                        property = new product_properties()
                        {
                            id = AppFunc.NewShortId(),
                            name = result.Value.FirstOrDefault(),
                            is_parrent = true,
                            url = CommonFunc.ConvertNonUnicodeURL(result.Value.FirstOrDefault()),
                            LangCode = item.Code,
                            active = true,
                            ReId = new_parentReId,
                        };
                        _db.product_properties.Add(property);
                        if (langCode == item.Code)
                        {
                            propertyRequest = property;
                        }

                        if (!string.IsNullOrEmpty(value_name))
                        {
                            result = CommonFunc.Translate(value_name, null, langCode, item.Code).FirstOrDefault();
                            if (result.Key != string.Empty)
                            {
                                throw new Exception(result.Key);
                            }

                            value = new product_properties()
                            {
                                id = AppFunc.NewShortId(),
                                name = result.Value.FirstOrDefault(),
                                parrent_properties_id = property.id,
                                is_parrent = false,
                                url = CommonFunc.ConvertNonUnicodeURL(result.Value.FirstOrDefault()),
                                LangCode = item.Code,
                                active = true,
                                ReId = new_ReId,
                            };
                            _db.product_properties.Add(value);
                            if (langCode == item.Code)
                            {
                                valueRequest = value;
                            }
                        }
                    }
                    _db.SaveChanges();
                }
                else
                {
                    property = _db.product_properties.Where(x => x.ReId == properti_ReId && x.is_parrent == true && x.LangCode == langCode).FirstOrDefault();
                    if (property == null)
                    {
                        throw new Exception("Không tìm thấy thuộc tính");
                    }

                    if (property.name != properti_name)
                    {
                        property.name = properti_name;
                        property.url = CommonFunc.ConvertNonUnicodeURL(properti_name);
                        _db.Entry(property).State = System.Data.Entity.EntityState.Modified;
                        _db.SaveChanges();
                        propertyRequest = property;
                    }
                }
                return Json(new object[] { true, propertyRequest, valueRequest });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }

        public JsonResult GetValue(string propertyReId, string name, string langCode)
        {
            try
            {
                langCode = string.IsNullOrEmpty(langCode) ? default_language.Code : langCode;
                var prop = _db.product_properties.Where(x => (x.ReId == propertyReId ||x.name== name) && x.is_parrent == true && x.LangCode == langCode).FirstOrDefault();
                if (prop == null)
                {
                    throw new Exception("Thuộc tính không tồn tại");
                }
                var listValue = _db.product_properties.Where(x => x.parrent_properties_id == prop.id && x.is_parrent == false && x.active == true).OrderBy(x => x.name).ToList();
                return Json(new object[] { true, listValue });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        public JsonResult SaveValue(string properti_ReId, string value_id, string value_name, string langCode)
        {
            try
            {
                if (string.IsNullOrEmpty(properti_ReId))
                {
                    throw new Exception("Vui lòng chọn thuộc tính");
                }
                else if (value_id == "-1")
                {
                    throw new Exception("Vui lòng chọn giá trị cần sửa");
                }
                langCode = string.IsNullOrEmpty(langCode) ? default_language.Code : langCode;

                product_properties value = null, valueRequest = null;
                var parent_properti = _db.product_properties.Where(x => x.ReId == properti_ReId && x.is_parrent == true && x.LangCode == langCode).FirstOrDefault();

                if (string.IsNullOrEmpty(value_id))
                {
                    var new_ReId = AppFunc.NewShortId();
                    foreach (var item in SiteLang.GetListLangs())
                    {
                        var result = CommonFunc.Translate(value_name, null, langCode, item.Code).FirstOrDefault();
                        if (result.Key != string.Empty)
                        {
                            throw new Exception(result.Key);
                        }
                        var parent_Id = _db.product_properties.Where(x => x.LangCode == item.Code && x.ReId == parent_properti.ReId).FirstOrDefault()?.id;

                        value = new product_properties()
                        {
                            id = AppFunc.NewShortId(),
                            name = result.Value.FirstOrDefault(),
                            parrent_properties_id = parent_Id,
                            is_parrent = false,
                            url = CommonFunc.ConvertNonUnicodeURL(result.Value.FirstOrDefault()),
                            active = true,
                            LangCode = item.Code,
                            ReId = new_ReId,
                        };
                        _db.product_properties.Add(value);

                        if (parent_properti.LangCode == item.Code)
                        {
                            valueRequest = value;
                        }
                    }
                    _db.SaveChanges();
                }
                else
                {
                    value = _db.product_properties.Where(x => x.id == value_id && x.parrent_properties_id == parent_properti.id && x.is_parrent == false).FirstOrDefault();
                    if (value == null)
                    {
                        throw new Exception("Giá trị chỉnh sửa không tồn tại");
                    }

                    if (value.name != value_name)
                    {
                        value.name = value_name;
                        value.url = CommonFunc.ConvertNonUnicodeURL(value_name);
                        _db.Entry(value).State = System.Data.Entity.EntityState.Modified;
                        valueRequest = value;
                        _db.SaveChanges();
                    }
                }
                return Json(new object[] { true, valueRequest });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        public JsonResult DelProperty(List<string> propertyReId)
        {
            try
            {
                if (propertyReId != null && propertyReId.Count > 0)
                {
                    foreach (var reid in propertyReId)
                    {
                        foreach (var prop in _db.product_properties.Where(x => x.ReId == reid).ToList())
                        {
                            prop.active = false;
                            _db.Entry(prop).State = System.Data.Entity.EntityState.Modified;
                        }
                    }
                    _db.SaveChanges();
                }
                return Json(new object[] { true, "Xóa thành công" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }
        #endregion

        #region PRODUCT OPTION PRICE
        public JsonResult GetProductPrice(string proPrice_ReId, string lang)
        {
            try
            {
                lang = string.IsNullOrEmpty(lang) ? default_language.Code : lang;
                var listPrice = new List<productoptionprice>();
                if (Session[session_price] != null)
                {
                    listPrice = Session[session_price] as List<productoptionprice>;
                }

                var proPrice = listPrice.Where(x => x.ReId == proPrice_ReId && x.LangCode == lang && x.IsActive == true).FirstOrDefault();
                if (proPrice == null)
                {
                    throw new Exception("Giá tùy chọn không tồn tại");
                }
                return Json(new object[] { true, proPrice });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        public JsonResult SaveProductPrice(string proPrice_ReId, string proPrice_name, string proPrice_value, string lang)
        {
            try
            {
                if (string.IsNullOrEmpty(proPrice_name))
                {
                    throw new Exception("Tên nhãn không được trống");
                }

                lang = string.IsNullOrEmpty(lang) ? default_language.Code : lang;
                var listPrice = new List<productoptionprice>();
                if (Session[session_price] != null)
                {
                    listPrice = Session[session_price] as List<productoptionprice>;
                }

                productoptionprice priceResponse = null;
                var _price = double.Parse(string.IsNullOrEmpty(proPrice_value) ? "0" : proPrice_value.Replace(".", string.Empty));
                if (string.IsNullOrEmpty(proPrice_ReId))
                {
                    var newReId = AppFunc.NewShortId();
                    foreach (var item in SiteLang.GetListLangs())
                    {
                        var result = AppLB.CommonFunc.Translate(proPrice_name, null, "vi", item.Code).FirstOrDefault();
                        if (result.Key != string.Empty)
                        {
                            throw new Exception(result.Key);
                        }

                        var newPrice = new productoptionprice()
                        {
                            Id = AppFunc.NewShortId(),
                            LabelName = result.Value.FirstOrDefault(),
                            Price = _price,
                            ReId = newReId,
                            LangCode = item.Code,
                            IsActive = true,
                        };
                        listPrice.Add(newPrice);
                        if (lang == item.Code)
                        {
                            priceResponse = newPrice;
                        }
                    }
                }
                else
                {
                    var priceByReId = listPrice.Where(x => x.ReId == proPrice_ReId).ToList();
                    var prodPrice = priceByReId.Where(x => x.LangCode == lang).FirstOrDefault();
                    if (prodPrice != null)
                    {
                        prodPrice.LabelName = proPrice_name;
                        prodPrice.Price = _price;
                        priceResponse = prodPrice;

                        foreach (var item in priceByReId.Where(x => x.LangCode != lang))
                        {
                            item.Price = _price;
                        }
                    }
                }
                Session[session_price] = listPrice;
                return Json(new object[] { true, priceResponse });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        public JsonResult DeleteProductPrice(string proPrice_ReId)
        {
            try
            {
                var listPrice = new List<productoptionprice>();
                if (Session[session_price] != null)
                {
                    listPrice = Session[session_price] as List<productoptionprice>;
                }

                var priceExistInDB = _db.productoptionprices.Any(x => x.ReId == proPrice_ReId && x.IsActive == true);
                var prices = listPrice.Where(x => x.ReId == proPrice_ReId).ToList();
                if (prices != null && prices.Count > 0)
                {
                    foreach (var item in prices)
                    {
                        if (priceExistInDB)
                        {
                            item.IsActive = false;
                        }
                        else
                        {
                            listPrice.Remove(item);
                        }
                    }
                }
                return Json(new object[] { true, "Xóa thành công", listPrice.Where(x => x.IsActive == true).Count() });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }
        #endregion
    }
}