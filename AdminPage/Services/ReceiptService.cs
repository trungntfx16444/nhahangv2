using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web.Mvc;
using Inner.Libs.Helpful;
using AdminPage.Models.CustomizeModels;
using AdminPage.Enums;
using AdminPage.Models;
using AdminPage.Services;
using AdminPage.Utils;

namespace AdminPage.Services
{
    /// <summary>
    /// Phiếu thu
    /// </summary>
    public class ReceiptService : ServicesBase
    {
        public ReceiptService() : base()
        {
        }
        public ReceiptService(AdminEntities db) : base(db)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public receipt Save(receipt item)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                item.Id = AppFunc.NewShortId();
                item.CreatedAt = DateTime.Now;
                item.CreatedBy = curMem.Fullname;
                DB.receipts.Add(item);
                DB.SaveChanges();
            }
            var old = DB.receipts.AsNoTracking().FirstOrDefault(i => i.Id == item.Id);
            if (old == null) { throw new Exception("Không tìm thấy phiếu nhập"); }
            item.CreatedAt = old.CreatedAt;
            item.CreatedBy = old.CreatedBy;
            item.UpdatedAt = DateTime.Now;
            item.UpdatedBy = curMem.Fullname;
            DB.Entry(item).State = EntityState.Modified;
            DB.SaveChanges();
            return item;
        }

        public receipt NewFromOrder(string orderId, string paymentMethod = "cod")
        {
            order _order = DB.orders.Find(orderId)!;
            receipt receipt = new receipt();
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
        /// <param name="data"></param>
        /// <param name="cond"></param>
        /// <param name="dt"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public List<receipt> Get(DataTable_request data, string cond, out int[] dt, DateTime? from = null, DateTime? to = null)
        {
            var receiptQuery = DB.receipts.AsQueryable();
            var recordsTotal = receiptQuery.Count();
            if (string.IsNullOrEmpty(cond) == false)
            {
                var cus = DB.customers.Where(c => c.FullName.Contains(cond)).Select(c => c.Id).ToList();
                if (cus.Count > 0)
                {
                    receiptQuery = receiptQuery.Where(r => cus.Contains(r.CustomerId));
                }
                else
                {
                    receiptQuery = receiptQuery.Where(r => r.OrderId == cond);
                }
            }
            if (from != null)
            {
                receiptQuery = receiptQuery.Where(r =>
                    r.CreatedAt >= from || r.ReceiptsAt >= from
                );
            }
            if (to != null)
            {
                receiptQuery = receiptQuery.Where(r =>
                    r.CreatedAt <= to || r.ReceiptsAt <= to
                );
            }

            var filtered_count = receiptQuery.Count();
            string[] orderColumns = { null, "Vendor_Name", "Total", "CreatedAt", "Status", null };
            var orderColumn = orderColumns[data.order?.FirstOrDefault()?.column ?? 1] ?? "CreatedAt";
            receiptQuery = receiptQuery.OrderBy($"{orderColumn} {data.order?.FirstOrDefault().dir}").Skip(data.start).Take(data.length);
            dt = new int[] { data.draw, filtered_count, recordsTotal };
            return receiptQuery.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cond"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public Dictionary<string, decimal> ReportInfo(List<receipt> receipts, string cond, DateTime? from = null, DateTime? to = null)
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