namespace PKWebShop.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.SqlServer;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Text.RegularExpressions;
    using System.Web.Mvc;
    using Newtonsoft.Json;
    using PKWebShop.AppLB;
    using PKWebShop.Models;
    using PKWebShop.Models.CustomizeModels;

    public class ProductController : ExpiredCheckController
    {
        private DBLangCustom db = new();

        // GET: Products
        public ActionResult Index(string url, string cateId)
        {
            try
            {
                var categorys = db.categories.Where(x => x.Sellable != false && x.Active == true).OrderBy(x => x.Order).ToList();
                var category = categorys.FirstOrDefault(c => c.ReId == cateId);
                ViewBag.category = category?.ReId ?? string.Empty;
                ViewBag.maxprice = db.products.Max(p => p.Price);

                var webinfo = db.webconfigurations.FirstOrDefault();
                if (category != null)
                {
                    ViewBag.SEO_category = category;
                }
                if (!string.IsNullOrEmpty(webinfo.SEO_Meta))
                {
                    var list_SEO = JsonConvert.DeserializeObject<List<SiteSEO.SEO_Meta>>(webinfo.SEO_Meta);
                    var SEO_sanpham = list_SEO.FirstOrDefault(x => x.code == SiteSEO.code_SEO.SanPham);
                    ViewBag.SEO_sanpham = SEO_sanpham;
                }
                ViewBag.WebInfo = webinfo;

                return View(categorys);
            }
            catch (Exception ex)
            {
                TempData["e"] = ex.Message;
                return Redirect("/notfound");
            }
        }

        public ActionResult GetProducts(string Search, string Category, double? PriceFrom, double? PriceTo, string Order, int View = 9, int Page = 1)
        {
            try
            {
                var webinfo = db.webconfigurations.FirstOrDefault();
                var products = db.products.Where(x => x.Sellable != false && x.IsActive != false).AsQueryable();
                var _db = new WebShopEntities();
                if (!string.IsNullOrEmpty(Search))
                {
                    // .*[oơỡợ].*
                    var sqlCommand = CommonFunc.SearchCommand("products", Search, "ProductName");
                    products = _db.products.SqlQuery(sqlCommand + " and LangCode = '" + db.LangCode + "'" + "and IsActive != false and Sellable != false").AsQueryable();

                }

                var categorys = db.categories.Where(x => x.Sellable != false && x.Active == true).OrderBy(x => x.Order).AsQueryable();
                string list_PrentCateReId = string.Empty;
                var cates = UserContent.GetAllChildCategory(Category);
                if (cates.Count > 1)
                {
                    products = products.Where(x => cates.Any(c => x.CategoryId.Contains(c)));
                }
                else if (cates.Count == 1)
                {
                    var cate = cates.First();
                    products = products.Where(x => x.CategoryId.Contains(cate));
                }
                ViewBag.PrentCateReId = string.Join(",", cates);

                if (PriceFrom > 0)
                {
                    products = products.Where(p => p.Price >= PriceFrom);
                }
                if (PriceTo > 0)
                {
                    products = products.Where(p => p.Price <= PriceTo);
                }
                if (!string.IsNullOrEmpty(Order))
                {
                    switch (Order)
                    {
                        case "1": products = products.OrderByDescending(p => p.CreateAt); break;
                        case "2": products = products.OrderByDescending(p => p.Sold); break;
                        case "3": products = products.Where(p => p.SalePrice > 0).OrderBy(p => (p.SalePrice ?? p.Price) / p.Price); break;
                        case "4": products = products.OrderBy(p => p.SalePrice ?? p.Price); break;
                        case "5": products = products.OrderByDescending(p => p.SalePrice ?? p.Price); break;
                    }
                }
                else
                {
                    products = products.OrderBy(p => p.ProductName);
                }

                int prd_count = products.Count();
                var pagecount = Math.Max((int)Math.Ceiling((decimal)prd_count / View), 1);
                Page = Math.Min(Page, pagecount);
                ViewBag.hienthi = "0 sản phẩm";
                if (prd_count > 0)
                {
                    ViewBag.hienthi = ((Page * View) - View + 1) + " - " + Math.Min(Page * View, prd_count) + " của " + prd_count + " sản phẩm";
                }
                var listProd = products.Skip((Page - 1) * View).Take(View).ToList();
                ViewBag.pages = (Page, pagecount);
                return PartialView("_ListProductPartial", listProd);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Product detail.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public ActionResult Detail(string url, string Id)
        {
            var pro = db.products.FirstOrDefault(p => p.ReId == Id && p.Sellable != false && p.IsActive != false);
            pro ??= db.products.FirstOrDefault(p => p.ReId == url || p.Id == url);
            if (pro == null)
            {
                return Redirect("/");
            }
            var pictures = db.uploadmorefiles.Where(u => u.TableId == pro.ReId && u.TableName == "products").ToList();
            if (!string.IsNullOrEmpty(pro.Picture))
            {
                pictures.Insert(0, new uploadmorefile { FileName = pro.Picture });
            }

            var listProperties = (from x in db.product_properties
                                  where (pro.list_parent_properties.Contains(x.ReId) && x.active != false)
                                  || (pro.list_properties.Contains(x.ReId) && x.active != false)
                                  orderby x.name
                                  select x).GroupJoin(db.uploadmorefiles, prop => pro.ReId + "|" + prop.ReId, f => f.TableId, (prop, files) => new { prop, file = files.FirstOrDefault() })
                                  .ToList().Select(x => { x.prop.image = x.file?.FileName; return x.prop; }).ToList();

            var arrObj = new object[]
            {
                pictures,
                db.categories.Where(c => pro.CategoryId.Contains(c.ReId)).ToList(),
                listProperties,
                null,
                db.products.Where(p => p.CategoryId == pro.CategoryId && p.ReId != pro.ReId).OrderByDescending(p => p.Sold).ToList(),
            };
            ViewBag.ArrObj = arrObj;
            ViewBag.og_image = pro.Picture;
            ViewBag.Title = pro.ProductName;
            var ListOpt = db.product_options.Where(p => p.ProductId == Id && string.IsNullOrEmpty(p.parentId))
                .Select(p => new { parent = p, listoption = db.product_options.Where(o => o.parentId == p.id).ToList() }).ToList()
                .Select(x => { x.parent.ListOption = x.listoption; return x.parent; }).ToList();
            ViewBag.ListOpt = ListOpt;
            if (ListOpt.Count > 0)
            {
                var prices = db.product_option_values.Where(x => x.productId == pro.ReId).Select(x => x.price);
                ViewBag.Price_range = prices.Min().ToString("#,##0 đ") + " - " + prices.Max().ToString("#,##0 đ");
            }
            return View(pro);
        }

        public JsonResult getPricebyOptions(string id, string option1, string option2)
        {
            try
            {
                var listvalues = db.product_option_values.Where(x => x.productId == id).AsQueryable();
                if (!string.IsNullOrEmpty(option1))
                {
                    listvalues = listvalues.Where(x => x.option1 == option1);
                }
                if (!string.IsNullOrEmpty(option2))
                {
                    listvalues = listvalues.Where(x => x.option2 == option2);
                }
                var rs = listvalues.ToList();
                return Json(new object[] { true, rs, (rs.Count == 1) ? rs.First().price.ToString("#,##0 đ") : null });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }

        /// <summary>
        /// Service.
        /// </summary>
        /// <param name="id">id.</param>
        /// <returns>service.</returns>
        public ActionResult Service(string id)
        {
            try
            {
                ViewBag.listCategory = db.categories.OrderBy(x => x.Order).ToList();
                var listService = db.services.OrderBy(s => s.Order).ToList();
                ViewBag.listService = listService;
                var service = new service();

                if (id == null)
                {
                    var defaulService = listService.FirstOrDefault();
                    if (defaulService == null)
                    {
                        throw new Exception();
                    }
                    else
                    {
                        service = defaulService;
                    }
                }
                else
                {
                    var sv = db.services.FirstOrDefault(s => s.ReId == id);
                    if (sv == null)
                    {
                        throw new Exception();
                    }
                    else
                    {
                        service = sv;
                    }
                }

                // Get top background image
                ViewBag.page_slide = (from f in db.sectionfeaturedetails
                                      join s in db.sectionfeatures on f.SectionCode equals s.Code
                                      where s.Status == true && s.Code == UserContent.Web_Feature.TopBackground.ToString()
                                      select new SectionFeatureModel
                                      {
                                          Description = f.Description,
                                          Picture = f.Picture,
                                          SectionCode = f.SectionCode,
                                          SectionTitle = s.Title,
                                          Title = f.Title,
                                          URL = f.URL,
                                          VolumeNumber = f.VolumeNumber,
                                      }).FirstOrDefault();
                return View(service);
            }
            catch (Exception)
            {
                return Redirect("/notfound");
            }
        }

        public ActionResult _ProductsHome()
        {
            //Sản phẩm
            string s_sanpham = UserContent.Web_Feature.trangchu_sanpham.ToString();
            var sp = db.sectionfeatures.Where(s => s.ReId == s_sanpham && s.Status == true).FirstOrDefault();
            if (sp != null)
            {
                var sanpham_banchay = (from s in db.products
                                       join od in (from od in db.orders_detail group od by od.ProductId into od_g select od_g) on s.Id equals od.Key into od_gj
                                       from od in od_gj.DefaultIfEmpty()
                                       orderby s.Order descending, od.Count() descending
                                       select s).Take(4).ToList();

                var sanpham_list = (from sd in db.sectionfeaturedetails.Where(sd => sd.SectionCode == s_sanpham)

                                    join g_sp in (from s in db.products group s by s.CategoryId into g_sp select g_sp) on sd.Description equals g_sp.Key into gj_sp
                                    from g_sp in gj_sp.DefaultIfEmpty()
                                    select new Products_SectionView
                                    {
                                        section_detail = sd,
                                        list_products = g_sp.OrderByDescending(s => s.Order).ThenBy(s => s.ProductName),
                                    }).OrderByDescending(d => d.section_detail.VolumeNumber).ToList();
                sanpham_list.ForEach(l => l.list_products = l.list_products.Take(4));
                sanpham_list.Where(l => l.section_detail.Description == "sanphambanchay").ToList().ForEach(l =>
                {
                    l.list_products = sanpham_banchay;

                });

                var sanphamkhuyenmai = (from s in db.products
                                        where s.SalePrice < s.Price
                                        orderby s.UpdateAt descending
                                        select s).Take(4).ToList();
                sanpham_list.Where(l => l.section_detail.Description == "sanphamkhuyenmai").ToList().ForEach(l =>
                {
                    l.list_products = sanphamkhuyenmai;
                });
                ViewBag.sanpham_list = sanpham_list;
                return PartialView(sanpham_list);
            }
            return null;
        }
    }
}