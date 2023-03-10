namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using PKWebShop.AppLB;
    using PKWebShop.Models;

    [Authorize]
    public class TeacherManController : UploadController
    {
        private WebShopEntities db = new ();

        // GET: Admin/TeacherMan
        public ActionResult Index()
        {
            return View(db.giasu_teacher.Where(x => x.IsDelete != true).OrderByDescending(x => x.CreateAt).ToList());
        }

        public ActionResult Save(string id)
        {
            try
            {
                // id: "teacherId" or "vteacherId"
                // vd: "2007161006308782" or "v2007161006308782"
                // nếu trước id có chữ "v" là [view detail teacher]
                // nếu trước id không có chữ "v" là [add new teacher] hoặc [update teacher]
                var key = !string.IsNullOrEmpty(id) ? id.Substring(0, 1) : string.Empty; // lấy chữ cái đầu tiên trong chuỗi id
                id = id?.Replace("v", string.Empty);

                var teacher = db.giasu_teacher.Find(id);
                var listOption = db.giasu_option.ToList();

                if (teacher == null)
                {
                    // add new
                    ViewBag.Title = "Thêm mới Gia sư";

                    var uploadFiles = new List<uploadmorefile>();
                    var arrData = new object[] { listOption, uploadFiles, key };
                    ViewBag.ArrData = arrData;

                    return View(new giasu_teacher());
                }
                else
                {
                    // edit
                    ViewBag.Title = "Cập nhật thông tin Gia sư";

                    if (key == "v")
                    {
                        ViewBag.Title = "Xem thông tin Gia sư";
                    }

                    var uploadFiles = db.uploadmorefiles.Where(f => f.TableId == id && f.TableName.Equals("giasu_teacher")).ToList();
                    var arrData = new object[] { listOption, uploadFiles, key };
                    ViewBag.ArrData = arrData;

                    return View(teacher);
                }
            }
            catch (Exception ex)
            {
                TempData["e"] = ex.Message;
                return RedirectToAction("index");
            }
        }

        [HttpPost]
        public async Task<JsonResult> SaveTeacher(giasu_teacher dataForm)
        {
            try
            {
                int filesCount = int.Parse(Request["filescount"]);
                var listOption = db.giasu_option.ToList();

                if (string.IsNullOrEmpty(dataForm.Id))
                {
                    #region Check Teacher Exit
                    var teacherExist = CheckTeacherExit(dataForm.Phone, out string errMsg);
                    if (!string.IsNullOrEmpty(errMsg))
                    {
                        throw new Exception(errMsg);
                    }
                    else
                    {
                        if (teacherExist != null && teacherExist.Length > 0)
                        {
                            return Json(new object[] { false, "-1", teacherExist });
                        }
                    }
                    #endregion

                    // add new teacher
                    Random rd = new ();
                    var teacher = new giasu_teacher();
                    teacher = dataForm;
                    teacher.Id = AppLB.CommonFunc.RandomNumber(DateTime.Now.ToString("yyMMddHHmmss"), rd);
                    teacher.Code = (db.giasu_teacher.Count() + 1).ToString().PadLeft(4, '0');
                    teacher.Avatar = Request.Unvalidated["textarea_Avatar"];
                    teacher.PictureCardId = Request.Unvalidated["textarea_PictureCardId1"] + "|" + Request.Unvalidated["textarea_PictureCardId2"];
                    teacher.Note = Request.Unvalidated["Note"];
                    teacher.Active = true;
                    teacher.CreateAt = DateTime.Now;
                    teacher.CreateBy = User.Identity.Name;

                    if (DateTime.TryParse(Request["Birthday"], out DateTime birthday))
                    {
                        dataForm.Birthday = birthday;
                    }

                    // Update District For Teacher
                    var result = UpdateDistrictForTeacher(teacher);
                    if (!string.IsNullOrEmpty(result))
                    {
                        throw new Exception(result);
                    }

                    // Update Option For Teacher
                    result = UpdateOptionForTeacher(listOption, teacher);
                    if (!string.IsNullOrEmpty(result))
                    {
                        throw new Exception(result);
                    }

                    db.giasu_teacher.Add(teacher);
                    UploadMoreFiles(teacher.Id, "giasu_teacher", filesCount, "/upload/images");

                    db.SaveChanges();
                    TempData["s"] = "Thêm mới Gia sư thành công";

                    return Json(new object[] { true, "Thêm mới Gia sư thành công", teacher.Id });
                }
                else
                {
                    // update teacher
                    var teacher = db.giasu_teacher.Find(dataForm.Id);
                    if (teacher == null)
                    {
                        throw new Exception("Không tìm thấy Gia sư");
                    }

                    // check sđt đã đăng ký hay chưa
                    var teacherExist = db.giasu_teacher.Where(x => x.Phone == dataForm.Phone && x.Id != teacher.Id && x.IsDelete != true).FirstOrDefault();
                    if (teacherExist != null)
                    {
                        string[] teacherInfo = new[]
                        {
                            teacherExist.Code,
                            teacherExist.Avatar ?? "~/Content/admin/img/no_image.jpg",
                            teacherExist.FullName,
                            teacherExist.Phone,
                            teacherExist.CreateAt.Value.ToString("dd/MM/yyyy"),
                        };
                        return Json(new object[] { false, "-1", teacherInfo });
                    }

                    teacher.FullName = dataForm.FullName;
                    teacher.Gender = dataForm.Gender;
                    teacher.ProvinceOrCity = dataForm.ProvinceOrCity;
                    teacher.CardId = dataForm.CardId;
                    teacher.Address = dataForm.Address;
                    teacher.Email = dataForm.Email;
                    teacher.Phone = dataForm.Phone;
                    teacher.Avatar = Request.Unvalidated["textarea_Avatar"];
                    teacher.PictureCardId = Request.Unvalidated["textarea_PictureCardId1"] + "|" + Request.Unvalidated["textarea_PictureCardId2"];
                    teacher.School = dataForm.School;
                    teacher.Majors = dataForm.Majors;
                    teacher.SchoolYearbook = dataForm.SchoolYearbook;
                    teacher.Degree = dataForm.Degree;
                    teacher.Note = Request.Unvalidated["Note"];
                    teacher.UpdateAt = DateTime.Now;
                    teacher.UpdateBy = User.Identity.Name;
                    teacher.District = string.Empty;
                    teacher.OptionId = string.Empty;

                    if (Request["Active"] == "1")
                    {
                        teacher.Active = true;
                    }
                    else
                    {
                        teacher.Active = false;
                    }

                    if (DateTime.TryParse(Request["Birthday"], out DateTime birthday))
                    {
                        teacher.Birthday = birthday;
                    }

                    // Update District For Teacher
                    var result = UpdateDistrictForTeacher(teacher);
                    if (!string.IsNullOrEmpty(result))
                    {
                        throw new Exception(result);
                    }

                    // Update Option For Teacher
                    result = UpdateOptionForTeacher(listOption, teacher);
                    if (!string.IsNullOrEmpty(result))
                    {
                        throw new Exception(result);
                    }

                    db.Entry(teacher).State = System.Data.Entity.EntityState.Modified;
                    UploadMoreFiles(dataForm.Id, "giasu_teacher", filesCount, "/upload/images");

                    db.SaveChanges();
                    TempData["s"] = "Cập nhật thông tin Gia sư thành công";

                    return Json(new object[] { true, "Cập nhật thông tin Gia sư thành công", teacher.Id });
                }
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, "Lưu thất bại. " + ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult ClientRegisterTeacher(giasu_teacher dataForm)
        {
            try
            {
                #region Check Teacher Exit
                var teacherExist = CheckTeacherExit(dataForm.Phone, out string errMsg);
                if (!string.IsNullOrEmpty(errMsg))
                {
                    throw new Exception(errMsg);
                }
                else
                {
                    if (teacherExist != null && teacherExist.Length > 0)
                    {
                        return Json(new object[] { false, "-1", teacherExist });
                    }
                }
                #endregion

                int filesCount = int.Parse(Request["filescount"]);
                var listOption = db.giasu_option.ToList();

                // client register teacher
                Random rd = new ();
                var teacher = new giasu_teacher();
                teacher = dataForm;
                teacher.Id = AppLB.CommonFunc.RandomNumber(DateTime.Now.ToString("yyMMddHHmmss"), rd);
                teacher.Code = (db.giasu_teacher.Count() + 1).ToString().PadLeft(4, '0');
                teacher.Avatar = Request.Unvalidated["textarea_Avatar"];
                teacher.PictureCardId = Request.Unvalidated["textarea_PictureCardId1"] + "|" + Request.Unvalidated["textarea_PictureCardId2"];
                teacher.Note = Request.Unvalidated["Note"];
                teacher.Active = true;
                teacher.CreateAt = DateTime.Now;
                teacher.CreateBy = string.Empty;

                if (DateTime.TryParse(Request["Birthday"], out DateTime birthday))
                {
                    dataForm.Birthday = birthday;
                }

                // Update District For Teacher
                var result = UpdateDistrictForTeacher(teacher);
                if (!string.IsNullOrEmpty(result))
                {
                    throw new Exception(result);
                }

                // Update Option For Teacher
                result = UpdateOptionForTeacher(listOption, teacher);
                if (!string.IsNullOrEmpty(result))
                {
                    throw new Exception(result);
                }

                db.giasu_teacher.Add(teacher);
                UploadMoreFiles(teacher.Id, "giasu_teacher", filesCount, "/upload/images");

                // gui email thong bao admin
                result = SendEmailAdmin(teacher);
                if (!string.IsNullOrEmpty(result))
                {
                    throw new Exception(result);
                }

                db.SaveChanges();
                return Json(new object[] { true, "Đăng ký làm Gia sư thành công" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        [AllowAnonymous]
        public string[] CheckTeacherExit(string phoneNumber, out string errMsg)
        {
            errMsg = string.Empty;
            try
            {
                var teacherExist = (from t in db.giasu_teacher.AsEnumerable()
                                    where t.Phone == phoneNumber
                                    && t.IsDelete != true
                                    select new[]
                                    {
                                        t.Code,
                                        t.Avatar ?? "~/Content/admin/img/no_image.jpg",
                                        t.FullName,
                                        t.Phone,
                                        t.CreateAt.Value.ToString("dd/MM/yyyy"),
                                    }).FirstOrDefault();

                return teacherExist;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return null;
            }
        }

        [AllowAnonymous]
        public string UpdateDistrictForTeacher(giasu_teacher teacher)
        {
            try
            {
                var khuVucDay = false;
                var provinceId = teacher.ProvinceOrCity?.Split('|')[0];
                var listdistrict = db.vn_district.Where(x => x.provinceid == provinceId).ToList();

                // khu vực dạy
                foreach (var item in listdistrict)
                {
                    if (Request["kvd_" + item.districtid] == "1")
                    {
                        teacher.District += "[" + item.districtid + "]" + item.type + " " + item.name + "|";
                        khuVucDay = true;
                    }
                }

                if (khuVucDay == false)
                {
                    throw new Exception("Vui lòng chọn khu vực dạy");
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        [AllowAnonymous]
        public string UpdateOptionForTeacher(List<giasu_option> listOption, giasu_teacher teacher)
        {
            try
            {
                var monDay = false;
                var lopDay = false;
                var thoiGianDay = false;

                // môn dạy
                #region Mon Day
                foreach (var item in listOption.Where(x => x.TypeName.Equals("môn dạy", StringComparison.OrdinalIgnoreCase)))
                {
                    if (Request["md_" + item.Id] == "1")
                    {
                        teacher.OptionId += "[" + item.Id + "]" + item.Name + "|";
                        monDay = true;
                    }
                }

                if (monDay == false)
                {
                    throw new Exception("Vui lòng chọn môn dạy");
                }
                #endregion

                // lớp dạy
                #region Lop Day
                foreach (var item in listOption.Where(x => x.TypeName.Equals("lớp dạy", StringComparison.OrdinalIgnoreCase)))
                {
                    if (Request["ld_" + item.Id] == "1")
                    {
                        teacher.OptionId += "[" + item.Id + "]" + item.Name + "|";
                        lopDay = true;
                    }
                }

                if (lopDay == false)
                {
                    throw new Exception("Vui lòng chọn lớp dạy");
                }
                #endregion

                // thời gian dạy
                #region Thoi Gian Day
                foreach (var item in listOption.Where(x => x.TypeName.Equals("thời gian dạy", StringComparison.OrdinalIgnoreCase)))
                {
                    if (Request["tgd_" + item.Id] == "1")
                    {
                        teacher.OptionId += "[" + item.Id + "]" + item.Name + "|";
                        thoiGianDay = true;
                    }
                }

                if (thoiGianDay == false)
                {
                    throw new Exception("Vui lòng chọn thời gian dạy");
                }
                #endregion

                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public JsonResult DeleteTeacher(string id)
        {
            using (var trans = db.Database.BeginTransaction())
            {
                try
                {
                    var teacher = db.giasu_teacher.Where(x => x.Id == id).FirstOrDefault();
                    if (teacher == null)
                    {
                        throw new Exception("Không tìm thấy Gia sư");
                    }

                    var listClass = db.giasu_class.Where(x => x.Teacher.Contains(id + "|") && x.IsComplete != true).ToList();
                    if (listClass != null && listClass.Count() > 0)
                    {
                        foreach (var item in listClass)
                        {
                            item.Teacher = string.Empty;
                            db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                        }
                    }

                    teacher.IsDelete = true;
                    db.Entry(teacher).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    trans.Commit();

                    return Json(new object[] { true, "Xóa thành công" });
                }
                catch (Exception ex)
                {
                    trans.Dispose();
                    return Json(new object[] { false, ex.Message });
                }
            }
        }

        public JsonResult GetInfo(string id)
        {
            try
            {
                var teacher = db.giasu_teacher.Find(id);
                if (teacher == null)
                {
                    throw new Exception("Không tìm thấy thông tin");
                }

                var list_kvd = string.Empty;
                var list_md = string.Empty;
                var list_ld = string.Empty;
                var list_tgd = string.Empty;

                #region Khu vực dạy
                var arr_kvd = teacher.District?.Split('|');
                for (int i = 0; i < arr_kvd.Length - 1; i++)
                {
                    list_kvd += arr_kvd[i]?.Split(']')[1] + ",";
                }
                #endregion

                var arr_opt = teacher.OptionId?.Split('|');
                var list_option = db.giasu_option.ToList();
                var optId = string.Empty;
                var optName = string.Empty;
                var typeName = string.Empty;

                #region Môn dạy, Lớp dạy, Thời gian dạy
                for (int i = 0; i < arr_opt.Length - 1; i++)
                {
                    optId = arr_opt[i]?.Split(']')[0]?.Replace("[", string.Empty);
                    optName = arr_opt[i]?.Split(']')[1];
                    typeName = list_option.Where(x => x.Id.Equals(optId, StringComparison.OrdinalIgnoreCase)).FirstOrDefault()?.TypeName;

                    if (typeName.Equals("môn dạy", StringComparison.OrdinalIgnoreCase))
                    {
                        list_md += optName + ",";
                    }
                    else if (typeName.Equals("lớp dạy", StringComparison.OrdinalIgnoreCase))
                    {
                        list_ld += optName + ",";
                    }
                    else if (typeName.Equals("thời gian dạy", StringComparison.OrdinalIgnoreCase))
                    {
                        list_tgd += optName + ",";
                    }
                }
                #endregion

                list_kvd = list_kvd.TrimEnd(',').Replace(",", ", ");
                list_md = list_md.TrimEnd(',').Replace(",", ", ");
                list_ld = list_ld.TrimEnd(',').Replace(",", ", ");
                list_tgd = list_tgd.TrimEnd(',').Replace(",", ", ");

                return Json(new object[] { true, list_kvd, list_md, list_ld, list_tgd });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        [AllowAnonymous]
        public string SendEmailAdmin(giasu_teacher teacher)
        {
            try
            {
                string url = Request.Url.Scheme + "://" + Request.Url.Authority + "/admin/teacherman/save/v" + teacher.Id;

                string subject = "Đăng ký làm Gia sư";
                string body = "<p>Xin chào Admin,</p>" +
                    "<p><b>" + teacher?.FullName + "</b> đã đăng ký làm Gia sư từ website của bạn: " + ConfigurationManager.AppSettings["Domain"] + "</p>" +
                    "<p><u>Thông tin liên hệ:</u></p>" +
                    "<p><b>Họ tên:</b> " + teacher?.FullName + "</p>" +
                    "<p><b>Số điện thoại: </b>" + teacher?.Phone + "</p>" +
                    "<p><b>Email: </b>" + teacher?.Email + "</p>" +
                    "<p><b>Ghi chú: </b><br/>" + teacher?.Note + "</p>" +
                    "<br/><p><b>Xem thông tin chi tiết tại đây:</b></p>" +
                    "<a href='" + url + "' target='_blank'>" + url + "</a>";

                var admin_email = db.webconfigurations.FirstOrDefault()?.ContactEmail;
                if (string.IsNullOrEmpty(admin_email))
                {
                    throw new Exception(string.Empty);
                }

                var result = SendEmail.Send(admin_email, subject, body, string.Empty, string.Empty, null);
                if (!string.IsNullOrEmpty(result))
                {
                    throw new Exception(result);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                return "Sự cố khi gửi email. " + ex.Message;
            }
        }
    }
}