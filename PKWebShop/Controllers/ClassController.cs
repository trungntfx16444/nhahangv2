using PKWebShop.AppLB;
using PKWebShop.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PKWebShop.Controllers
{
    public class ClassController : Controller
    {
        private WebShopEntities db = new WebShopEntities();

        // GET: Teacher
        public ActionResult Index(int? page, int? rpp)
        {
            try
            {
                #region Get Option Search
                ViewBag.ListOption = (from o in db.giasu_option
                                      where o.TypeName.Equals("lớp dạy", StringComparison.OrdinalIgnoreCase)
                                      || o.TypeName.Equals("môn dạy", StringComparison.OrdinalIgnoreCase)
                                      || o.TypeName.Equals("trình độ", StringComparison.OrdinalIgnoreCase)
                                      orderby o.Order
                                      select o).ToList();
                #endregion

                #region Request Search
                if (string.IsNullOrWhiteSpace(HttpContext.Request.Url.Query) == true)
                {
                    TempData.Remove("malop"); TempData.Remove("tinhthanh"); TempData.Remove("quanhuyen");
                    TempData.Remove("trinhdo"); TempData.Remove("monhoc"); TempData.Remove("lophoc");
                }

                if (Request["malop"] != null)
                {
                    TempData["malop"] = Request["malop"];
                }

                if (Request["tinhthanh"] != null)
                {
                    TempData["tinhthanh"] = Request["tinhthanh"];
                }

                if (Request["quanhuyen"] != null)
                {
                    TempData["quanhuyen"] = Request["quanhuyen"];
                }

                if (Request["trinhdo"] != null)
                {
                    TempData["trinhdo"] = Request["trinhdo"];
                }

                if (Request["monhoc"] != null)
                {
                    TempData["monhoc"] = Request["monhoc"];
                }

                if (Request["lophoc"] != null)
                {
                    TempData["lophoc"] = Request["lophoc"];
                }

                string malop = TempData["malop"] == null ? string.Empty : TempData["malop"].ToString();
                string tinhthanh = TempData["tinhthanh"] == null ? string.Empty : TempData["tinhthanh"].ToString();
                string quanhuyen = TempData["quanhuyen"] == null ? string.Empty : TempData["quanhuyen"].ToString();
                string trinhdo = TempData["trinhdo"] == null ? string.Empty : TempData["trinhdo"].ToString();
                string monhoc = TempData["monhoc"] == null ? string.Empty : TempData["monhoc"].ToString().ToLower();
                string lophoc = TempData["lophoc"] == null ? string.Empty : TempData["lophoc"].ToString();

                TempData.Keep("malop"); TempData.Keep("tinhthanh"); TempData.Keep("quanhuyen");
                TempData.Keep("trinhdo"); TempData.Keep("monhoc"); TempData.Keep("lophoc");
                #endregion

                var listClass = (from c in db.giasu_class.AsEnumerable()
                                 where c.IsDelete != true && c.IsComplete != true
                                 && (string.IsNullOrEmpty(malop) || (!string.IsNullOrEmpty(malop) && c.Code.Contains(malop)))
                                 && (string.IsNullOrEmpty(tinhthanh) || (!string.IsNullOrEmpty(tinhthanh) && c.ProvinceOrCity == tinhthanh))
                                 && (string.IsNullOrEmpty(quanhuyen) || (!string.IsNullOrEmpty(quanhuyen) && c.District == quanhuyen))
                                 && (string.IsNullOrEmpty(trinhdo) || (!string.IsNullOrEmpty(trinhdo) && c.DegreeRequired == trinhdo))
                                 && (string.IsNullOrEmpty(monhoc) || (!string.IsNullOrEmpty(monhoc) && c.Subjects.ToLower().Contains(monhoc)))
                                 && (string.IsNullOrEmpty(lophoc) || (!string.IsNullOrEmpty(lophoc) && c.ClassName == lophoc))
                                 orderby c.CreateAt descending
                                 select c).ToList();

                // paged
                int _page, _rpp, take, skip;
                rpp = 6;
                int totalRecords = listClass.Count();
                CommonFunc.PagedList(page, rpp, totalRecords, out _page, out _rpp, out skip, out take);
                TempData["totalRecords"] = totalRecords;
                TempData["rpp"] = _rpp;
                TempData["page"] = _page;

                return View(listClass.Skip(skip).Take(take));
            }
            catch (Exception ex)
            {
                return Redirect("/trang-chu");
            }
        }

        // Phụ huynh đăng ký tìm Gia sư
        public ActionResult RegisterClass()
        {
            try
            {
                ViewBag.listOption = (from opt in db.giasu_option
                                      where opt.TypeName.Equals("lớp dạy", StringComparison.OrdinalIgnoreCase) == true
                                      || opt.TypeName.Equals("trình độ", StringComparison.OrdinalIgnoreCase) == true
                                      select opt).ToList();

                return View(new giasu_class());
            }
            catch (Exception ex)
            {
                return Redirect("/trang-chu");
            }
        }
    }
}