namespace AdminPage.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Web;
    using Newtonsoft.Json;
    using AdminPage.AppLB;
    using AdminPage.Models;
    using AdminPage.Models.CustomizeModels;
    using AdminPage.Utils;

    public class ExpenseService : ServicesBase
    {
        public List<expens> LoadExpense(DataTable_request data, DateTime? From, DateTime To, string search, string s_vendor, string s_status, out int[] dt, out string err_msg)
        {
            var listExpense = new List<expens>();
            err_msg = string.Empty;
            try
            {
                var recordsTotal = DB.expenses.Count();
                IQueryable<expens> expenseQuery;

                if (!string.IsNullOrEmpty(search))
                {
                    var sqlCommand = CommonFunc.SearchCommand("expenses", search, "Id", "ImportTicket_Id");
                    expenseQuery = DB.expenses.SqlQuery(sqlCommand).Where(x => (string.IsNullOrEmpty(s_vendor) || (s_vendor == "-1" && string.IsNullOrEmpty(x.Vendor_Id)) || x.Vendor_Id == s_vendor) && (string.IsNullOrEmpty(s_status) || x.Status == s_status) && x.CreatedAt >= From && x.CreatedAt <= To).AsQueryable();
                }
                else
                {
                    expenseQuery = DB.expenses.Where(x => (string.IsNullOrEmpty(s_vendor) || (s_vendor == "-1" && string.IsNullOrEmpty(x.Vendor_Id)) || x.Vendor_Id == s_vendor) && (string.IsNullOrEmpty(s_status) || x.Status == s_status) && x.CreatedAt >= From && x.CreatedAt <= To);
                }

                var filtered_count = expenseQuery.Count();
                string[] orderColumns = { "Id", " Title", "Total", "CreatedAt", "Status", null };
                var orderColumn = orderColumns[data.order?.FirstOrDefault()?.column ?? 1] ?? "CreatedAt";
                listExpense = expenseQuery.OrderBy($"{orderColumn} {data.order?.FirstOrDefault().dir}").Skip(data.start).Take(data.length).ToList();
                dt = new int[] { data.draw, filtered_count, recordsTotal };
                return listExpense;
            }
            catch (Exception ex)
            {
                err_msg = ex.Message?.Replace("\r\n", string.Empty);
                dt = new int[] { 0, 0, 0 };
                return listExpense;
            }
        }

        public string SaveExpense(expens data, string totalStr, string expenseName, string expensePrice)
        {
            import_ticket importTicket = null;
            string importTicket_Id = string.Empty;
            if (!string.IsNullOrEmpty(data.Vendor_Id))
            {
                if (!string.IsNullOrEmpty(data.ImportTicket_Id))
                {
                    importTicket = DB.import_ticket.FirstOrDefault(x => x.Code == data.ImportTicket_Id && x.Vendor_Id == data.Vendor_Id);
                    if (importTicket == null)
                    {
                        throw new Exception("Mã phiếu nhập không đúng");
                    }
                    else if (data.Status == "1" && importTicket.PayStatus == "FullyPaid")
                    {
                        throw new Exception("Phiếu nhập này đã được thanh toán hoàn tất trước đó");
                    }
                    importTicket_Id = importTicket.Code;
                }
            }
            else if (string.IsNullOrEmpty(data.Vendor_Id) && !string.IsNullOrEmpty(data.ImportTicket_Id))
            {
                importTicket = DB.import_ticket.FirstOrDefault(x => x.Code == data.ImportTicket_Id);
                if (importTicket == null)
                {
                    throw new Exception("Mã phiếu nhập không đúng");
                }
                else if (data.Status == "1" && importTicket.PayStatus == "FullyPaid")
                {
                    throw new Exception("Phiếu nhập này đã được thanh toán hoàn tất trước đó");
                }
                importTicket_Id = importTicket.Code;
                data.Vendor_Id = importTicket.Vendor_Id;
            }

            var total = string.IsNullOrEmpty(totalStr) ? "0" : totalStr.Replace(".", string.Empty);
            if (total == "0")
            {
                throw new Exception("Vui lòng nhập tổng tiền thanh toán");
            }

            var listExpense = new List<ExpenseObjectModel>();
            string[] expName = (expenseName ?? string.Empty).Split(',');
            string[] expPrice = (expensePrice ?? string.Empty).Split(',');
            if (expName.Length < expPrice.Length)
            {
                throw new Exception("Vui lòng nhập tên các chi phí");
            }

            for (int i = 0; i < expName.Length; i++)
            {
                listExpense.Add(new ExpenseObjectModel()
                {
                    Name = expName[i],
                    Price = expPrice[i],
                });
            }

            string reasonJson = string.Empty;
            if (listExpense.Count > 0)
            {
                reasonJson = JsonConvert.SerializeObject(listExpense);
            }

            var vendor = DB.vendors.FirstOrDefault(x => x.Id == data.Vendor_Id);

            #region CREATE/UPDATE EXPENSE
            if (string.IsNullOrEmpty(data.Id))
            {
                data.Id = AppFunc.NewShortId();
                data.Vendor_Id = vendor?.Id ?? string.Empty;
                data.Vendor_Name = vendor?.Name ?? string.Empty;
                data.ImportTicket_Id = importTicket_Id;
                data.Total = decimal.Parse(total);
                data.Reason = reasonJson;
                data.CreatedBy = Authority.GetThisUser(false)?.Fullname;
                data.CreatedAt = DateTime.Now;
                DB.expenses.Add(data);
                DB.SaveChanges();
            }
            else
            {
                var exp = DB.expenses.FirstOrDefault(x => x.Id == data.Id);
                if (exp == null)
                {
                    throw new Exception("Chi phí không tồn tại.");
                }

                exp.Title = data.Title;
                exp.Vendor_Id = vendor?.Id ?? string.Empty;
                exp.Vendor_Name = vendor?.Name ?? string.Empty;
                exp.ImportTicket_Id = importTicket_Id;
                exp.Total = decimal.Parse(total);
                exp.PaymentMethod = data.PaymentMethod;
                exp.Reason = reasonJson;
                exp.Note = data.Note;
                exp.Status = data.Status;
                exp.PaymentDate = data.PaymentDate;
                exp.UpdatedBy = Authority.GetThisUser(false)?.Fullname;
                exp.UpdatedAt = DateTime.Now;
                DB.Entry(exp).State = System.Data.Entity.EntityState.Modified;
                DB.SaveChanges();
            }
            #endregion

            #region UPDATE IMPORT_TICKET
            if (importTicket != null && data.Status == "1")
            {
                if (DB.expenses.Where(x => x.ImportTicket_Id == importTicket.Code && x.Status == "1").Sum(x => x.Total) >= importTicket.Total)
                {
                    importTicket.PayStatus = "FullyPaid";
                }
                else
                {
                    importTicket.PayStatus = "PaidAPart";
                }
                DB.Entry(importTicket).State = System.Data.Entity.EntityState.Modified;
                DB.SaveChanges();
            }
            #endregion

            return string.Empty;
        }
    }
}