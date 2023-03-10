using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web.Mvc;
using Inner.Libs.Helpful;
using PKWebShop.Models.CustomizeModels;
using PKWebShop.Enums;
using PKWebShop.Models;
using PKWebShop.Services;
using PKWebShop.Utils;
using PKWebShop.AppLB;

namespace PKWebShop.Services
{
    /// <summary>
    /// Phiếu thu
    /// </summary>
    public class ReceiptService : ServicesBase
    {
        public ReceiptService() : base()
        {
        }
        public ReceiptService(WebShopEntities db) : base(db)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public receipts Save(receipts item)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                item.Id = AppFunc.NewShortId();
                item.CreatedAt = DateTime.Now;
                item.CreatedBy = "Customer Payment";
                DB.receipts.Add(item);
                DB.SaveChanges();
            }
            var old = DB.receipts.AsNoTracking().FirstOrDefault(i => i.Id == item.Id);
            if (old == null) { throw new Exception("Không tìm thấy phiếu nhập"); }
            item.CreatedAt = old.CreatedAt;
            item.CreatedBy = old.CreatedBy;
            item.UpdatedAt = DateTime.Now;
            item.UpdatedBy = "Customer Payment";
            DB.Entry(item).State = EntityState.Modified;
            DB.SaveChanges();
            return item;
        }

        public receipts NewFromOrder(string orderId, string paymentMethod = "cod")
        {
            order _order = DB.orders.Find(orderId)!;
            receipts receipt = new receipts();
            receipt.CustomerId = _order.CustomerId;
            receipt.OrderId = orderId;
            receipt.PaymentStatus = PaymentStatus.FullyPaid.Code<string>();
            receipt.PaymentMethod = paymentMethod;
            receipt.PaymentAmount = (decimal)_order.GrandTotal;
            receipt.ReceiptsAt = DateTime.Now;
            return Save(receipt);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="cond"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public Dictionary<string, decimal> ReportInfo(List<receipts> receipts, string cond, DateTime? from = null, DateTime? to = null)
        {
            List<string> ordersId = receipts.Select(r => r.OrderId).ToList();
            Dictionary<string, decimal> rs = new Dictionary<string, decimal>();
            var receipt_total = (decimal)(DB.orders.Where(or => ordersId.Contains(or.Id)).Sum(or => or.GrandTotal) ?? 0);
            var receipted = receipts.Sum(or => or.PaymentAmount);
            rs["receipt_total"] = receipt_total;
            rs["receipted"] = receipted;
            rs["receipt_remaining"] = receipt_total - receipted;
            return rs;
        }

        public List<customer> Customer()
        {
            return DB.customers.ToList();
        }

        public List<order> Orders()
        {
            return DB.orders.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Delete(string id)
        {
            try
            {
                DB.receipts.Remove(DB.receipts.Find(id));
                DB.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}