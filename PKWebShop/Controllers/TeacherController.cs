using PKWebShop.AppLB;
using PKWebShop.Models;
using PKWebShop.Models.CustomizeModels;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PKWebShop.Controllers
{
    public class TeacherController : Controller
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
                    TempData.Remove("tinhthanh"); TempData.Remove("quanhuyen"); TempData.Remove("lophoc");
                    TempData.Remove("monhoc"); TempData.Remove("trinhdo"); TempData.Remove("gioitinh");
                }

                if (Request["tinhthanh"] != null)
                {
                    TempData["tinhthanh"] = Request["tinhthanh"];
                }

                if (Request["quanhuyen"] != null)
                {
                    TempData["quanhuyen"] = Request["quanhuyen"];
                }

                if (Request["lophoc"] != null)
                {
                    TempData["lophoc"] = Request["lophoc"];
                }

                if (Request["monhoc"] != null)
                {
                    TempData["monhoc"] = Request["monhoc"];
                }

                if (Request["trinhdo"] != null)
                {
                    TempData["trinhdo"] = Request["trinhdo"];
                }

                if (Request["gioitinh"] != null)
                {
                    TempData["gioitinh"] = Request["gioitinh"];
                }

                string tinhthanh = TempData["tinhthanh"] == null ? string.Empty : TempData["tinhthanh"].ToString();
                string quanhuyen = TempData["quanhuyen"] == null ? string.Empty : TempData["quanhuyen"].ToString();
                string lophoc = TempData["lophoc"] == null ? string.Empty : TempData["lophoc"].ToString();
                string monhoc = TempData["monhoc"] == null ? string.Empty : TempData["monhoc"].ToString().ToLower();
                string trinhdo = TempData["trinhdo"] == null ? string.Empty : TempData["trinhdo"].ToString();
                string gioitinh = TempData["gioitinh"] == null ? string.Empty : TempData["gioitinh"].ToString();

                TempData.Keep("tinhthanh"); TempData.Keep("quanhuyen"); TempData.Keep("lophoc");
                TempData.Keep("monhoc"); TempData.Keep("trinhdo"); TempData.Keep("gioitinh");
                #endregion

                var listTeacher = (from t in db.giasu_teacher.AsEnumerable()
                                   where t.IsDelete != true && t.Active != false
                                   && (string.IsNullOrEmpty(tinhthanh) || (!string.IsNullOrEmpty(tinhthanh) && t.ProvinceOrCity == tinhthanh))
                                   && (string.IsNullOrEmpty(quanhuyen) || (!string.IsNullOrEmpty(quanhuyen) && t.District.Contains(quanhuyen)))
                                   && (string.IsNullOrEmpty(trinhdo) || (!string.IsNullOrEmpty(trinhdo) && t.Degree == trinhdo))
                                   && (string.IsNullOrEmpty(gioitinh) || (!string.IsNullOrEmpty(gioitinh) && t.Gender == gioitinh))
                                   && (string.IsNullOrEmpty(monhoc) || (!string.IsNullOrEmpty(monhoc) && t.OptionId.Contains(monhoc)))
                                   && (string.IsNullOrEmpty(lophoc) || (!string.IsNullOrEmpty(lophoc) && t.OptionId.Contains(lophoc)))
                                   orderby t.CreateAt descending
                                   select new teacherModel
                                   {
                                       Id = t.Id,
                                       Code = t.Code,
                                       Avatar = string.IsNullOrEmpty(t.Avatar) ? "/Content/admin/img/no_image.jpg" : t.Avatar,
                                       FullName = t.FullName,
                                       Degree = t.Degree?.Split('|')[1],
                                       Birthday = t.Birthday == null ? "" : t.Birthday.Value.ToString("dd/MM/yyyy"),
                                       School = t.School,
                                       SchoolYearbook = t.SchoolYearbook,
                                       Majors = t.Majors,
                                       KVD = t.District.TrimEnd('|'),
                                       OptionId = t.OptionId.TrimEnd('|'),
                                       Note = t.Note,
                                       LD = string.Empty,
                                       MD = string.Empty,
                                   }).ToList();

                if (listTeacher != null && listTeacher.Count() > 0)
                {
                    var listOption = db.giasu_option.OrderBy(x => x.Order).ToList();
                    var type = string.Empty;
                    var optionId = string.Empty;
                    var optionName = string.Empty;

                    foreach (var item in listTeacher)
                    {
                        // option: "[Id]Name"
                        #region Khu vực dạy
                        var kvd = string.Empty;
                        var option = item.KVD.Split('|');
                        for (int i = 0; i < option.Length; i++)
                        {
                            kvd += option[i]?.Split(']')[1] + ",";
                        }

                        item.KVD = kvd.TrimEnd(',').Replace(",", ", ");
                        #endregion

                        #region Lớp dạy, Môn dạy
                        var ld = string.Empty;
                        var md = string.Empty;
                        option = item.OptionId.Split('|');

                        for (int j = 0; j < option.Length; j++)
                        {
                            optionId = option[j]?.Split(']')[0].Replace("[", string.Empty).Replace("]", string.Empty);
                            optionName = option[j]?.Split(']')[1];

                            type = listOption.Where(x => x.Id == optionId).FirstOrDefault()?.TypeName;

                            if (type.Equals("lớp dạy", StringComparison.OrdinalIgnoreCase))
                            {
                                ld += optionName + ",";
                            }
                            else if (type.Equals("môn dạy", StringComparison.OrdinalIgnoreCase))
                            {
                                md += optionName + ",";
                            }
                        }

                        item.LD = ld.TrimEnd(',').Replace(",", ", ");
                        item.MD = md.TrimEnd(',').Replace(",", ", ");
                        #endregion
                    }
                }

                // paged
                int _page, _rpp, take, skip;
                rpp = 6;
                int totalRecords = listTeacher.Count();
                CommonFunc.PagedList(page, rpp, totalRecords, out _page, out _rpp, out skip, out take);
                TempData["totalRecords"] = totalRecords;
                TempData["rpp"] = _rpp;
                TempData["page"] = _page;

                return View(listTeacher.Skip(skip).Take(take));
            }
            catch (Exception ex)
            {
                return Redirect("/trang-chu");
            }
        }

        public ActionResult RegisterTeacher()
        {
            try
            {
                ViewBag.listOption = db.giasu_option.ToList();
                return View(new giasu_teacher());
            }
            catch (Exception ex)
            {
                return Redirect("/trang-chu");
            }
        }
    }
}