namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Web;
    using System.Web.Mvc;
    using PKWebShop.AppLB;
    using PKWebShop.Areas.Admin.CustomizeModel;
    using PKWebShop.Models;

    public class MemberLogController : ExpiredCheckController
    {
        public ActionResult Index()
        {
            if (!User.IsInRole("Admin"))
            {
                return Redirect("/home");
            }
            var db = new WebShopEntities();
            return View(
                db.users.AsEnumerable<user>().Select(m => new user { UserId = m.UserId, UserName = m.UserName }).OrderBy(m => m.UserName).ToList());
        }

        public JsonResult Load_table(DataTable_request data, string member)
        {
            var db = new WebShopEntities();
            var items = db.user_actions_log.AsQueryable();
            if (!string.IsNullOrEmpty(member))
            {
                items = items.Where(i => i.UserId == member);
            }
            var recordsTotal = db.user_actions_log.Count();
            var filtered_count = items.Count();
            string[] orderColumns = { "Time", "UserName", "Action", "EntityType", null };
            var orderColumn = orderColumns[data.order?.FirstOrDefault()?.column ?? 1] ?? "Time";
            var list_ticket = items.OrderBy(orderColumn + " " + data.order?.FirstOrDefault().dir).Skip(data.start).Take(data.length).ToList();
            var html = CommonFunc.RenderRazorViewToString("_tableData", list_ticket, this);
            return Json(new { draw = data.draw, recordsFiltered = filtered_count, recordsTotal = recordsTotal, data = html });
        }
    }
}