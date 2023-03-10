using System.Web.Routing;
using Inner.Libs.Helpful;

namespace PKWebShop.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using Newtonsoft.Json;
    using PKWebShop.AppLB;
    using PKWebShop.DataAsset;
    using PKWebShop.Models;
    using PKWebShop.Models.CustomizeModels;
    using PKWebShop.Services;
    using PKWebShop.Utils;

    public class HomeController : ExpiredCheckController
    {
        private webconfiguration info = UserContent.GetWebInfomation();

        public ActionResult ChangeLanguage(string lang, string url)
        {
            Session.RemoveAll();
            SiteLang.SetLanguage(lang);
            return Redirect(url ?? "Index");
        }

        public ActionResult Index()
        {
            GetInfo();

            var webinfo = new WebShopEntities().webconfigurations.FirstOrDefault();
            if (!string.IsNullOrEmpty(webinfo.SEO_Meta))
            {
                // lấy đối tượng json trong SEO_meta từ webinfo sau đố chuyển thành một danh sách các đối tượng seo meta
                var list_SEO = JsonConvert.DeserializeObject<List<SiteSEO.SEO_Meta>>(webinfo.SEO_Meta);
                // từ danh sách các đối tượng seo meta ta chọn ra đối tượng seo meta trang chủ
                var SEO_trangchu = list_SEO.FirstOrDefault(x => x.code == SiteSEO.code_SEO.TrangChu);
                // truyền tất cả vào view
                ViewBag.Title = SEO_trangchu.meta_title;
                ViewBag.SEO_Description = SEO_trangchu.meta_desc;
                ViewBag.SEO_MetaKeyWord = SEO_trangchu.meta_keyword;
            }
            ViewBag.WebInfo = webinfo;

            return View();
        }

        public ActionResult _AccountAuth()
        {
            return PartialView();
        }

        public ActionResult SignUp(customer cus)
        {
            try
            {
                customer newCus = new CustomerService().Register(cus);
                DA_Customer dA_cus = new(new WebShopEntities());
                if (dA_cus.CustomerLogin(newCus, out string errMsg) != null)
                {
                    return Json(new object[] { true, "Successful" });
                }
                throw new Exception(errMsg);
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, "Đăng ký thất bại! " + ex.Message });
            }
        }

        #region Loc Inactive 01/07/2021
        //[HttpPost]
        //public JsonResult RegisterSubmit(customer data)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(data.FullName) || string.IsNullOrWhiteSpace(data.FullName))
        //        {
        //            throw new Exception("Họ tên không được trống");
        //        }
        //        if (string.IsNullOrEmpty(data.Password) || string.IsNullOrWhiteSpace(data.Password))
        //        {
        //            throw new Exception("Password không được trống");
        //        }
        //        if (string.IsNullOrEmpty(data.Email) || string.IsNullOrWhiteSpace(data.Email))
        //        {
        //            throw new Exception("Email không được trống");
        //        }

        //        DA_Customer dA_cus = new(new WebShopEntities());
        //        if (dA_cus.CustomerLogin(new CustomerService().Register(data), out string errMsg) != null)
        //        {
        //            return Json(new object[] { true, "Đăng ký thành công" });
        //        }
        //        throw new Exception(errMsg);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new object[] { false, "Đăng ký thất bại! " + ex.Message });
        //    }
        //}
        #endregion

        [HttpPost]
        public ActionResult LoginSubmit(customer data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.Email))
                {
                    throw new Exception("Vui lòng nhập Email");
                }

                if (string.IsNullOrWhiteSpace(data.Password))
                {
                    throw new Exception("Password không được trống");
                }

                DA_Customer dA_cus = new(new WebShopEntities());
                if (dA_cus.CustomerLogin(data.Email, data.Password, out string errMsg) != null)
                {
                    return Json(new object[] { true });
                }
                throw new Exception(errMsg);
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message, ex.ToString() });
            }
        }

        public ActionResult CustomerLogout()
        {
            HttpCookie ck = Request.Cookies["Customer"];
            if (ck != null)
            {
                ck.Value = null;
                ck.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(ck);
            }

            Session.Abandon();
            return RedirectToAction("index");
        }

        [HttpPost]
        public JsonResult CusForgotPassword(string fg_email, string fg_pass, string fg_retype_pass)
        {
            try
            {
                string email = fg_email ?? string.Empty;
                string pass = fg_pass ?? string.Empty;
                string retype_pass = fg_retype_pass ?? string.Empty;
                if (email == string.Empty || pass == string.Empty || retype_pass == string.Empty)
                {
                    throw new Exception("Vui lòng nhập đầy đủ thông tin");
                }
                else if (pass != retype_pass)
                {
                    throw new Exception("Mật khẩu không trùng khớp vui lòng nhập lại mật khẩu");
                }

                WebShopEntities db = new WebShopEntities();
                var customer = db.customers.FirstOrDefault(x => x.Email == email);
                if (customer == null)
                {
                    throw new Exception("Tài khoản không tồn tại");
                }

                #region SendEmail
                string link_confirm = $"{Request.Url.Scheme}://{Request.Url.Authority}/home/confirmforgotpassword?id={Uri.EscapeDataString(SecurityLibrary.Encrypt(DateTime.Now.ToString() + "|" + customer.Id + "|" + pass))}";
                string subject = $"[{ConfigurationManager.AppSettings["Domain"]}] Xác nhận thay đổi mật khẩu";
                string body =
                    "<table cellpadding='20' cellspacing='0' style='width: 600px;margin-left: auto;margin-right: auto;border: 1px solid #dedede; border-radius: 3px;'><thead><tr><td style='padding: 12px 48px; background-color: #0193de; border-bottom: 0; font-weight: bold; border-radius: 3px 3px 0 0;'><h1 style='font-size:22px;font-weight:300;line-height:150%;margin:0;text-align:left;color:#ffffff;background-color:inherit;text-align:center;'>Xác nhận thông tin</h1></td></tr></thead><tbody><tr><td valign='top' style='padding:32px 48px 32px'><div style='color:#636363;font-family:&quot;Helvetica Neue&quot;,Helvetica,Roboto,Arial,sans-serif;font-size:14px;line-height:150%;text-align:left'>" +
                    "<p>Xin chào " + customer.FullName + ",</p>" +
                    "<p>Bạn nhận được thông báo này vì bạn đã thay đổi mật khẩu tại " + ConfigurationManager.AppSettings["Domain"] + ".</p>" +
                    "<p>Vui lòng click vào nút xác nhận bên dưới để thay đổi mật khẩu.</p><br/>" +
                    "<p><a href='" + link_confirm + "' style='text-decoration:none; border:none; color:white; background-color:#007bff; padding:10px 12px;'>Xác nhận thay đổi mật khẩu</a></p>" +
                    "<p><i>email có hiệu lực trong 15 phút</i></p>" +
                    "</div></td></tr></tbody><tfoot style='background-color: #eff1f3;'><tr><td style='text-align: center;padding: 10px; font-size: 12px;font-family:&quot;Helvetica Neue&quot;,Helvetica,Roboto,Arial,sans-serif;'> Đây là email liên hệ của Khách hàng từ website: " + ConfigurationManager.AppSettings["Domain"] + "</td></tr></tfoot></table>";

                var result = SendEmail.Send(customer.Email, subject, body);
                if (result != string.Empty)
                {
                    throw new Exception("Đã sự cố xảy ra. Vui lòng quay lại sau.");
                }
                #endregion

                return Json(new object[] { true, "Vui lòng kiểm tra email và xác nhận thay đổi mật khẩu." });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        /// <summary>
        /// Khách hàng xác nhận thay đổi mật khẩu.
        /// </summary>
        /// <param name="id">key.</param>
        /// <returns></returns>
        public ActionResult ConfirmForgotPassword(string id)
        {
            try
            {
                var data = SecurityLibrary.Decrypt(Uri.UnescapeDataString(id ?? string.Empty)).Split('|');
                if (DateTime.TryParse(data[0], out DateTime datetime))
                {
                    TimeSpan interval = DateTime.Now - datetime;
                    if (interval.TotalMinutes <= 15)
                    {
                        WebShopEntities db = new WebShopEntities();
                        string cusId = data[1] ?? string.Empty;
                        var customer = db.customers.FirstOrDefault(x => x.Id == cusId);
                        if (customer != null)
                        {
                            customer.Password = data[2] ?? string.Empty;
                            db.Entry(customer).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();

                            #region SendEmail
                            string subject = $"[{ConfigurationManager.AppSettings["Domain"]}] Thông tin tài khoản của bạn";
                            string body =
                                "<table cellpadding='20' cellspacing='0' style='width: 600px;margin-left: auto;margin-right: auto;border: 1px solid #dedede; border-radius: 3px;'><thead><tr><td style='padding: 12px 48px; background-color: #0193de; border-bottom: 0; font-weight: bold; border-radius: 3px 3px 0 0;'><h1 style='font-size:22px;font-weight:300;line-height:150%;margin:0;text-align:left;color:#ffffff;background-color:inherit;text-align:center;'>Thông tin tài khoản</h1></td></tr></thead><tbody><tr><td valign='top' style='padding:32px 48px 32px'><div style='color:#636363;font-family:&quot;Helvetica Neue&quot;,Helvetica,Roboto,Arial,sans-serif;font-size:14px;line-height:150%;text-align:left'>" +
                                "<p>Xin chào " + customer.FullName + ",</p>" +
                                "<p>Bạn nhận được thông báo này vì bạn đã thay đổi mật khẩu tại " + ConfigurationManager.AppSettings["Domain"] + ".</p>" +
                                "<p>Dưới đây là thông tin tài khoản của bạn:</p>" +
                                "<p><b>Tên tài khoản:</b> " + customer.Email + "</p>" +
                                "<p><b>Mật khẩu mới:</b> " + customer.Password + "</p>" +
                                "</div></td></tr></tbody><tfoot style='background-color: #eff1f3;'><tr><td style='text-align: center;padding: 10px; font-size: 12px;font-family:&quot;Helvetica Neue&quot;,Helvetica,Roboto,Arial,sans-serif;'> Đây là email liên hệ của Khách hàng từ website: " + ConfigurationManager.AppSettings["Domain"] + "</td></tr></tfoot></table>";

                            var result = SendEmail.Send(customer.Email, subject, body);
                            #endregion

                            TempData["ForgotPasswordNoty"] = "Đổi mật khẩu thành công. Vui lòng kiểm tra email.";
                        }
                        else
                        {
                            throw new Exception("Đổi mật khẩu thất bại. Tài khoản không tồn tại.");
                        }
                    }
                    else
                    {
                        throw new Exception("Đổi mật khẩu thất bại. Thời gian xác nhận đổi mật khẩu đã hết. Vui lòng thử lại.");
                    }
                }
                else
                {
                    throw new Exception("Đã có sự cố xảy ra. Vui lòng quay lại sau.");
                }
            }
            catch (Exception ex)
            {
                TempData["ForgotPasswordNoty"] = ex.Message;
            }
            return Redirect("/trang-chu");
        }

        /// <summary>
        /// get data tinh nang trang chu.(truyền dữ liệu vào viewBag)
        /// </summary>
        private void GetInfo()
        {
            using (var db = new DBLangCustom())
            {
                // lấy danh sách các tính năng
                var sFeatures = new DA_SectionFeatures().getAllFeatures();
                // thêm các tính năng vào sFeatures_view()
                sFeatures = new DA_SectionFeatures().addFixedFeatures(sFeatures);
                // thêm các sản phẩm vào sự kiện
                sFeatures = new DA_SectionFeatures().addFixedProducts(sFeatures);
                // truyền vào view
                ViewBag.sFeatures = sFeatures;
                ViewBag.quangcao = db.popupads.Where(p => !(p.PopupAdsFrom > DateTime.Now) && !(p.PopupAdsTo < DateTime.Now) && p.Active != false).OrderByDescending(p => p.Id).FirstOrDefault();
            }
        }

        public ActionResult LoadProductPartial(string cateId, string order, string partialName, int take = 8)
        {
            using (var db = new DBLangCustom())
            {
                try
                {
                    var listProd = db.products.Where(x => x.ShowHomePage == true).AsQueryable();
                    if (!string.IsNullOrEmpty(cateId))
                    {
                        var all_ChildCate = UserContent.GetAllChildCategory(cateId);
                        listProd = listProd.Where(p => all_ChildCate.Contains(p.CategoryId)).OrderByDescending(x => x.CreateAt);
                    }
                    if (!string.IsNullOrEmpty(order))
                    {
                        if (order == "sanphambanchay")
                        {
                            listProd = listProd.Where(s => s.Sold > 0).OrderByDescending(s => s.Sold);
                        }
                        else if (order == "sanphamkhuyenmai")
                        {
                            listProd = listProd.Where(s => s.SalePrice > 0 && s.SalePrice < s.Price).OrderBy(s => (s.SalePrice ?? s.Price) / s.Price);
                        }
                    }
                    return PartialView("components/product/" + partialName, listProd.Take(take).ToList());
                }
                catch (Exception ex)
                {
                    return PartialView("components/product/" + partialName, new List<product>());
                }
            }
        }
        public ActionResult LoadProductHome()
        {
            using (var db = new DBLangCustom())
            {
                try
                {
                    string s_sanpham = UserContent.Web_Feature.trangchu_sanpham.ToString();
                    var sp = db.sectionfeatures.Where(s => s.ReId == s_sanpham && s.Status == true).FirstOrDefault();
                    if (sp != null)
                    {
                        var listCate = db.categories.Where(x => x.Active == true);
                        var listProd = db.products.Where(x => x.ShowHomePage == true).OrderByDescending(x => x.CreateAt);
                        var list_ProductHome = new List<ProductsHome>();

                        foreach (var f_detail in db.sectionfeaturedetails.Where(sd => sd.SectionCode == s_sanpham).OrderBy(x => x.VolumeNumber).ToList())
                        {
                            var productHome = new ProductsHome()
                            {
                                Section_detail = f_detail,
                                Pics = db.uploadmorefiles.Where(u => u.TableId == f_detail.Id && u.TableName == "sectionfeaturedetails").ToList(),
                                Cate_product = new List<CateProduct>(),
                            };

                            if (f_detail.URL.Contains("-cte"))
                            {
                                var reId = f_detail.URL.Split('-').LastOrDefault()?.Substring(2);
                                var cateParent = listCate.FirstOrDefault(x => x.ReId == reId);
                                if (cateParent != null)
                                {
                                    foreach (var c_lv2 in listCate.Where(x => x.ParentId == cateParent.ReId).ToList())
                                    {
                                        var listCateReId = new List<string>();
                                        var cateProduct = new CateProduct()
                                        {
                                            Cate = new category(),
                                            Products = new List<product>(),
                                        };

                                        cateProduct.Cate = c_lv2;
                                        listCateReId.Add(c_lv2.ReId);
                                        foreach (var c_lv3 in listCate.Where(x => x.ParentId == c_lv2.ReId).ToList())
                                        {
                                            listCateReId.Add(c_lv3.ReId);
                                        }

                                        var cleanReId = listCateReId.Distinct();
                                        cateProduct.Products.AddRange(listProd.Where(x => cleanReId.Contains(x.CategoryId)).Take(10));
                                        productHome.Cate_product.Add(cateProduct);

                                    }
                                    list_ProductHome.Add(productHome);
                                }
                            }
                            else
                            {
                                var prods = listProd;
                                if (f_detail.URL.Contains("sanphambanchay"))
                                {
                                    prods = listProd.Where(s => s.Sold > 0).OrderByDescending(s => s.Sold);
                                }
                                else if (f_detail.URL.Contains("sanphamkhuyenmai"))
                                {
                                    prods = listProd.Where(s => s.SalePrice > 0 && s.SalePrice < s.Price).OrderBy(s => (s.SalePrice ?? s.Price) / s.Price);
                                }

                                foreach (var c_lv1 in listCate.Where(x => x.Level == 1).ToList())
                                {
                                    var listCateReId = new List<string>();
                                    var cateProduct = new CateProduct()
                                    {
                                        Cate = new category(),
                                        Products = new List<product>(),
                                    };

                                    cateProduct.Cate = c_lv1;
                                    listCateReId.Add(c_lv1.ReId);
                                    foreach (var c_lv2 in listCate.Where(x => x.ParentId == c_lv1.ReId).ToList())
                                    {
                                        listCateReId.Add(c_lv2.ReId);
                                        foreach (var c_lv3 in listCate.Where(x => x.ParentId == c_lv2.ReId).ToList())
                                        {
                                            listCateReId.Add(c_lv3.ReId);
                                        }
                                    }

                                    var cleanReId = listCateReId.Distinct();
                                    cateProduct.Products.AddRange(prods.Where(x => cleanReId.Contains(x.CategoryId)).Take(8));
                                    productHome.Cate_product.Add(cateProduct);
                                }
                                list_ProductHome.Add(productHome);
                            }
                        }
                        return PartialView("_ProductHomePartial", list_ProductHome);
                    }
                    return PartialView("_ProductHomePartial", new List<ProductsHome>());
                }
                catch (Exception ex)
                {
                    return PartialView("_ProductHomePartial", new List<ProductsHome>());
                }
            }
        }
        [AllowAnonymous]
        public ActionResult getFooterMenu()
        {
            using (var db = new DBLangCustom())
            {
                string s_sanpham = UserContent.Web_Feature.trangchu_sanpham.ToString();
                var list = new List<(string title, List<(string name, string url)> items)>();
                var cate = db.categories.Where(x => string.IsNullOrEmpty(x.ParentId)).OrderBy(x => x.Order).ToList().Select(p => (name: p.CategoryName, url: $"{p.UrlCode}-cte{p.ReId}")).Take(4).ToList();
                //var topic_tintuc = db.n_parent_topic.FirstOrDefault(t => t.URL == "huong-dan");
                //var a = db.n_news.Where(n => n.Active != false && n.ParentTopicId == topic_tintuc.ReId).OrderByDescending(n => n.Order).ToList();
                //var Tintuc_list = db.n_news.Where(n => n.Active != false && n.ParentTopicId == topic_tintuc.ReId).OrderByDescending(n => n.Order).ThenByDescending(n => n.CreatedAt).Take(6)
                //    .ToList().Select(n => (name: n.Name, url: n.UrlCode + "-nd" + n.ReId)).ToList();

                //list.Add(("Hỗ trợ khách hàng", Tintuc_list));
                list.Add(("Danh mục nổi bật", cate));
                return PartialView("_FooterMenu", list);
            }
        }

        public ActionResult NotFound()
        {
            DBLangCustom db = new();

            // Get top background image
            ViewBag.page_slide = (from f in db.sectionfeaturedetails
                                  join s in db.sectionfeatures on f.SectionCode equals s.ReId
                                  where s.Status == true && s.ReId == UserContent.Web_Feature.TopBackground.ToString()
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

            return View();
        }

        public ActionResult RegisterSuccess()
        {
            return View();
        }

        #region JQuery Load Data
        public JsonResult LoadProvince()
        {
            try
            {
                using (var db = new WebShopEntities())
                {
                    var listProvince = db.vn_province.OrderBy(x => x.name).ToList();

                    return Json(new object[] { true, listProvince }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        /// <summary>
        /// load quan/huyen theo tinh/tp.
        /// </summary>
        /// <param name="province">ProvinceId|ProvinceName.</param>
        /// <returns></returns>
        public JsonResult LoadDistrict(string province)
        {
            try
            {
                using (var db = new WebShopEntities())
                {
                    var id = province?.Split('|')[0];
                    var listDistrict = db.vn_district.Where(x => x.provinceid == id).OrderByDescending(x => x.type).ToList();
                    return Json(new object[] { true, listDistrict }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        /// <summary>
        /// load xã/phường theo quan/huyen.
        /// </summary>
        /// <param name="district">ProvinceId|ProvinceName.</param>
        /// <returns>json.</returns>
        public JsonResult LoadWard(string district)
        {
            try
            {
                using (var db = new WebShopEntities())
                {
                    var listDistrict = db.vn_ward.Where(x => x.districtid == district).OrderBy(x => new { x.type, x.name }).ToList();
                    return Json(new object[] { true, listDistrict }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }
        /*public JsonResult LoadSchool()
        {
            try
            {
                using (var db = new WebShopEntities())
                {
                    var listSchool = db.giasu_option.Where(x => x.TypeName.Equals("trường đào tạo")).Select(x => x.Name).ToArray();
                    return Json(new object[] { true, listSchool });
                }
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        public JsonResult LoadMajors()
        {
            try
            {
                using (var db = new WebShopEntities())
                {
                    var listMajors = db.giasu_option.Where(x => x.TypeName.Equals("chuyên ngành")).Select(x => x.Name).ToArray();
                    return Json(new object[] { true, listMajors });
                }
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }*/
        #endregion

        public ActionResult Contact()
        {
            ViewBag.topbg = CommonFunc.getTopBackground();
            return View();
        }

        public ActionResult BookingConfirm(string Start, string End, string Adults, string Children)
        {
            ViewBag.topbg = CommonFunc.getTopBackground();
            var db = new DBLangCustom();
            ViewBag.Roomtype = db.services.ToList() ?? new List<service>();
            ViewBag.Start = Start;
            ViewBag.End = End;
            ViewBag.Adults = Adults;
            ViewBag.Children = Children;
            return View();
        }

        public ActionResult Thanks(string Id = "")
        {
            ViewBag.topbg = CommonFunc.getTopBackground();
            var db = new DBLangCustom();
            string t = UserContent.Web_Feature.thankyou.ToString();
            var slice = db.sectionfeatures.Where(s => s.ReId == t && s.Status == true).FirstOrDefault();
            if (slice == null)
            {
                return View(new sectionfeaturedetail());
            }
            var slide_list = db.sectionfeaturedetails.Where(f => f.SectionCode == slice.ReId).OrderBy(f => f.VolumeNumber)
                        .Select(f => new { f, pics = db.uploadmorefiles.Where(u => u.TableId == f.Id && u.TableName == "sectionfeaturedetails").ToList() })
                        .AsEnumerable().Select(f => (f.f, f.pics)).FirstOrDefault();

            #region LOC UPDATE 06/10/2021
            if (!string.IsNullOrEmpty(Id))
            {
                var order = db.orders.FirstOrDefault(x => x.Id == Id);
                if (order != null)
                {
                    var language_default = SiteLang.GetDefault();
                    var listProductItem = db.orders_detail.Where(x => x.OrderId == Id).ToList();
                    string listParentProp = string.Join(",", listProductItem.GroupBy(x => x.ParentPropertiesId).Select(x => x.Key).Distinct());
                    string listChildProp = string.Join(",", listProductItem.GroupBy(x => x.PropertiesId).Select(x => x.Key).Distinct());

                    var listProp = (from x in db.product_properties
                                    where (listParentProp.Contains(x.ReId) && x.LangCode == language_default.Code)
                                    || (listChildProp.Contains(x.ReId) && x.LangCode == language_default.Code)
                                    orderby x.name
                                    select x).ToList();

                    ViewBag.ListProp = listProp;
                    ViewBag.ListOrderDetail = listProductItem;
                    ViewBag.Order = order;
                }
            }
            #endregion

            return View(slide_list);
        }

        public ActionResult Expired()
        {
            return View();
        }

        #region COMMENT - LOC UPDATE 25/11/2021

        /// <summary>
        /// Load Số lượng sao trung bình (số sao/số comment).
        /// </summary>
        /// <param name="mappingId">ReId.</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ActionResult LoadStrarNumber(string mappingId, string type)
        {
            using (var db = new WebShopEntities())
            {
                double? starNumber = 0;
                int? commNumber = 0;
                var listComm = db.comments.Where(x => x.MappingId == mappingId && x.Type == type && x.IsActive ==true);
                if (listComm != null && listComm.Count() > 0)
                {
                    starNumber = listComm.Sum(x => x.StarNumber) / listComm.Count();
                    commNumber = listComm.Count();
                }

                ViewBag.StarNumber = starNumber;
                ViewBag.CommNumber = commNumber;
                return PartialView("comment/_StarNumberPartial");
            }
        }

        /// <summary>
        /// Load Comments.
        /// </summary>
        /// <param name="mappingId">ReId.</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ActionResult LoadComments(string mappingId, string type)
        {
            using (var db = new WebShopEntities())
            {
                var listComm = db.comments.Where(x => x.MappingId == mappingId && x.Type == type && x.IsActive == true).OrderByDescending(x => x.CreateAt).ToList();
                return PartialView("comment/_ListComments", listComm);
            }
        }

        [HttpPost]
        public JsonResult AddComment()
        {
            try
            {
                var reId = Request["ReId"];
                var type = Request["Type"] ?? string.Empty;
                var content = Request.Unvalidated["Content"]?.Trim();
                var peopleName = Request["PeopleName"]?.Trim();
                var email = Request["Email"]?.Trim();

                if (string.IsNullOrEmpty(content))
                {
                    throw new Exception("Vui lòng nhập nhận xét của bạn");
                }
                if (string.IsNullOrEmpty(peopleName))
                {
                    throw new Exception("Vui lòng nhập Tên");
                }
                if (string.IsNullOrEmpty(email))
                {
                    throw new Exception("Vui lòng nhập Email");
                }
                if (CommonFunc.IsValidEmail(email) == false)
                {
                    throw new Exception("Email không hợp lệ");
                }

                var ck_key = $"{Session.SessionID}_{type}";
                var ck = Request.Cookies.Get(ck_key);
                if (ck != null)
                {
                    throw new Exception("Bạn vừa đánh giá thành công. Vui lòng quay lại sau 60 phút.");
                }

                var db = new WebShopEntities();
                if (type == "news")
                {
                    if (!db.n_news.Any(x => x.ReId == reId))
                    {
                        throw new Exception("Bài viết không tồn tại");
                    }
                }
                else
                {
                    if (!db.products.Any(x => x.ReId == reId))
                    {
                        throw new Exception("Sản phẩm không tồn tại");
                    }
                }

                var comm = new comments()
                {
                    Id = AppFunc.NewShortId(),
                    MappingId = reId,
                    Type = type,
                    PeopleName = peopleName,
                    StarNumber = 5,
                    Content = content,
                    Email = email,
                    CreateAt = DateTime.Now,
                    IsActive = false,
                };

                if (double.TryParse(Request["StarNumber"], out double sNumber) && sNumber > 0 && sNumber <= 5)
                {
                    comm.StarNumber = sNumber;
                }
                db.comments.Add(comm);
                db.SaveChanges();

                ck = new(ck_key);
                ck.Value = email;
                ck.Expires = DateTime.Now.AddMinutes(60);
                Response.Cookies.Add(ck);

                return Json(new object[] { true, "Đánh giá thành công" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }
        #endregion
        
    }
}