namespace PKWebShop.DataAsset
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using Newtonsoft.Json;
    using PKWebShop.Models;
    using PKWebShop.Services;
    using PKWebShop.Utils;

    public class DA_Order
    {
        // Khoi tao Constructors
        private WebShopEntities db;

        public DA_Order()
        {
            db = new WebShopEntities();
        }

        public DA_Order(WebShopEntities dataModel)
        {
            db = dataModel;
        }

        #region Function
        internal order CreateOrder(string Cus_id, order order, List<cart_detail> listItem, DateTime? bookingDate, string giftcode, int point_rate)
        {
            var webLicense = new PackageServices().WebPackInfo();
            var cus = db.customers.Find(Cus_id);
            var gC = new gift_code();
            if (webLicense.GiftCode == true)
            {
                gC = db.gift_code.FirstOrDefault(g => g.code == giftcode && (g.end_date ?? DateTime.MaxValue) >= DateTime.Now && (g.start_date ?? DateTime.MinValue) <= DateTime.Now && g.active == true);
                if (db.orders.Any(o => o.CustomerId == cus.Id && o.GiftCode == giftcode))
                {
                    gC = null;
                }
            }

            var newOrder = new order
            {
                Id = AppFunc.NewShortId(),
                CustomerId = cus?.Id,
                CustomerName = cus?.FullName,
                CustomerPhone = cus?.Phone,
                CustomerEmail = cus?.Email,
                Customer_Address = cus?.Address,
                Type = "Đặt hàng",
                Status = "1",
                AppointmentDate = bookingDate,
                SubTotal = 0,
                GiftCode = gC?.code,
                DiscountAmount = (double)(gC?.price ?? 0),
                DiscountPercent = (double)(gC?.percent ?? 0),
                GrandTotal = 0,
                GuestsPay = 0,
                ExcessCash = 0,
                CreatedAt = DateTime.Now,
                //CreateBy = cus?.FullName,
                CreateBy = "SYSTEM",
                ShippingAddress = order.ShippingAddress,
                Shipping_Name = order.Shipping_Name,
                Shipping_Phone = order.Shipping_Phone,
                Shipping_District = order.Shipping_District,
                Shipping_Province = order.Shipping_Province,
                Shipping_Ward = order.Shipping_Ward,
                Shipping = 0,
                Customer_Comment = order.Customer_Comment,
                PaymentMethod = order.PaymentMethod,
            };

            var langCode_default = SiteLang.GetDefault().Code;
            var listOrderDetail = new List<orders_detail>();
            foreach (var item in listItem.Where(x => x.Type == "Product"))
            {
                var product_langDefault = db.products.FirstOrDefault(x => x.ReId == item.ItemId && x.LangCode == langCode_default);
                var productName = product_langDefault?.ProductName + (!string.IsNullOrEmpty(item.PriceOpt1) ? " (" + item.PriceOpt1_name + ")" : "") + (!string.IsNullOrEmpty(item.PriceOpt2) ? " (" + item.PriceOpt2_name + ")" : "");
                var newOrderDetail = new orders_detail()
                {
                    Id = AppFunc.NewShortId(),
                    OrderId = newOrder.Id,
                    ProductId = item.ItemId,
                    ProductCode = product_langDefault?.Url,
                    ProductName = productName,
                    ParentPropertiesId = product_langDefault?.list_parent_properties,
                    PropertiesId = string.IsNullOrEmpty(item.Properties) ? string.Empty : item.Properties.Replace("|", ","),
                    PriceOptId1 = item.PriceOpt1,
                    PriceOptId2 = item.PriceOpt2,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    CreateAt = DateTime.Now,
                };
                db.orders_detail.Add(newOrderDetail);
                listOrderDetail.Add(newOrderDetail);

                db.product_option_values.Where(x => x.option1 == item.PriceOpt1 && x.option2 == item.PriceOpt2).ToList().ForEach(x =>
                {
                    x.sold += item.Quantity;
                    if (x.qty != -1)
                    {
                        if (x.qty <= 0)
                        {
                            throw new Exception($"{productName} đã hết hàng.", new Exception(item.Id + "|" + x.qty));
                        }
                        if (x.qty < item.Quantity)
                        {
                            throw new Exception($"{productName} số lượng kho chỉ còn " + x.qty, new Exception(item.Id + "|" + x.qty));
                        }
                        x.qty -= item.Quantity;
                    }

                    db.Entry(x).State = System.Data.Entity.EntityState.Modified;
                });
                product_langDefault.Sold = (product_langDefault.Sold ?? 0) + item.Quantity;
            }

            newOrder.SubTotal = listOrderDetail.Sum(o => o.Price * o.Quantity);
            newOrder.DiscountAmount = listItem.FirstOrDefault(x => x.Type == "Discount")?.Price ?? 0;
            var grandTotal = newOrder.SubTotal - newOrder.DiscountAmount;

            var shippingconfigs = db.shipping_config.Where(s => s.Active);
            var free_ship = (double?)shippingconfigs.FirstOrDefault(s => s.Type == "free" && s.ShippingFee > 0)?.ShippingFee ?? double.MaxValue;
            if (webLicense.ShippingFee == true && grandTotal < free_ship && !string.IsNullOrEmpty(order.Shipping_District))
            {
                var selected_district = db.vn_district.Find(order.Shipping_District);
                shipping_config ship_cfg = shippingconfigs.Where(s => s.ProvineId == selected_district.provinceid && s.DictrictData.Contains("\"" + selected_district.districtid + "\"")).FirstOrDefault();
                if (ship_cfg == null)
                {
                    ship_cfg = shippingconfigs.Where(s => s.ProvineId == selected_district.provinceid && s.Active == true && string.IsNullOrEmpty(s.DictrictData)).FirstOrDefault();
                }

                if (ship_cfg == null)
                {
                    ship_cfg = shippingconfigs.Where(s => string.IsNullOrEmpty(s.ProvineId) && s.Type != "free").FirstOrDefault();
                }

                if (ship_cfg != null)
                {
                    newOrder.Shipping = (double)ship_cfg.ShippingFee;
                }
            }

            newOrder.GrandTotal = grandTotal + (grandTotal >= free_ship ? 0 : (newOrder.Shipping ?? 0));

            //if (webLicense.MembershipPoints == true)
            //{
            //    var bonusPoint = db.BonusPointConfig.FirstOrDefault();
            //    if (bonusPoint != null && cus.BonusPoints > 0 && point_rate > 0)
            //    {
            //        var money_rate = cus.BonusPoints * bonusPoint.Value;
            //        if (money_rate >= newOrder.GrandTotal)
            //        {
            //            cus.points_used = (cus.points_used ?? 0) + (int)(newOrder.GrandTotal / bonusPoint.Value);
            //            cus.BonusPoints -= cus.points_used ?? 0;

            //            newOrder.BonusPointsDiscount = (decimal)newOrder.GrandTotal;
            //            newOrder.BonusPointsUsed = (int)(newOrder.GrandTotal / bonusPoint.Value);
            //            newOrder.GrandTotal = 0;
            //        }
            //        else
            //        {
            //            newOrder.BonusPointsDiscount = (decimal?)money_rate;
            //            newOrder.BonusPointsUsed = cus.BonusPoints;
            //            newOrder.GrandTotal -= money_rate;

            //            cus.points_used += cus.BonusPoints;
            //            cus.BonusPoints = 0;
            //        }

            //        db.Entry(cus).State = System.Data.Entity.EntityState.Modified;
            //    }

            //    newOrder.BonusPointsGet = BonusPonts_calculation(newOrder, out List<string> BonusIds, listOrderDetail);
            //    newOrder.BonusList = JsonConvert.SerializeObject(BonusIds);
            //}

            if (newOrder.GrandTotal <= 0 && point_rate == 0)
            {
                throw new Exception("Không thể tiến hành đặt đơn hàng này!");
            }

            db.orders.Add(newOrder);
            db.SaveChanges();
            newOrder.Details = listOrderDetail;
            return newOrder;
        }
        public int BonusPonts_calculation(order order, out List<string> BonusIds, List<orders_detail> orders_Details = null)
        {
            var webLicense = new PackageServices().WebPackInfo();
            if (order.CreatedAt == null || !webLicense.MembershipPoints)
            {
                BonusIds = new List<string>();
                return 0;
            }

            if (orders_Details == null)
            {
                orders_Details = db.orders_detail.Where(d => d.OrderId == order.Id).ToList();
            }

            var creat = order.CreatedAt.Value.Date;
            var events = db.customer_score.Where(s =>
                s.Active == true
                && (s.FromDate == null || s.FromDate <= creat)
                && (s.ToDate == null || s.ToDate >= creat)
                && (s.MultipleOnCustomer || !db.orders.Any(o => o.CustomerId == order.CustomerId && o.Id != order.Id && o.BonusList.Contains(s.Id))))
            .ToList().Select(s =>
            {
                double value = 0;
                if (!string.IsNullOrEmpty(s.Products))
                {
                    var id_categ = s.Products.Split('|');
                    var id = db.products.Where(p => id_categ.Contains(p.CategoryId)).Select(p => p.ReId).ToList();
                    value = orders_Details.Where(d => id.Contains(d.ProductId)).Sum(d => (d.Price ?? 0) * (d.Quantity ?? 0)) - (order.DiscountAmount ?? 0);
                }
                else
                {
                    value = orders_Details.Sum(d => (d.Price ?? 0) * (d.Quantity ?? 0)) - (order.DiscountAmount ?? 0);
                }

                var multiple = (int)(value / s.Order_GranTotal);
                if (!s.MultipleOnOrder)
                {
                    multiple = Math.Min(multiple, 1);
                }

                return new { id = s.Id, points = s.Score ?? 0, multiple };
            }).Where(s => s.multiple > 0).ToList();
            BonusIds = events.Select(e => e.id).ToList();
            return events.Sum(s => s.points * s.multiple);
        }

        public decimal BonusPonts_calculation(string orderId, List<string> BonusIds = null)
        {
            var order = db.orders.Find(orderId);
            if (order == null)
            {
                return 0;
            }

            var orders_Details = db.orders_detail.Where(d => d.OrderId == orderId).ToList();
            return BonusPonts_calculation(order, out BonusIds, orders_Details);
        }


        #endregion
    }
}