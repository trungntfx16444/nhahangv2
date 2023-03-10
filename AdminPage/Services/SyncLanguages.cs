namespace AdminPage.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using AdminPage.AppLB;
    using AdminPage.Models;
    using AdminPage.Services;
    using AdminPage.Utils;

    public class SyncLanguages
    {
        static List<SiteLang.Language> listLang = SiteLang.GetListLangs();

        internal static string Product()
        {
            using (var db = new AdminEntities())
            {
                try
                {
                    string result = string.Empty;

                    // check xem các danh mục sản phẩm có đủ ngôn ngữ hay chưa
                    var listProduct = db.products.AsQueryable();
                    if (listProduct.Count() < (listProduct.Select(x => x.ReId).Distinct().Count() * listLang.Count()))
                    {
                        // CHECK & UPDATE PRODUCT PRICE OPTION
                        result = PriceOption();

                        // CHECK & UPDATE PRODUCT PROPERTY
                        result = Property();

                        // CHECK & UPDATE PRODUCT CATEGORY
                        result = Category();

                        // CHECK & UPDATE PRODUCT
                        var listPro = listProduct.ToList();
                        var listReId = listPro.Select(x => x.ReId).Distinct();

                        foreach (var reId in listReId)
                        {
                            var prods = listPro.Where(x => x.ReId == reId);
                            if (prods.Count() < listLang.Count)
                            {
                                foreach (var lang in listLang)
                                {
                                    if (!prods.Any(x => x.LangCode == lang.Code))
                                    {
                                        CloneProduct(db, listPro, prods.FirstOrDefault(), lang.Code);
                                    }
                                }
                            }
                        }
                    }
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
        }

        internal static void CloneProduct(AdminEntities db, List<product> listPro, product prod, string langCode)
        {
            var prod_clone = new product()
            {
                Id = AppFunc.NewShortId(),
                Picture = prod.Picture,
                CategoryId = prod.CategoryId,
                Price = prod.Price,
                SalePrice = prod.SalePrice,
                DiscountAmount = prod.DiscountAmount,
                DiscountPercent = prod.DiscountPercent,
                ShortDescription = prod.ShortDescription,
                Description = prod.Description,
                ShowHomePage = prod.ShowHomePage,
                Quantity = prod.Quantity ?? 0,
                CreateBy = prod.CreateBy,
                CreateAt = prod.CreateAt,
                UpdateBy = prod.UpdateBy,
                UpdateAt = DateTime.Now,
                LangCode = langCode,
                Url = prod.Url,
                Rating = prod.Rating ?? 5,
                list_parent_properties = prod.list_parent_properties,
                list_properties = prod.list_properties,
                IsActive = prod.IsActive,
                ReId = prod.ReId,
                VendorId = prod.VendorId,
                WarningQuantity = prod.WarningQuantity,
                Sellable = prod.Sellable,
                ValueExchange = prod.ValueExchange,
                SEO_Desc = prod.SEO_Desc,
                Unit = prod.Unit,
            };

            var cateName = string.Empty;
            foreach (var cate in db.categories.Where(x => prod_clone.CategoryId.Contains(x.ReId) && x.LangCode == langCode))
            {
                cateName += $"{cate.CategoryName},";
            }
            prod_clone.CategoryName = cateName.Trim(',');
            prod_clone.ProductName = CommonFunc.Translate(prod.ProductName, null, prod.LangCode, langCode).FirstOrDefault().Value.First();
            prod_clone.ImageAlt = prod_clone.ProductName;
            prod_clone.SEO_Title = prod_clone.ProductName;

            db.products.Add(prod_clone);
            db.SaveChanges();
            listPro.Add(prod_clone);
        }

        #region PRODUCT CATEGORY
        internal static string Category()
        {
            using (var db = new AdminEntities())
            {
                try
                {
                    // check xem các danh mục sản phẩm có đủ ngôn ngữ hay chưa
                    var listCategory = db.categories.AsQueryable();
                    if (listCategory.Count() < (listCategory.Select(x => x.ReId).Distinct().Count() * listLang.Count))
                    {
                        var listCate = listCategory.ToList();
                        var listReId = listCate.OrderBy(x => x.Level).Select(x => x.ReId).Distinct();

                        foreach (var reId in listReId)
                        {
                            var cates = listCate.Where(x => x.ReId == reId);
                            if (cates.Count() < listLang.Count)
                            {
                                foreach (var lang in listLang)
                                {
                                    if (!cates.Any(x => x.LangCode == lang.Code))
                                    {
                                        CloneCategory(db, listCate, cates.FirstOrDefault(), lang.Code);
                                    }
                                }
                            }
                        }
                    }
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
        }

        internal static void CloneCategory(AdminEntities db, List<category> listCate, category cate, string langCode)
        {
            var cate_clone = new category()
            {
                Id = AppFunc.NewShortId(),
                CategoryName = CommonFunc.Translate(cate.CategoryName, null, cate.LangCode, langCode).FirstOrDefault().Value.First(),
                Description = cate.Description,
                Level = cate.Level,
                Order = cate.Order,
                MetaTitle = cate.MetaTitle,
                MetaDesc = cate.MetaDesc,
                MetaKeyWord = cate.MetaKeyWord,
                Active = cate.Active ?? false,
                Sellable = cate.Sellable ?? false,
                LangCode = langCode,
                ReId = cate.ReId,
            };
            cate_clone.UrlCode = CommonFunc.ConvertNonUnicodeURL(cate_clone.CategoryName);
            if (cate_clone.Level > 1)
            {
                var parentReId = listCate.FirstOrDefault(x => x.Id == cate.ParentId)?.ReId;
                var cateParent = listCate.FirstOrDefault(x => x.ReId == parentReId && x.LangCode == langCode);
                cate_clone.ParentId = cateParent.Id;
                cate_clone.ParentName = cateParent.CategoryName;
            }
            db.categories.Add(cate_clone);
            db.SaveChanges();
            listCate.Add(cate_clone);
        }
        #endregion

        #region PRODUCT PRICE OPTION
        internal static string PriceOption()
        {
            using (var db = new AdminEntities())
            {
                try
                {
                    // check xem các giá bán tùy chọn có đủ ngôn ngữ hay chưa
                    var listPriceOpt = db.productoptionprices.AsQueryable();
                    if (listPriceOpt.Count() < (listPriceOpt.Select(x => x.ReId).Distinct().Count() * listLang.Count))
                    {
                        var listPrice = listPriceOpt.ToList();
                        var listReId = listPrice.Select(x => x.ReId).Distinct();

                        foreach (var reId in listReId)
                        {
                            var prices = listPrice.Where(x => x.ReId == reId);
                            if (prices.Count() < listLang.Count)
                            {
                                foreach (var lang in listLang)
                                {
                                    if (!prices.Any(x => x.LangCode == lang.Code))
                                    {
                                        ClonePriceOption(db, listPrice, prices.FirstOrDefault(), lang.Code);
                                    }
                                }
                            }
                        }
                    }
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
        }

        internal static void ClonePriceOption(AdminEntities db, List<productoptionprice> listPrice, productoptionprice price, string langCode)
        {
            var price_clone = new productoptionprice()
            {
                Id = AppFunc.NewShortId(),
                ProductId = price.ProductId,
                LabelName = CommonFunc.Translate(price.LabelName, null, price.LangCode, langCode).FirstOrDefault().Value.First(),
                Price = price.Price,
                ReId = price.ReId,
                LangCode = langCode,
                IsActive = price.IsActive,
            };
            db.productoptionprices.Add(price_clone);
            db.SaveChanges();
            listPrice.Add(price_clone);
        }
        #endregion

        #region PRODUCT PROPERTY
        internal static string Property()
        {
            using (var db = new AdminEntities())
            {
                try
                {
                    // check xem các thuộc tính sản phẩm có đủ ngôn ngữ hay chưa
                    var listProperties = db.product_properties.AsQueryable();
                    if (listProperties.Count() < (listProperties.Select(x => x.ReId).Distinct().Count() * listLang.Count))
                    {
                        var listProp = listProperties.ToList();
                        var listReId = listProp.OrderBy(x => x.is_parrent).Select(x => x.ReId).Distinct();

                        foreach (var reId in listReId)
                        {
                            var props = listProp.Where(x => x.ReId == reId);
                            if (props.Count() < listLang.Count)
                            {
                                foreach (var lang in listLang)
                                {
                                    if (!props.Any(x => x.LangCode == lang.Code))
                                    {
                                        CloneProperty(db, listProp, props.FirstOrDefault(), lang.Code);
                                    }
                                }
                            }
                        }
                    }
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
        }

        internal static void CloneProperty(AdminEntities db, List<product_properties> listProp, product_properties prop, string langCode)
        {
            var prop_clone = new product_properties()
            {
                id = AppFunc.NewShortId(),
                is_parrent = prop.is_parrent,
                LangCode = langCode,
                active = true,
                ReId = prop.ReId,
            };
            prop_clone.name = CommonFunc.Translate(prop.name, null, prop.LangCode, langCode).FirstOrDefault().Value.First();
            prop_clone.url = CommonFunc.ConvertNonUnicodeURL(prop_clone.name);

            if (prop_clone.is_parrent == false)
            {
                var parentReId = listProp.FirstOrDefault(x => x.id == prop.parrent_properties_id)?.ReId;
                var propParent = listProp.FirstOrDefault(x => x.ReId == parentReId && x.LangCode == langCode);
                prop_clone.parrent_properties_id = propParent.id;
            }
            db.product_properties.Add(prop_clone);
            db.SaveChanges();
            listProp.Add(prop_clone);
        }
        #endregion
    }
}