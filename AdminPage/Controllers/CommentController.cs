using AdminPage.AppLB;
using AdminPage.Models;
using AdminPage.Models.CustomizeModels;
using AdminPage.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web;
using System.Web.Mvc;

namespace AdminPage.Controllers
{
    public class CommentController : Controller
    {
        AdminEntities _db = new AdminEntities();
        const string session_price = "session_product_price";
        private Dictionary<string, bool> access = Authority.GetAccessAuthority();
        private SiteLang.Language default_language = SiteLang.GetDefault();
        // GET: Comment
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult Load_table(DataTable_request data, string search, bool? isActive, string type = "products")
        {
            if (type == "news")
            {
                return Json(new { draw = data.draw, recordsFiltered = 0, recordsTotal = 0, data = "" });
            }
            else
            {
                var recordsTotal = _db.comments.Count();
                var orderQuery = _db.comments.Join(_db.products, c=>c.MappingId, p=>p.ReId, (c,p)=>new { c, p }).AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    search = "%" + search + "%";
                    orderQuery = orderQuery.Where(q =>
                    DbFunctions.Like(q.c.Content, search)
                    || DbFunctions.Like(q.c.Email, search)
                    || DbFunctions.Like(q.c.PeopleName, search)
                    || DbFunctions.Like(q.p.ProductName, search));
                }
                if (isActive != null)
                {
                    orderQuery = orderQuery.Where(q => q.c.IsActive == isActive);
                }
                var filtered_count = orderQuery.Count();
                string[] orderColumns = { "c.CreateAt", "p.ProductName", "c.PeopleName", "c.IsActive", null };
                var orderColumn = orderColumns[data.order?.FirstOrDefault()?.column ?? 1] ?? "c.CreateAt";

                var listOrder = orderQuery.OrderBy($"{orderColumn} {data.order?.FirstOrDefault().dir}").Skip(data.start).Take(data.length).ToArray().Select(x=>(x.c,x.p)).ToList();
                var html = CommonFunc.RenderRazorViewToString("_ProductsComment", listOrder, this);

                return Json(new { draw = data.draw, recordsFiltered = filtered_count, recordsTotal = recordsTotal, data = html });
            }
        }
        public JsonResult Active(string Id)
        {
            try
            {
                var comment = _db.comments.Find(Id);
                if (comment == null)
                {
                    throw new Exception("Không tìm thấy bình luận");
                }
                comment.IsActive = !(comment.IsActive ?? false);
                _db.Entry(comment).State = EntityState.Modified;
                _db.SaveChanges();
                return Json(new object[] { true, comment.IsActive == true ? "Đã duyệt bình luận" : "Đã đặt bình luận thành chưa duyệt" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
            
        }
        public JsonResult Delete(string Id)
        {
            try
            {
                var comment = _db.comments.Find(Id);
                if (comment == null)
                {
                    throw new Exception("Không tìm thấy bình luận");
                }
                _db.comments.Remove(comment);
                _db.SaveChanges();
                return Json(new object[] { true, "Đã xóa bình luận" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
            
        }

    }
}