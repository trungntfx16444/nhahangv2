using PKWebShop.Utils;

namespace PKWebShop.Controllers
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Web.Mvc;
    using PKWebShop.AppLB;
    using PKWebShop.Models;
    using PKWebShop.Models.CustomizeModels;

    public class ContactController : ExpiredCheckController

    {
        // GET: Contact
        public ActionResult Index()
        {
            DBLangCustom db = new ();
            var webinfo = db.webconfigurations.FirstOrDefault();
            // Giới thiệu
            string s_contact = UserContent.Web_Feature.contact.ToString();
            var contact = db.sectionfeatures.Where(s => s.ReId == s_contact && s.Status == true).FirstOrDefault();
            if (contact != null)
            {
                var contact_detail = db.sectionfeaturedetails.Where(f => f.SectionCode == contact.ReId).FirstOrDefault();
                ViewBag.contact = contact_detail;
                var contact_banner = db.uploadmorefiles.Where(f => f.TableId == contact_detail.Id && f.TableName == "sectionfeaturedetails").FirstOrDefault();
                ViewBag.contact_banner = contact_banner;
            }
            
            // ViewBag.tranglienhe_topbg = (from f in db.sectionfeaturedetails.AsEnumerable()
            //                             join s in db.sectionfeatures on f.SectionCode equals s.Code
            //                             where s.Status == true
            //                             && s.Code == UserContent.Web_Feature.tranglienhe_topbg.ToString()
            //                             select new SectionFeatureModel
            //                             {
            //                                 Description = f.Description,
            //                                 Picture = f.Picture,
            //                                 SectionCode = f.SectionCode,
            //                                 SectionTitle = s.Title,
            //                                 Title = f.Title,
            //                                 URL = f.URL,
            //                                 VolumeNumber = f.VolumeNumber,
            //                             }).FirstOrDefault();
            return View(webinfo);
        }

        [HttpPost]
        public JsonResult CustomerRequest([System.Web.Http.FromBody] customer_request rq)
        {
            WebShopEntities db = new ();
            using (var trans = db.Database.BeginTransaction())
            {
                try
                {
                    if (string.IsNullOrEmpty(rq.FullName))
                    {
                        throw new Exception("Vui lòng nhập Họ tên");
                    }
                    else if (string.IsNullOrEmpty(rq.Phone))
                    {
                        throw new Exception("Vui lòng nhập Số điện thoại");
                    }

                    var request = new customer_request()
                    {
                        Id = AppFunc.NewShortId(),
                        FullName = rq.FullName,
                        Email = rq.Email,
                        Phone = rq.Phone,
                        FromDate = DateTime.Now,

                        // Appointment = rq.Appointment,
                        Note = rq.Note,
                        Status = 0,
                        CreateAt = DateTime.Now,
                    };
                    db.customer_request.Add(request);
                    db.SaveChanges();

                    #region SendEmail
                    string subject = "Khách hàng liên hệ";
                    string link_ne = UserContent.GetAdminSite() + "/appointment";
                    string body = "<p>Xin chào Admin,</p>" +
                        "<p>Bạn nhận được một email liên hệ của Khách hàng từ website: " + ConfigurationManager.AppSettings["Domain"] + "</p>" +
                        "<table cellpadding='20' cellspacing='0' style='width: 600px;margin-left: auto;margin-right: auto;border: 1px solid #dedede; border-radius: 3px;'><thead><tr><td style='padding: 12px 48px; background-color: #0193de; border-bottom: 0; font-weight: bold; border-radius: 3px 3px 0 0;'><h1 style='font-size:22px;font-weight:300;line-height:150%;margin:0;text-align:left;color:#ffffff;background-color:inherit;text-align:center;'>Thông Tin Liên Hệ</h1></td></tr></thead><tbody><tr><td valign='top' style='padding:32px 48px 32px'><div style='color:#636363;font-family:&quot;Helvetica Neue&quot;,Helvetica,Roboto,Arial,sans-serif;font-size:14px;line-height:150%;text-align:left'>" +
                        "<p style='margin: 0 0 16px;'><b>Họ tên:</b> " + rq.FullName + "</p>" +
                        "<p style='margin: 0 0 16px;'><b>Số điện thoại:</b> " + rq.Phone + "</p>" +
                        "<p style='margin: 0 0 16px;'><b>Email:</b> " + rq.Email + "</p>" +

                        // "<p style='margin: 0 0 16px;'><b>Thời gian đặt hẹn:</b> " + rq.Appointment?.ToString("dd/MM/yyyy HH:mm") + "</p>" +
                        "<p style='margin: 0 0 16px;'><b>Ghi chú:</b> " + rq.Note?.Replace("\r\n", "<br/>") + "</p>" +
                        "<p style='margin: 0 0;'>Vui lòng click vào <a href='" + link_ne + "' target='_blank'>link này</a> để vào trang quản trị " + link_ne + "</p>" +
                        "</div></td></tr></tbody><tfoot style='background-color: #eff1f3;'><tr><td style='text-align: center;padding: 10px; font-size: 12px;font-family:&quot;Helvetica Neue&quot;,Helvetica,Roboto,Arial,sans-serif;'> Đây là email liên hệ của Khách hàng từ website: " + ConfigurationManager.AppSettings["Domain"] + "</td></tr></tfoot></table>";

                    var admin_email = UserContent.GetWebInfomation()?.ContactEmail;
                    if (string.IsNullOrEmpty(admin_email))
                    {
                        throw new Exception("Đã xảy ra sự cố khi gửi email");
                    }

                    var result = SendEmail.Send(admin_email, subject, body, string.Empty, string.Empty, null);
                    if (!string.IsNullOrEmpty(result))
                    {
                        throw new Exception("Đã xảy ra sự cố khi gửi email");
                    }
                    #endregion

                    trans.Commit();
                    return Json(new object[] { true, "Liên hệ thành công. Chúng tôi sẽ sớm liên lạc với bạn." });
                }
                catch (Exception ex)
                {
                    trans.Dispose();
                    return Json(new object[]{ false, ex.Message });
                }
            }
        }

        /// <summary>
        /// Send email feedback.
        /// </summary>
        /// <returns>json.</returns>
        [HttpPost]
        public JsonResult SendEmailFeedBack()
        {
            try
            {
                var name = Request["contact-form-name"];
                var email = Request["contact-form-email"];
                var phone = Request["contact-form-mobile"];
                var message = Request.Unvalidated["contact-form-message"];
                var option = Request["options"] ?? "Khách hàng";

                if (string.IsNullOrEmpty(name) == true || string.IsNullOrEmpty(email) == true || string.IsNullOrEmpty(phone) == true || string.IsNullOrEmpty(message) == true)
                {
                    throw new Exception("Vui lòng điền đầy đủ thông tin vào khung liên hệ phía dưới.");
                }
                else
                {
                    string subject = option + " liên hệ";
                    string body = "<p>Xin chào Admin,</p>" +
                        "<p>Bạn nhận được một email liên hệ của <b>" + option + "</b> từ website: " + ConfigurationManager.AppSettings["Domain"] + "</p>" +
                        "<p><u>Thông tin liên lạc:</u></p>" +
                        "<p><b>Họ tên:</b> " + name + "</p>" +
                        "<p><b>Số điện thoại: </b>" + phone + "</p>" +
                        "<p><b>Email: </b>" + email + "</p>" +
                        "<p><b>Nội dung phản hồi: </b><br/>" + message.Replace("\r\n", "<br/>") + "</p>";

                    var admin_email = UserContent.GetWebInfomation()?.ContactEmail;
                    if (string.IsNullOrEmpty(admin_email))
                    {
                        throw new Exception("Đã xảy ra sự cố khi gửi email");
                    }

                    var result = SendEmail.Send(admin_email, subject, body, string.Empty, string.Empty, null);

                    if (result == string.Empty)
                    {
                        return Json(new object[] { true, "Gửi email thành công." });
                    }
                    else
                    {
                        throw new Exception(result);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, "Gửi email thất bại! " + ex.Message });
            }
        }
    }
}