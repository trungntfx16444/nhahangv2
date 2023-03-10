namespace ClientPage.DataAsset
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using System.Web;
    using Microsoft.Ajax.Utilities;
    using Google.Protobuf.WellKnownTypes;
    using PKWebShop.Models;
    using PKWebShop.Services;
    using PKWebShop.AppLB;

    public class DA_CartDetail : IDisposable
    {
        // Khoi tao Constructors
        private WebShopEntities db = new WebShopEntities();
        public string cusId;

        public DA_CartDetail(string customerId = null)
        {
            cusId = customerId;
            if (string.IsNullOrEmpty(cusId))
            {
                cusId = getCookieId();
            }
        }

        public string getCookieId()
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies["g_cus_id"] ?? new HttpCookie("g_cus_id");
            cookie.Value ??= Guid.NewGuid().ToString();
            cookie.Expires = DateTime.UtcNow.AddDays(30);
            HttpContext.Current.Response.Cookies.Add(cookie);
            return cookie.Value;
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public cart_detail AddToCart(string ProductId, int? qty, string type, string PriceOpt1 = null, string PriceOpt2 = null, List<string> Properties = null)
        {
            if (type == "Product")
            {
                var lang = SiteLang.GetCurrentLang();
                var product = db.products.FirstOrDefault(p => p.ReId == ProductId && p.LangCode == lang);
                if (product == null || !(product.Price > 0))
                {
                    throw new Exception("Không tìm thấy sản phẩm!");
                }

                var properties = string.Join("|", Properties ?? new());
                var detail = db.cart_details.FirstOrDefault(x => x.CustomerId == cusId && x.ItemId == ProductId && x.PriceOpt1 == PriceOpt1 && x.PriceOpt2 == PriceOpt2 && x.Properties == properties) ?? new();
                // Nếu số lượng không lớn hơn không
                if (!(detail.Quantity + (qty ?? 1) > 0))
                {
                    // xóa detail nếu có.
                    if (detail.CreatedAt != null)
                    {
                        RemoveFromCart(detail.Id);
                    }

                    return null;
                }

                detail.Quantity += qty ?? 1;
                detail.Type = type;
                detail.ItemId = ProductId;
                detail.CustomerId = cusId;
                detail.PriceOpt1 = PriceOpt1;
                detail.PriceOpt2 = PriceOpt2;
                detail.Properties = properties;
                detail.CreatedAt ??= DateTime.Now;
                db.cart_details.AddOrUpdate(detail);
                db.SaveChanges();
                return detail;
            }
            else
            {
                var giftcode = db.gift_code.FirstOrDefault(p => p.code == ProductId && p.active == true);
                if (giftcode == null || giftcode.start_date > DateTime.Now.Date)
                {
                    throw new Exception("Mã giảm giá không đúng!");
                }

                if (giftcode.end_date <= DateTime.Now.Date)
                {
                    throw new Exception("Mã giảm giá đã hết hạn!");
                }


                var total_of_shop = GetCart()?.Sum(c => c.Price * c.Quantity);
                if ((decimal?)total_of_shop > giftcode.GrandTotalFrom)
                {
                    db.cart_details.RemoveRange(db.cart_details.Where(c => c.Type == "Discount" && c.CustomerId == cusId));
                    var detail = new cart_detail();
                    detail.Quantity += qty ?? 1;
                    detail.Type = type;
                    detail.ItemId = ProductId;
                    detail.CustomerId = cusId;
                    detail.CreatedAt ??= DateTime.Now;
                    db.cart_details.AddOrUpdate(detail);
                    db.SaveChanges();
                    return detail;
                }
                else
                {
                    throw new Exception("Chỉ áp dụng cho đơn hàng từ " + giftcode.GrandTotalFrom?.ToString("#,##0 đ"));
                }
            }
        }

        public cart_detail SetQty(int Id, int qty = 1)
        {
            var lang = SiteLang.GetCurrentLang();
            var detail = db.cart_details.Find(Id);
            if (detail == null || detail.CustomerId != cusId)
            {
                return null;
            }

            var product = db.products.FirstOrDefault(p => p.ReId == detail.ItemId && p.LangCode == lang);
            if (product == null || !(product.Price > 0))
            {
                RemoveFromCart(detail.Id);
                throw new Exception("Sản phẩm này đã ngừng bán!");
            }

            // Nếu số lượng không lớn hơn không
            if (!(qty > 0))
            {
                // xóa detail nếu có.
                if (detail.CreatedAt != null)
                {
                    RemoveFromCart(detail.Id);
                }

                return null;
            }
            var opt = db.product_option_values.FirstOrDefault(x => x.productId == product.ReId && x.option1 == detail.PriceOpt1 && x.option2 == detail.PriceOpt2);
            if (opt != null && opt.qty < qty)
            {
                detail.Quantity = opt.qty;
                db.Entry(detail).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                throw new Exception("số lượng sản phẩm này chỉ còn "+ opt.qty);
            }

            detail.Quantity = qty;
            db.Entry(detail).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return detail;
        }

        internal void RemoveFromCart(int? Id)
        {
            db.cart_details.RemoveRange(db.cart_details.Where(c => (Id == null || c.Id == Id) && c.CustomerId == cusId));
            db.SaveChanges();
        }

        internal void RemoveAllFromCart()
        {
            db.cart_details.RemoveRange(db.cart_details.Where(c => c.CustomerId == cusId));
            db.SaveChanges();
        }

        internal List<cart_detail> GetCart(bool include_Discount = false)
        {
            var cur_cus = UserContent.getCurrentCustomer();
            var lang = SiteLang.GetCurrentLang();
            var outdate = new List<cart_detail>();
            var rs = db.cart_details.Where(c => c.CustomerId == cusId && c.Type == "Product")
                .Join(db.products.Where(p => p.LangCode == lang), c => c.ItemId, p => p.ReId, (c, p)
                => new { c, p, opt = db.product_option_values.FirstOrDefault(v => v.option1 == c.PriceOpt1 && v.option2 == c.PriceOpt2), }).AsEnumerable()
                .Select(x =>
                {
                    if (!(x.c.Quantity > 0))
                    {
                        outdate.Add(x.c);
                        return null;
                    }
                    var rs = x.c;
                    rs.StockQuantity = x.opt?.qty;
                    rs.ItemName = x.p.ProductName;
                    rs.ProductPicture = x.p.Picture;
                    rs.ProductCode = x.p.Url;
                    if (x.c.PriceOpt1 != null)
                    {
                        if (x.opt == null)
                        {
                            outdate.Add(x.c);
                            return null;
                        }

                        rs.Price = cur_cus?.Role == "Partner" ? (double)(x.opt.wholeSalePrice ?? x.opt.price) : (double)x.opt.price;
                        rs.PriceOpt1_name = x.opt.option1_name;
                        rs.PriceOpt2_name = x.opt.option2_name;
                    }
                    else
                    {
                        rs.Price = (x.p.SalePrice > 0 ? x.p.SalePrice : x.p.Price) ?? 0;
                    }

                    return rs;
                }).Where(x => x != null).ToList();
            if (include_Discount)
            {
                var curdate = DateTime.Now.Date;
                var discount = db.cart_details.Where(c => c.CustomerId == cusId && c.Type == "Discount")
                    .Join(db.gift_code, c => c.ItemId, g => g.code, (c, g) => new { c, g }).AsEnumerable()
                    .Select(x =>
                    {
                        x.c.ItemName = x.g.name;
                        x.c.ProductCode = x.g.code;
                        if (x.g.start_date <= curdate && x.g.end_date >= curdate)
                        {
                            var total_of_shop = rs?.Sum(c => c.Price * c.Quantity);
                            if ((decimal?)total_of_shop > x.g.GrandTotalFrom)
                            {
                                if (x.g.percent > 0)
                                {
                                    x.c.Price = (total_of_shop * x.g.percent / 100) ?? 0;
                                }

                                if (x.c.Price == 0 || x.c.Price > (double?)x.g.price) //nếu không có giảm giá theo % hoặc giảm nhiều hơn số tối đa.
                                {
                                    x.c.Price = (double?)x.g.price ?? 0;
                                }

                                return x.c;
                            }
                        }

                        outdate.Add(x.c);
                        return null;
                    }).Where(x => x != null).ToList();
                rs.AddRange(discount);
            }
            if (outdate.Count > 0)
            {
                db.cart_details.RemoveRange(outdate);
                db.SaveChanges();
            }
            return rs;
        }
        internal void SwapCustomer(string newId, string oldId = null)
        {
            oldId ??= cusId;
            var oldcart = db.cart_details.Where(c => c.CustomerId == oldId).ToList();
            if (oldcart.Count > 0)
            {
                db.cart_details.RemoveRange(db.cart_details.Where(d => d.CustomerId == newId));
                oldcart.ForEach(c =>
                {
                    c.CustomerId = newId;
                    db.Entry(c).State = System.Data.Entity.EntityState.Modified;
                });
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Get List config phí ship theo shop
        /// </summary>
        /// <param name="districtId">Id huyện</param>
        /// <returns></returns>
        internal List<shipping_config> GetShippingFee(string districtId)
        {
            var distric = db.vn_district.Find(districtId);
            var lang = SiteLang.GetCurrentLang();
            var cartInfo = GetCart().ToList();

            var listConfig = cartInfo.Select(x => x.ShopId).Distinct().Select(shopId =>
            {
                var cfs = db.shipping_config.Where(s => s.Active);

                //check freeship
                var freeship_amount = cfs.FirstOrDefault(s => s.Type == "free" && s.ShippingFee > 0);
                if ((decimal)cartInfo.Where(c => c.ShopId == shopId).Sum(c => c.Quantity * c.Price) > freeship_amount?.ShippingFee)
                {
                    //Không tính phí ship của shop này
                    return null;
                }
                else
                {
                    //check phí ship huyện nội thành
                    shipping_config ship_cfg = cfs.Where(s => s.ProvineId == distric.provinceid && s.DictrictData.Contains("\"" + distric.districtid + "\"")).FirstOrDefault()
                    // check phí ship default tỉnh
                    ?? cfs.Where(s => s.ProvineId == distric.provinceid && string.IsNullOrEmpty(s.DictrictData)).FirstOrDefault()
                    // Check phí ship default shop
                    ?? cfs.FirstOrDefault(s => string.IsNullOrEmpty(s.ProvineId) && s.Type == "default");
                    return ship_cfg;
                }
            }).Where(c => c != null).ToList();

            return listConfig;
        }
    }
}