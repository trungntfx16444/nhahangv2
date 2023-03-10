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
    using PKWebShop.Models.CustomizeModels;

    public class ClassManController : Controller
    {
        private WebShopEntities db = new ();

        // GET: Admin/ClassMan
        public ActionResult Index()
        {
            return View(db.giasu_class.Where(x => x.IsDelete != true).OrderByDescending(x => x.CreateAt).ToList());
        }

        [HttpPost]
        public JsonResult SaveClass(giasu_class dataForm)
        {
            try
            {
                // Status: "Lớp chưa giao", "Lớp đã giao không gia sư", "Lớp đã giao có gia sư"
                string status = string.IsNullOrEmpty(Request["Status"]) ? "Lớp chưa giao" : Request["Status"];

                if (string.IsNullOrEmpty(dataForm.Id))
                {
                    // add new class
                    Random rd = new ();
                    var class_ = new giasu_class();
                    class_ = dataForm;
                    class_.Id = AppLB.CommonFunc.RandomNumber(DateTime.Now.ToString("yyMMddHHmmss"), rd);
                    class_.Code = (db.giasu_class.Count() + 1).ToString().PadLeft(4, '0');
                    class_.Note = Request.Unvalidated["Note"];
                    class_.Active = true;
                    class_.CreateAt = DateTime.Now;
                    class_.CreateBy = User.Identity.Name ?? string.Empty;

                    if (status == "Lớp chưa giao" || status == "Lớp đã giao không gia sư")
                    {
                        class_.Status = status;
                        class_.Teacher = string.Empty;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(Request["listIdTeacher"]))
                        {
                            class_.Status = "Lớp chưa giao";
                            class_.Teacher = string.Empty;
                        }
                        else
                        {
                            class_.Status = status;
                            class_.Teacher = Request["listIdTeacher"];
                        }
                    }

                    db.giasu_class.Add(class_);
                    db.SaveChanges();
                    TempData["s"] = "Thêm mới Lớp học thành công";

                    return Json(new object[] { true, "Thêm mới Lớp học thành công", class_.Id });
                }
                else
                {
                    // update teacher
                    var class_ = db.giasu_class.Find(dataForm.Id);
                    if (class_ == null)
                    {
                        throw new Exception("Không tìm thấy Lớp học");
                    }

                    class_.ContactName = dataForm.ContactName;
                    class_.ProvinceOrCity = dataForm.ProvinceOrCity;
                    class_.District = dataForm.District;
                    class_.HomeNumber = dataForm.HomeNumber;
                    class_.ContactAddress = dataForm.ContactAddress;
                    class_.ContactPhone = dataForm.ContactPhone;
                    class_.ContactEmail = dataForm.ContactEmail;
                    class_.ClassName = dataForm.ClassName;
                    class_.Subjects = dataForm.Subjects;
                    class_.StudentQty = dataForm.StudentQty;
                    class_.LessonNumber = dataForm.LessonNumber;
                    class_.LearningTime = dataForm.LearningTime;
                    class_.DegreeRequired = dataForm.DegreeRequired;
                    class_.Note = Request.Unvalidated["Note"];
                    class_.Salary = dataForm.Salary;

                    class_.UpdateAt = DateTime.Now;
                    class_.UpdateBy = User.Identity.Name;

                    if (!string.IsNullOrEmpty(Request["IdTeacherExist"]))
                    {
                        var listIdTeacher = Request["listIdTeacher"] ?? string.Empty;
                        var exitSplit = Request["IdTeacherExist"].Split('|');
                        for (int i = 0; i < exitSplit.Length - 1; i++)
                        {
                            if (listIdTeacher.Contains(exitSplit[i] + "|") == false)
                            {
                                listIdTeacher += exitSplit[i] + "|";
                            }
                        }
                    }
                    else
                    {
                        class_.Teacher = Request["listIdTeacher"];
                    }

                    if (Request["Active"] == "1")
                    {
                        class_.Active = true;
                    }
                    else
                    {
                        class_.Active = false;
                    }

                    if (Request["IsComplete"] == "1")
                    {
                        class_.IsComplete = true;
                    }
                    else
                    {
                        class_.IsComplete = false;
                    }

                    if (status == "Lớp chưa giao" || status == "Lớp đã giao không gia sư")
                    {
                        class_.Status = status;
                        class_.Teacher = string.Empty;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(Request["listIdTeacher"]))
                        {
                            class_.Status = "Lớp chưa giao";
                            class_.Teacher = string.Empty;
                        }
                        else
                        {
                            class_.Status = status;
                            class_.Teacher = Request["listIdTeacher"];
                        }
                    }

                    db.Entry(class_).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    TempData["s"] = "Cập nhật thông tin Lớp học thành công";

                    return Json(new object[] { true, "Cập nhật thông tin Lớp học thành công", class_.Id });
                }
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, "Lưu thất bại. " + ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult ClientRegisterClass(giasu_class dataForm)
        {
            try
            {
                if (string.IsNullOrEmpty(dataForm.ContactPhone))
                {
                    throw new Exception("Vui lòng nhập số điện thoại");
                }

                // client register class
                Random rd = new ();
                var class_ = new giasu_class();
                class_ = dataForm;
                class_.Id = AppLB.CommonFunc.RandomNumber(DateTime.Now.ToString("yyMMddHHmmss"), rd);
                class_.Code = (db.giasu_class.Count() + 1).ToString().PadLeft(4, '0');
                class_.Note = Request.Unvalidated["Note"];
                class_.Active = true;
                class_.Teacher = Request["listIdTeacher"];
                class_.CreateAt = DateTime.Now;
                class_.CreateBy = User.Identity.Name ?? string.Empty;

                if (!string.IsNullOrEmpty(class_.Teacher))
                {
                    class_.Status = "Lớp đã giao có gia sư";
                }
                else
                {
                    class_.Status = "Lớp chưa giao";
                }

                db.giasu_class.Add(class_);

                // gui email thong bao admin
                string result = SendEmailAdmin(class_);
                if (!string.IsNullOrEmpty(result))
                {
                    throw new Exception(result);
                }

                db.SaveChanges();

                return Json(new object[] { true, "Đăng ký tìm Gia sư thành công", class_.Id });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        public ActionResult Approved(string id)
        {
            try
            {
                var @class = db.giasu_class.Find(id);
                if (@class == null)
                {
                    throw new Exception("Không tìm thấy lớp cần duyệt");
                }

                @class.ApprovedAt = DateTime.Now;
                @class.ApprovedBy = User.Identity.Name;
                db.Entry(@class).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                TempData["s"] = "Duyệt lớp thành công!";
                return RedirectToAction("Save", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["e"] = ex.Message;
                return RedirectToAction("index");
            }
        }

        public JsonResult DeleteClass(string id)
        {
            try
            {
                var @class = db.giasu_class.Where(x => x.Id == id).FirstOrDefault();
                if (@class == null)
                {
                    throw new Exception("Không tìm thấy lớp");
                }

                @class.IsDelete = true;
                db.Entry(@class).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                return Json(new object[] { true, "Xóa thành công" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        [AllowAnonymous]
        public string SendEmailAdmin(giasu_class _class)
        {
            try
            {
                string url = Request.Url.Scheme + "://" + Request.Url.Authority + "/admin/classman/save/v" + _class?.Id;

                string subject = "Phụ huynh đăng ký tìm Gia sư";
                string body = "<p>Xin chào Admin,</p>" +
                    "<p>Phụ huynh <b>" + _class?.ContactName + "</b> đã đăng ký tìm Gia sư từ website của bạn: " + ConfigurationManager.AppSettings["Domain"] + "</p>" +
                    "<p><u>Thông tin liên hệ:</u></p>" +
                    "<p><b>Họ tên:</b> " + _class?.ContactName + "</p>" +
                    "<p><b>Số điện thoại: </b>" + _class?.ContactPhone + "</p>" +
                    "<p><b>Email: </b>" + _class?.ContactEmail + "</p>" +
                    "<p><b>Ghi chú: </b><br/>" + _class?.Note + "</p>" +
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