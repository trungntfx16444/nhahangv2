namespace PKWebShop.DataAsset
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using PKWebShop.Models;
    using PKWebShop.Models.CustomizeModels;
    using PKWebShop.AppLB;
    
    // section hien thi
    public class DA_SectionFeatures
    {
        // Khoi tao Constructors
        private WebShopEntities db;
        
       
        public DA_SectionFeatures()
        {
            db = new WebShopEntities();
        }

        public DA_SectionFeatures(WebShopEntities dataModel)
        {
            db = dataModel;
        }
           
        // lấy dict toàn bộ tính năng trong bảng sectionfeature
        #region Function
        public IDictionary<UserContent.Web_Feature, sFeatures_view> getAllFeatures()
        {
            // trả về danh sách chuỗi các feature
            var code = EnumFunction.GetList<UserContent.Web_Feature>().Select(f => f.ToString());

            // lây danh sách các tính năng dựa trên sf.reId trong danh sách string code trong bảng sectionfeatures
            var sf = db.sectionfeatures.Where(s => code.Contains(s.ReId) && s.Status == true && s.Hidden != true);

            var sf2 = sf.GroupJoin(db.sectionfeaturedetails, f => f.ReId, d => d.SectionCode, (f, group_d) =>
            new sFeatures_view
            {
                Feature = f,
                Details = group_d.GroupJoin(db.uploadmorefiles.Where(u => u.TableName == "sectionfeaturedetails"), d => d.Id, f => f.TableId, (d, group_f) => new featuredetail_view
                {
                    Detail = d,
                    Files = group_f.ToList(),
                }).OrderBy(d => d.Detail.VolumeNumber).ToList(),
            }).OrderBy(f => f.Feature.Order).ToList().ToDictionary(f => f.Feature.Code.ToEnum<UserContent.Web_Feature>(), f => f);
            return sf2;
        }

        public sFeatures_view getViewFeature(UserContent.Web_Feature f)
        {
            string code = f.ToString();
            var sf = db.sectionfeatures.Where(s => s.ReId == code && s.Status == true).FirstOrDefault();
            if (sf != null)
            {
                var result = new sFeatures_view();
                result.Feature = sf;
                result.Details = db.sectionfeaturedetails.Where(f => f.SectionCode == sf.ReId).OrderBy(f => f.VolumeNumber)
                    .Select(f => new { f, pics = db.uploadmorefiles.Where(u => u.TableId == f.Id && u.TableName == "sectionfeaturedetails").ToList() })
                    .AsEnumerable().Select(f => new featuredetail_view { Detail = f.f, Files = f.pics }).ToList();
                return result;
            }
            return null;
        }

        // thêm hoặc cập nhật các đối tượng sfeatures_view
        internal IDictionary<UserContent.Web_Feature, sFeatures_view> addFixedFeatures(IDictionary<UserContent.Web_Feature, sFeatures_view> sFeatures)
        {
            if (sFeatures.ContainsKey(UserContent.Web_Feature.trangchu_tintuc))
            {
                sFeatures[UserContent.Web_Feature.trangchu_tintuc].Details =
                db.n_news.Where(n => (n.ParentTopicId == "2") && n.Active == true).OrderByDescending(n => n.Order).ThenByDescending(n => n.CreatedAt).ToList()
                .Select(n => new featuredetail_view
                {
                    Detail = new sectionfeaturedetail { Id = n.ParentTopicId, Title = n.Name, Description = n.ShortDescription, URL = "/" + n.UrlCode + "-nd" + n.ReId, CreateAt = n.CreatedAt, },
                    Files = new List<uploadmorefile> { new uploadmorefile { FileName = n.Picture } },
                }).Take(10).ToList();
            }
            if (sFeatures.ContainsKey(UserContent.Web_Feature.trangchu_duan))
            {
                sFeatures[UserContent.Web_Feature.trangchu_duan].Details = db.n_toppic.Where(t => t.ParentTopicId == "3").ToList().Select(t => new featuredetail_view
                {
                    Detail = new sectionfeaturedetail { Id = t.ReId, Title = t.Name, URL = t.URL },
                    Data = db.n_news.Where(n => n.TopicId.Contains(t.ReId)).Take(4).ToList() ?? new(),
                }).ToList() ?? new();
            }
            return sFeatures;
        }
           
        // các sản phẩm vào sự kiện đặc biệt của trang chủ
        internal IDictionary<UserContent.Web_Feature, sFeatures_view> addFixedGroupProduct(IDictionary<UserContent.Web_Feature, sFeatures_view> sFeatures)
        {
            if (sFeatures.ContainsKey(UserContent.Web_Feature.trangchu_sanpham))
            {
                foreach (var c in sFeatures[UserContent.Web_Feature.trangchu_sanpham].Details)
                {
                    if (c.Detail.URL.Contains("?order=sanphambanchay"))
                    {
                        List<(category, List<product>)> rs = new();
                        rs.Add((new category(), db.products.Where(x => x.IsActive != false && x.ShowHomePage != false && x.Sellable == true).OrderByDescending(p => p.Sold).Take(16).ToList()));
                        c.Data = rs;
                    }
                    else if (c.Detail.URL.Contains("?order=sanphamkhuyenmai"))
                    {
                        List<(category, List<product>)> rs = new()
                        {
                            (new category(), db.products.Where(x => x.IsActive != false && x.ShowHomePage != false && x.Sellable == true && x.SalePrice > 0).OrderBy(p => p.SalePrice / p.Price).Take(16).ToList()),
                        };
                        c.Data = rs;
                    }
                    else if (c.Detail.URL.Contains("?order=sanphamnoibat"))
                    {
                        List<(category, List<product>)> rs = new()
                        {
                            (new category(), db.products.Where(x => x.IsActive != false && x.ShowHomePage != false && x.Sellable == true).OrderByDescending(p => p.Order).Take(16).ToList()),
                        };
                        c.Data = rs;
                    }
                    else if (c.Detail.URL.Contains("?order=sanphammoi"))
                    {
                        List<(category, List<product>)> rs = new()
                        {
                            (new category(), db.products.Where(x => x.IsActive != false && x.ShowHomePage != false && x.Sellable == true).OrderByDescending(p => p.CreateAt).Take(16).ToList()),
                        };
                        c.Data = rs;
                    }
                    else
                    {
                        var cateId = c.Detail.URL.Split(new string[] { "-pc" }, StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);
                        List<(category, List<(category, uploadmorefile)>)> rs = new();
                        rs.Add((new category(), db.categories.Where(x => x.Active != false && x.Sellable == true & x.ParentId == cateId)
                            .GroupJoin(db.uploadmorefiles, c => c.Id, u => u.TableId, (c, u) => new { c, file = u.FirstOrDefault() })
                            .OrderBy(p => p.c.Order).AsEnumerable().Select(x => (x.c, x.file)).ToList()));
                        c.Data = rs;
                    }

                }
            }
            return sFeatures;
        }
        internal IDictionary<UserContent.Web_Feature, sFeatures_view> addFixedProducts(IDictionary<UserContent.Web_Feature, sFeatures_view> sFeatures)
        {
            if (sFeatures.ContainsKey(UserContent.Web_Feature.trangchu_sanpham))
            {
                foreach (var c in sFeatures[UserContent.Web_Feature.trangchu_sanpham].Details)
                {
                    var cateId = c.Detail.URL.Split(new string[] { "-pc" }, StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);
                    var order = c.Detail.URL.Contains("-pc") ? string.Empty : c.Detail.URL.Split('?').ElementAtOrDefault(1);
                    var list = db.products.Where(x => x.ShowHomePage == true && x.IsActive == true).AsQueryable();
                    if (!string.IsNullOrEmpty(cateId))
                    {
                        var all_ChildCate = UserContent.GetAllChildCategory(cateId);
                        if (all_ChildCate.Count > 1)
                        {
                            list = list.Where(p => all_ChildCate.Any(x => p.CategoryId.Contains(x)));
                        }
                        else if (all_ChildCate.Count == 1)
                        {
                            var childc = all_ChildCate[0];
                            list = list.Where(p => p.CategoryId.Contains(childc));
                        }
                        list = list.OrderByDescending(x => x.CreateAt);
                    }
                    if (!string.IsNullOrEmpty(order))
                    {
                        if (order == "order=sanphambanchay")
                        {
                            list = list.Where(s => s.Sold > 0).OrderByDescending(s => s.Sold);
                        }
                        else if (order == "order=sanphamkhuyenmai")
                        {
                            list = list.Where(s => s.SalePrice > 0 && s.SalePrice < s.Price).OrderBy(s => (s.SalePrice ?? s.Price) / s.Price);
                        }
                        else if (order == "order=sanphamnoibat")
                        {
                            list = list.OrderByDescending(s => s.Order);
                        }
                        else if (order == "order=sanphammoi")
                        {
                            list = list.OrderByDescending(s => s.CreateAt);
                        }
                    }
                    c.Data = list.Take(10).ToList() ?? new();
                    c.Detail.Id = cateId;
                }
            }
            return sFeatures;
        }

        #endregion
    }
}