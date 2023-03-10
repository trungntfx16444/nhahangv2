namespace AdminPage.Services
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using AdminPage.AppLB;
    using AdminPage.Models;
    using AdminPage.Utils;
    using Newtonsoft.Json;

    public class Products
    {
        internal static product SaveProduct(product p, string newReId)
        {
            var adminSite = CommonFunc.GetAdminSite();
            using (var db = new AdminEntities())
            {
                int i = 0;
                start_random:
                if (i < 5)
                {
                    p.Url = !string.IsNullOrEmpty(p.Url) ? p.Url : CommonFunc.ConvertNonUnicodeURL(p.ProductName);
                    if (db.products.Any(x => x.Url == p.Url && x.ReId != p.ReId && x.IsActive != false))
                    {
                        p.Url += CommonFunc.GetRandomString(1, "0123456789");
                        i++;
                        goto start_random;
                    }
                }
                else
                {
                    throw new Exception("Url sản phẩm đã tồn tại.");
                }

                var tags = p.TagName?.Split(',').Select(t => new tag { TagName = t, TagCode = CommonFunc.ConvertNonUnicodeURL(t) }).ToList();
                user cMem = Authority.GetThisUser();
                Regex regex = new("<img(?!((?!/>).)* loading=\"lazy\")");

                if (string.IsNullOrEmpty(p.LangCode))
                {
                    p.LangCode = SiteLang.GetDefault().Code;
                }

                //if (!p.Picture.Contains("/Content/admin/img/no_image.jpg"))
                //{
                //    p.Picture = AppLB.CommonFunc.converttojpg(p.Picture);
                //    AppLB.CommonFunc.create_mini_images(p.Picture);
                //}
                
                var product = new product();
                p.Description = regex.Replace(p.Description ?? string.Empty, "<img loading=\"lazy\"");
                p.Description = p.Description?.Replace("src=\"/Upload/", $"src=\"{adminSite}/Upload/");
                p.Description = p.Description?.Replace("href=\"/Upload/", $"href=\"{adminSite}/Upload/");
                p.Quantity = (p.Quantity == null || p.Quantity < -1) ? -1 : p.Quantity;

                if (!string.IsNullOrEmpty(p.ReId))
                {
                    product = db.products.FirstOrDefault(x => x.ReId == p.ReId && x.LangCode == p.LangCode);
                    if (product == null)
                    {
                        throw new Exception("Không tìm thấy sản phẩm");
                    }

                    var listCate = db.categories.Where(x => p.CategoryId.Contains(x.ReId)).OrderBy(x => x.Order).ToList();
                    var cateName = string.Empty;
                    foreach (var item in listCate.Where(x => x.LangCode == p.LangCode))
                    {
                        cateName += $"{item.CategoryName},";
                    }

                    product.ProductName = p.ProductName;
                    product.CategoryId = p.CategoryId;
                    product.CategoryName = cateName.Trim(',');
                    product.Description = p.Description;
                    product.ShortDescription = p.ShortDescription;
                    product.ShowHomePage = p.ShowHomePage;
                    product.Price = p.Price;
                    product.SalePrice = p.SalePrice;
                    product.DiscountAmount = p.DiscountAmount;
                    product.DiscountPercent = p.DiscountPercent;
                    product.UpdateAt = DateTime.Now;
                    product.UpdateBy = cMem.Fullname;
                    product.Picture = p.Picture;
                    product.Url = p.Url;
                    product.Quantity = p.Quantity;
                    product.Rating = p.Rating ?? 5;
                    product.list_parent_properties = p.list_parent_properties?.TrimStart(',');
                    product.list_properties = p.list_properties?.TrimStart(',');
                    product.VendorId = p.VendorId;
                    product.WarningQuantity = p.WarningQuantity;
                    product.Sellable = p.Sellable;
                    product.ValueExchange = p.ValueExchange;
                    product.TagName = string.Join(",", tags?.Select(t => t.TagName) ?? new List<string>());
                    product.TagID = string.Join(",", tags?.Select(t => t.TagCode) ?? new List<string>());
                    product.SEO_Title = p.SEO_Title;
                    product.SEO_Desc = p.SEO_Desc;
                    product.ImageAlt = string.IsNullOrEmpty(p.ImageAlt) ? p.ProductName : p.ImageAlt;
                    product.Unit = p.Unit;
                    product.Code = p.Code;
                    tags?.Where(t => !db.tags.Any(_t => _t.TagCode == t.TagCode)).ToList().ForEach(t =>
                    {
                        t.Id = AppFunc.NewShortId();
                        db.tags.Add(t);
                    });

                    db.Entry(product).State = System.Data.Entity.EntityState.Modified;

                    foreach (var pr in db.products.Where(x => x.ReId == p.ReId && x.Id != product.Id).ToList())
                    {
                        cateName = string.Empty;
                        foreach (var item in listCate.Where(x => x.LangCode == pr.LangCode).ToList())
                        {
                            cateName += $"{item.CategoryName},";
                        }

                        pr.Picture = product.Picture;
                        pr.CategoryId = product.CategoryId;
                        pr.CategoryName = cateName.Trim(',');
                        pr.Price = product.Price;
                        pr.SalePrice = product.SalePrice;
                        pr.ShowHomePage = product.ShowHomePage;
                        pr.Quantity = product.Quantity ?? 0;
                        pr.Rating = product.Rating ?? 5;
                        pr.list_parent_properties = product.list_parent_properties;
                        pr.list_properties = product.list_properties;
                        pr.VendorId = product.VendorId;
                        pr.WarningQuantity = product.WarningQuantity;
                        pr.Sellable = product.Sellable;
                        pr.ValueExchange = product.ValueExchange;
                        pr.Unit = product.Unit;
                        db.Entry(pr).State = System.Data.Entity.EntityState.Modified;
                    }
                }
                else
                {
                    foreach (var item in SiteLang.GetListLangs())
                    {
                        var result = CommonFunc.Translate(p.ProductName, null, "vi", item.Code).FirstOrDefault();
                        if (result.Key != string.Empty)
                        {
                            throw new Exception(result.Key);
                        }

                        p.ProductName = result.Value.First();
                        var cateName = string.Empty;
                        foreach (var cate in db.categories.Where(x => p.CategoryId.Contains(x.ReId) && x.LangCode == item.Code))
                        {
                            cateName += $"{cate.CategoryName},";
                        }

                        product = new product()
                        {
                            Id = AppFunc.NewShortId(),
                            ProductName = p.ProductName,
                            Picture = p.Picture,
                            CategoryId = p.CategoryId,
                            CategoryName = cateName.Trim(','),
                            Price = p.Price,
                            SalePrice = p.SalePrice,
                            DiscountAmount = p.DiscountAmount,
                            DiscountPercent = p.DiscountPercent,
                            ShortDescription = p.ShortDescription,
                            Description = p.Description,
                            ShowHomePage = p.ShowHomePage,
                            Quantity = p.Quantity,
                            CreateBy = cMem.Fullname,
                            CreateAt = DateTime.Now,
                            UpdateBy = cMem.Fullname,
                            UpdateAt = DateTime.Now,
                            LangCode = item.Code,
                            Url = p.Url,
                            Rating = p.Rating ?? 5,
                            list_parent_properties = p.list_parent_properties?.TrimStart(','),
                            list_properties = p.list_properties?.TrimStart(','),
                            IsActive = true,
                            ReId = newReId,
                            VendorId = p.VendorId,
                            WarningQuantity = p.WarningQuantity,
                            Sellable = p.Sellable,
                            ValueExchange = p.ValueExchange,
                            SEO_Title = p.SEO_Title,
                            SEO_Desc = p.SEO_Desc,
                            ImageAlt = string.IsNullOrEmpty(p.ImageAlt) ? p.ProductName : p.ImageAlt,
                            Unit = p.Unit,
                            Code = p.Code,
                    };

                        product.TagName = string.Join(",", tags?.Select(t => t.TagName) ?? new List<string>());
                        product.TagID = string.Join(",", tags?.Select(t => t.TagCode) ?? new List<string>());
                        tags?.Where(t => !db.tags.Any(_t => _t.TagCode == t.TagCode)).ToList().ForEach(t =>
                        {
                            t.Id = AppFunc.NewShortId();
                            db.tags.Add(t);
                        });
                        db.products.Add(product);
                    }
                }
                db.SaveChanges();

                // cap nhat lai session
                //UserContent.GetWebCategoryProduct(true);
                return product;
            }
        }

        //internal static bool SaveProductFromExcel(DataTable excelTable, string cateReId, out string message)
        //{
        //    message = string.Empty;
        //    var db = new AdminEntities();
        //    using (var trans = db.Database.BeginTransaction())
        //    {
        //        try
        //        {
        //            var langDefault = SiteLang.GetDefault();
        //            var listNewCate = new List<category>();
        //            var listAllCate = db.categories.ToList();
        //            var hinhanhphu = new List<Dictionary<string, string>>();
        //            var quycachgia = new List<Dictionary<string, string>>();

        //            var cateLv1 = listAllCate.FirstOrDefault(x => x.ReId == cateReId && x.LangCode == langDefault.Code);
        //            if (cateLv1 == null)
        //            {
        //                throw new Exception("Danh mục sản phẩm không tồn tại.");
        //            }

        //            user cMem = Authority.GetThisUser();
        //            var listProd = new List<product>();
        //            for (int i = 0; i < excelTable.Rows.Count; i++)
        //            {
        //                var prod = new product()
        //                {
        //                    Id = AppFunc.NewShortId(),
        //                    ProductName = string.Empty,
        //                    Picture = string.Empty,
        //                    CategoryId = string.Empty,
        //                    CategoryName = string.Empty,
        //                    Price = 0,
        //                    SalePrice = 0,
        //                    ShortDescription = string.Empty,
        //                    ShowHomePage = false,
        //                    Quantity = 1,
        //                    CreateBy = cMem.Fullname,
        //                    CreateAt = DateTime.Now,
        //                    LangCode = langDefault.Code,
        //                    Rating = 5,
        //                    IsActive = true,
        //                    Sellable = true,
        //                };
        //                prod.ReId = prod.Id;

        //                var gioiTinh = new Dictionary<string, string>();
        //                var nhanHieu = new Dictionary<string, string>();


        //                // Cap nhat data tu file excel
        //                for (int j = 0; j < excelTable.Columns.Count; j++)
        //                {
        //                    var caption = excelTable.Columns[j].Caption;
        //                    var captionLower = caption?.ToLower();
        //                    var _value = excelTable.Rows[i].ItemArray[j].ToString();

        //                    //if (captionLower == ExcelColumns.Link)
        //                    //{
        //                    //    string url = _value?.Split('/').LastOrDefault()?.Split('?').FirstOrDefault()?.Replace(".html", string.Empty);
        //                    //    string f_replace = $"{url?.Split('-').FirstOrDefault()}-";
        //                    //    prod.Url = url?.Replace(f_replace, string.Empty);
        //                    //}
        //                    if (captionLower == ExcelColumns.TenSanPham)
        //                    {
        //                        prod.ProductName = _value;
        //                    }
        //                    else if (captionLower == ExcelColumns.NhanHieu)
        //                    {
        //                        nhanHieu.Add(caption, _value);
        //                    }
        //                    //else if (captionLower == ExcelColumns.GioiTinh)
        //                    //{
        //                    //    gioiTinh.Add(caption, _value);
        //                    //}
        //                    else if (captionLower == ExcelColumns.XuatXu || captionLower == ExcelColumns.NhomHuong || captionLower == ExcelColumns.NamPhatHanh
        //                       || captionLower == ExcelColumns.PhongCach || captionLower == ExcelColumns.XuatXu)
        //                    {
        //                        if (!string.IsNullOrEmpty(_value))
        //                        {
        //                            prod.ShortDescription += $"{caption}:{(_value.Contains(":") ? "\r\n" : " ")}{_value}\r\n";
        //                        }
        //                    }
        //                    else if (captionLower == ExcelColumns.GiaChuaGiam)
        //                    {
        //                        double giachuagiam = double.Parse(Regex.Replace(_value, "\\D", ""));
        //                        //giam 50k so voi file excel
        //                        prod.Price = giachuagiam > 50000 ? (giachuagiam - 50000) : giachuagiam;
        //                    }
        //                    else if (captionLower == ExcelColumns.GiaBan)
        //                    {
        //                        double giagiam = double.Parse(Regex.Replace(_value, "\\D", ""));
        //                        //giam 50k
        //                        prod.SalePrice = giagiam > 0 ? (giagiam > 50000 ? (giagiam - 50000) : giagiam) : null;

        //                    }
        //                    else if (captionLower == ExcelColumns.Anh1)
        //                    {
        //                        prod.Picture = _value;
        //                    }
        //                    else if (captionLower == ExcelColumns.ThongTin)
        //                    {
        //                        prod.Description = _value;
        //                    }
        //                    else if (captionLower == ExcelColumns.Anh2)
        //                    {
        //                        //save records hinh anh vao upload morefiles: 5hinhanh
        //                        var _hinhanhphu = new Dictionary<string, string>();
        //                        _hinhanhphu.Add("prodid", prod.Id);
        //                        for (int p = 0; p < 5; p++)
        //                        {
        //                            _hinhanhphu.Add("pic" + (p + 1), excelTable.Rows[i].ItemArray[j + p].ToString());
        //                        }
        //                        hinhanhphu.Add(_hinhanhphu);

        //                        j += 4;
        //                    }
        //                    else if (captionLower == ExcelColumns.Quycach1)
        //                    {
        //                        //6quycach
        //                        //ong thu 6 la ong nhỏ nhất chỉ có 10ml chi danh cho loai nuoc hoa nam/nu

        //                        //var _qc = new Dictionary<string, string>();
        //                        //_qc.Add("prodid", prod.Id);
        //                        //_qc.Add("name", "Eau de Parfum 10ml");
        //                        //_qc.Add("price", ((prod.Price * 15)/100+20000).ToString());
        //                        //quycachgia.Add(_qc);

        //                        //quy cach 1->5
        //                        for (int qc = 0; qc < 5; qc += 2)
        //                        {
        //                            string str_quycach = excelTable.Rows[i].ItemArray[j + qc].ToString();
        //                            string str_quycach_gia = excelTable.Rows[i].ItemArray[j + qc + 1].ToString();
        //                            if (!string.IsNullOrWhiteSpace(str_quycach))
        //                            {

        //                                try
        //                                {
        //                                    var qc1 = new Dictionary<string, string>();
        //                                    double gia = double.Parse(Regex.Replace(str_quycach_gia, "\\D", ""));
        //                                    qc1.Add("prodid", prod.Id);
        //                                    qc1.Add("name", str_quycach.ToString());
        //                                    qc1.Add("price", (gia > 50000 ? (gia - 50000) : gia).ToString());

        //                                    // _qc.Add("price", gia.ToString());
        //                                    quycachgia.Add(qc1);
        //                                }
        //                                catch (Exception)
        //                                { }

        //                            }

        //                        }

        //                        j += 4;
        //                    }

        //                }

        //                var cateLv2 = new category();
        //                //if (!string.IsNullOrEmpty(gioiTinh.Keys.FirstOrDefault()) && !string.IsNullOrEmpty(gioiTinh.Values.FirstOrDefault()))
        //                //{
        //                //    cateLv2 = CreateCategory(listAllCate, cateLv1, $"{cateLv1.CategoryName} {gioiTinh.Values.FirstOrDefault()}", langDefault.Code, out string msg);
        //                //    if (cateLv2 != null && msg == "new")
        //                //    {
        //                //        listNewCate.Add(cateLv2);
        //                //        listAllCate.Add(cateLv2);
        //                //    }
        //                //}

        //                var cateLv3 = new category();
        //                //if (!string.IsNullOrEmpty(nhanHieu.Keys.FirstOrDefault()) && !string.IsNullOrEmpty(nhanHieu.Values.FirstOrDefault()))
        //                //{
        //                //    var cateTemp = CreateCategory(listAllCate, string.IsNullOrEmpty(cateLv2?.Id) ? cateLv1 : cateLv2, $"{(string.IsNullOrEmpty(cateLv2?.Id) ? cateLv1.CategoryName : string.Empty)} {nhanHieu.Values.FirstOrDefault()}", langDefault.Code, out string msg);
        //                //    if (cateTemp != null && msg == "new")
        //                //    {
        //                //        listNewCate.Add(cateTemp);
        //                //        listAllCate.Add(cateTemp);
        //                //    }

        //                //    if (string.IsNullOrEmpty(cateLv2?.Id))
        //                //    {
        //                //        cateLv2 = cateTemp;
        //                //    }
        //                //    else
        //                //    {
        //                //        cateLv3 = cateTemp;
        //                //    }
        //                //}

        //                if (string.IsNullOrEmpty(cateLv2?.Id) && string.IsNullOrEmpty(cateLv3?.Id))
        //                {
        //                    prod.CategoryId = cateLv1.ReId;
        //                    prod.CategoryName = cateLv1.CategoryName;
        //                }
        //                else
        //                {
        //                    prod.CategoryId = string.IsNullOrEmpty(cateLv3?.Id) ? cateLv2.ReId : cateLv3.ReId;
        //                    prod.CategoryName = string.IsNullOrEmpty(cateLv3?.Id) ? cateLv2.CategoryName : cateLv3.CategoryName;
        //                }
        //                prod.Url = CommonFunc.ConvertNonUnicodeURL(prod.CategoryName + "-" + prod.ProductName);
        //                prod.ImageAlt = prod.ProductName;
        //                prod.SEO_Title = prod.ProductName;
        //                prod.SEO_Desc = prod.ShortDescription;
        //                listProd.Add(prod);
        //            }

        //            //save hinh anh phu
        //            var data_hinhanhphu = new List<uploadmorefile>();
        //            foreach (var item in hinhanhphu)
        //            {
        //                for (int i = 0; i < 5; i++)
        //                {
        //                    if (!string.IsNullOrWhiteSpace(item["pic" + (i + 1)]))
        //                    {
        //                        data_hinhanhphu.Add(new uploadmorefile { UploadId = AppFunc.NewShortId(), FileName = item["pic" + (i + 1)], TableId = item["prodid"], LangCode = langDefault.Code, TableName = "products" });
        //                    }
        //                }

        //            }

        //            //save quy cach gia
        //            var data_quycach = new List<ProductOptionPrice>();
        //            foreach (var item in quycachgia)
        //            {
        //                var id_ = AppFunc.NewShortId();
        //                data_quycach.Add(new ProductOptionPrice { Id = id_, IsActive = true, LabelName = item["name"], Price = double.Parse(item["price"]), ProductId = item["prodid"], LangCode = langDefault.Code, ReId = id_ });
        //            }

        //            db.ProductOptionPrice.AddRange(data_quycach);
        //            db.uploadmorefiles.AddRange(data_hinhanhphu);
        //            db.categories.AddRange(listNewCate);
        //            db.products.AddRange(listProd);
        //            db.SaveChanges();
        //            trans.Commit();
        //            message = "Import dữ liệu thành công.";
        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            trans.Dispose();
        //            message = $"Import thất bại. Đã xảy ra lỗi: {ex.Message}";
        //            return false;
        //        }
        //    }
        //}

        internal static category CreateCategory(List<category> listCate, category cateParent, string cateName, string langCode, out string msg)
        {
            msg = "old";
            cateName = cateName?.Trim()?.ToLower();
            var cate = listCate.FirstOrDefault(x => x.CategoryName.Trim().ToLower() == cateName && x.Level == (cateParent.Level + 1) && x.ParentId == cateParent.Id && x.LangCode == langCode);
            if (cate == null)
            {
                cate = new category
                {
                    Id = AppFunc.NewShortId(),
                    CategoryName = cateName,
                    Level = cateParent.Level + 1,
                    MetaTitle = cateName,
                    MetaDesc = cateName,
                    Active = true,
                    Sellable = true,
                    LangCode = langCode,
                    ParentId = cateParent.Id,
                    ParentName = cateParent.CategoryName,
                };
                cate.Order = (listCate?.Max(x => x.Order) ?? 0) + 1;
                cate.ReId = cate.Id;
                cate.UrlCode = CommonFunc.ConvertNonUnicodeURL(cate.CategoryName);
                msg = "new";
            }
            return cate;
        }

        internal static List<string> GetAllChildCategory(string cateId)
        {
            using (var db = new AdminEntities())
            {
                var cateIds = new List<string> { cateId };
                var listCateId = cateIds;
                while (cateIds.Count > 0)
                {
                    var child_cate = db.categories.Where(c => cateIds.Contains(c.ParentId)).Select(c => c.Id).ToList();
                    listCateId.AddRange(child_cate);
                    cateIds = child_cate;
                }
                return db.categories.Where(c => listCateId.Contains(c.Id)).Select(c => c.ReId).ToList();
            }

        }

        internal static void AddOptionPrice(string productId, List<product_option> optionPrices, product_option_value[] optionPricesData)
        {
            var db = new AdminEntities();

            //remove
            var listid = optionPrices?.SelectMany(x => x.ListOption ?? new()).Select(x => x.id).Distinct().ToList() ?? new();
            listid.AddRange(optionPrices?.Select(x => x.id).Distinct().ToList() ?? new());
            db.product_option.RemoveRange(db.product_option.Where(x => x.ProductId == productId && !listid.Any(id => id == x.id)));
            var listDataId = optionPricesData?.Select(x => x.id).Distinct().ToList() ?? new();
            db.product_option_value.RemoveRange(db.product_option_value.Where(x => x.productId == productId && !listDataId.Any(id => id == x.id)));
            db.SaveChanges();
            //add and update option
            var rs = optionPrices?.Select(_group_option =>
            {
                var group_option = db.product_option.Find(_group_option.id) ?? new product_option { id = AppFunc.NewShortId() };
                group_option.name = _group_option.name;
                group_option.ProductId ??= productId;
                db.product_option.AddOrUpdate(group_option);
                return _group_option.ListOption?.Select(_option =>
                {
                    var option = db.product_option.Find(_option.id) ?? new product_option { id = AppFunc.NewShortId() };
                    option.name = _option.name;
                    option.ProductId ??= productId;
                    option.parentId ??= group_option.id;
                    option.parentName ??= group_option.name;
                    db.product_option.AddOrUpdate(option);
                    return option;
                }).ToList();
            }).ToList();

            //add and update option value
            int i = 0;
            decimal minprice = decimal.MaxValue;
            foreach (var x in rs?.FirstOrDefault() ?? new List<product_option> { new product_option() })
            {
                foreach (var y in rs?.Skip(1).FirstOrDefault() ?? new List<product_option> { new product_option() })
                {
                    var _option_data = optionPricesData?.ElementAtOrDefault(i);
                    if (_option_data != null)
                    {
                        var option_data = (_option_data.id > 0 ? db.product_option_value.Find(_option_data.id) : new()) ?? new();
                        option_data.option1 = x.id;
                        option_data.option1_name = x.parentName + ": "+ x.name;
                        option_data.option2 = y.id;
                        option_data.option2_name = y.parentName + ": " + y.name;
                        option_data.price = _option_data.price;
                        option_data.wholeSalePrice = _option_data.wholeSalePrice;
                        option_data.qty = _option_data.qty;
                        option_data.productId = productId;
                        db.product_option_value.AddOrUpdate(option_data);
                        if (_option_data.price < minprice)
                        {
                            minprice = _option_data.price;
                        }
                    }
                    i++;
                }
            }

            //update default price of product
            if (rs?.Count > 0)
            {
                db.products.Where(x => x.ReId == productId).ToList().ForEach(x =>
                {
                    x.Price = (double)minprice;
                    x.SalePrice = null;
                    db.Entry(x).State = EntityState.Modified;
                });
                db.SaveChanges();
            }
        }

        internal static void UpdatePropertyImgs(string productId,string langCode, string list_pic_properties)
        {
            var _db = new AdminEntities();
            #region Properties img
            _db.uploadmorefiles.RemoveRange(_db.uploadmorefiles.Where(x => x.TableId.StartsWith(productId + "|") && x.TableName == "Product_property"));
            var properties_file = JsonConvert.DeserializeObject<List<dynamic>>(list_pic_properties) ?? new();
            foreach (var file in properties_file)
            {
                _db.uploadmorefiles.Add(new uploadmorefile
                {
                    FileName = file.value,
                    LangCode = langCode,
                    TableId = productId + "|" + file.id,
                    TableName = "Product_property",
                    UploadId = Utils.AppFunc.NewShortId(),
                });
            }
            _db.SaveChanges();

            #endregion

        }
    }
}