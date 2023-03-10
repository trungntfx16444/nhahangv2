using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Inner.Libs.Helpful;
using PKWebShop.AppLB;
using PKWebShop.Enums;
using PKWebShop.Models;
using PKWebShop.Utils;

namespace PKWebShop.Services
{
    public class PaymentServices : ServicesBase
    {
        public string Pay(order order, string locale, string callbackUrl, out string paymentUrl)
        {
            paymentUrl = string.Empty;
            webconfiguration info = UserContent.GetWebInfomation(true);
            string vnp_TmnCode = info.vnp_TmnCode; //Ma website
            string vnp_HashSecret = info.vnp_HashSecret; //Chuoi bi mat
            if (string.IsNullOrEmpty(vnp_TmnCode) || string.IsNullOrEmpty(vnp_HashSecret))
            {
                return "Không hỗ trợ thanh toán online";
            }

            //Get Config Info
            var host = $"{HttpContext.Current.Request.Url.Scheme}://{HttpContext.Current.Request.Url.Authority}";
            string vnp_Returnurl = callbackUrl; //URL nhan ket qua tra ve 
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"]; //URL thanh toan cua VNPAY 

            // Build URL for VNPAY
            VnPayLibrary vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", info.vnp_Version);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);

            List<string> locales = new() { "vn", "en" };
            if (!string.IsNullOrEmpty(locale) && locales.Contains(locale))
            {
                vnpay.AddRequestData("vnp_Locale", locale);
            }
            else
            {
                vnpay.AddRequestData("vnp_Locale", "vn");
            }

            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_TxnRef", order.Id);
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toán cho đơn hàng : #{order.Id}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_Amount", (order.GrandTotal * 100).ToString());
            vnpay.AddRequestData("vnp_ReturnUrl", $"{host}/{vnp_Returnurl}");
            vnpay.AddRequestData("vnp_IpAddr", AppFunc.GetIpAddress());
            vnpay.AddRequestData("vnp_CreateDate", order.CreatedAt.Value.ToString("yyyyMMddHHmmss"));

            paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return string.Empty;
        }

        public string Callback()
        {
            webconfiguration info = UserContent.GetWebInfomation(true);
            var vnpayData = HttpContext.Current.Request.QueryString;
            VnPayLibrary vnpay = new VnPayLibrary();
            foreach (string s in vnpayData)
            {
                //get all querystring data
                if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(s, vnpayData[s]);
                }
            }

            //vnp_TxnRef: Ma don hang merchant gui VNPAY tai command=pay    
            string orderId = vnpay.GetResponseData("vnp_TxnRef");
            //vnp_ResponseCode:Response code from VNPAY: 00: Thanh cong, Khac 00: Xem tai lieu
            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            //vnp_SecureHash: MD5 cua du lieu tra ve
            String vnp_SecureHash = HttpContext.Current.Request.QueryString["vnp_SecureHash"];
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, info.vnp_HashSecret);
            if (checkSignature)
            {
                using DBLangCustom db = new DBLangCustom();
                VNP_PaymentData? paymentData = db.VNP_PaymentData.FirstOrDefault(pay => pay.OrderId == orderId);
                order order = db.orders.Find(orderId)!;
                if (paymentData == null)
                {
                    paymentData = new VNP_PaymentData();
                    paymentData.Id = AppFunc.NewShortId();
                    paymentData.CreatedAt = DateTime.Now;
                    paymentData.vnp_TxnRef = orderId;
                    paymentData.OrderId = orderId;
                    paymentData.CustomerId = order.CustomerId;

                    db.VNP_PaymentData.Add(paymentData);
                    db.SaveChanges();
                }
                //
                paymentData.vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount"));
                paymentData.vnp_BankCode = vnpay.GetResponseData("vnp_BankCode");
                paymentData.vnp_CardType = vnpay.GetResponseData("vnp_CardType");
                paymentData.vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");
                paymentData.vnp_PayDate = vnpay.GetResponseData("vnp_PayDate");
                paymentData.vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                paymentData.vnp_SecureHashType = vnpay.GetResponseData("vnp_SecureHashType") ?? "SHA256";
                paymentData.vnp_BankTranNo = vnpay.GetResponseData("vnp_BankTranNo");
                paymentData.vnp_TransactionNo = vnpay.GetResponseData("vnp_TransactionNo");
                paymentData.UpdatedAt = DateTime.Now;
                //
                if (vnp_ResponseCode == "00" || vnp_ResponseCode == "07")
                {
                    order.Status = "2";
                    order.PaymentMethod = PaymentMethod.VNPAY.Code<string>();
                    db.Entry(order).State = EntityState.Modified;
                }
                else
                {
                    throw new Exception(VNPAY_MessageResp(vnp_ResponseCode, false));
                }
                //
                db.Entry(paymentData).State = EntityState.Modified;
                db.SaveChanges();

                new ReceiptService(db).NewFromOrder(orderId, PaymentMethod.VNPAY.Code<string>());

                return orderId;
            }
            throw new Exception("Có lỗi xảy ra trong quá trình xử lý");
        }

        public string NotiPayment(ControllerContext context, order order)
        {
            webconfiguration info = UserContent.GetWebInfomation(true);
            #region SendEmail
            string listParentProp = string.Join(",", order.Details.GroupBy(x => x.ParentPropertiesId).Select(x => x.Key).Distinct());
            string listChildProp = string.Join(",", order.Details.GroupBy(x => x.PropertiesId).Select(x => x.Key).Distinct());
            var lang = SiteLang.GetDefault().Code;
            var db = new WebShopEntities();
            var listProp = (from x in db.product_properties
                            where (listParentProp.Contains(x.ReId) && x.LangCode == lang)
                                  || (listChildProp.Contains(x.ReId) && x.LangCode == lang)
                            orderby x.name
                            select x).ToList();
            context.Controller.ViewBag.ListProp = listProp;
            string shipping = $"{order.ShippingAddress} - ";
            if (!string.IsNullOrEmpty(order.Shipping_Ward))
            {
                var ward = db.vn_ward.Find(order.Shipping_Ward);
                shipping += $"{ward.type} {ward.name} - ";
            }
            if (!string.IsNullOrEmpty(order.Shipping_District))
            {
                var ward = db.vn_district.Find(order.Shipping_District);
                shipping += $"{ward.type} {ward.name} - ";
            }
            if (!string.IsNullOrEmpty(order.Shipping_Province))
            {
                var ward = db.vn_province.Find(order.Shipping_Province);
                shipping += $"{ward.type} {ward.name}";
            }
            context.Controller.ViewBag.Shipping = shipping;
            #endregion
            string viewPath = "sendmail/NotiPayment";
            string body = AppFunc.RenderViewToString(context, viewPath, order, true);
            return SendEmail.Send(info.ContactEmail, $"Đơn hàng (#{order.Id}) đã được thanh toán ", body, string.Empty, string.Empty, null);
        }

        public string SendInvoice(ControllerContext context, string id, string type = "")
        {
            try
            {
                WebShopEntities db = new();
                var order = db.orders.Find(id);
                if (order == null || string.IsNullOrEmpty(order.CustomerEmail))
                {
                    return "";
                }

                var cus = db.customers.Find(order.CustomerId);
                if (cus == null)
                {
                    return "";
                }

                context.Controller.ViewBag.Title = $"Đơn hàng #{id}";
                var language_default = SiteLang.GetDefault();
                var listProductItem = db.orders_detail.Where(x => x.OrderId == id).ToList();
                string listParentProp = string.Join(",", listProductItem.GroupBy(x => x.ParentPropertiesId).Select(x => x.Key).Distinct());
                string listChildProp = string.Join(",", listProductItem.GroupBy(x => x.PropertiesId).Select(x => x.Key).Distinct());

                var listProp = (from x in db.product_properties
                                where (listParentProp.Contains(x.ReId) && x.LangCode == language_default.Code)
                                      || (listChildProp.Contains(x.ReId) && x.LangCode == language_default.Code)
                                orderby x.name
                                select x).ToList();

                context.Controller.ViewBag.ListProp = listProp;
                context.Controller.ViewBag.ListOrderDetail = listProductItem;

                string cusAddress = $"{cus.Address}, ";
                if (!string.IsNullOrEmpty(cus.Ward))
                {
                    var ward = db.vn_ward.Find(cus.Ward);
                    cusAddress += $"{ward.type} {ward.name}, ";
                }
                if (!string.IsNullOrEmpty(cus.District))
                {
                    var district = db.vn_district.Find(cus.District);
                    cusAddress += $"{district.type} {district.name}, ";
                }
                if (!string.IsNullOrEmpty(cus.Province))
                {
                    var province = db.vn_province.Find(cus.Province);
                    cusAddress += $"{province.type} {province.name}, ";
                }
                context.Controller.ViewBag.CusAddress = cusAddress;

                string shipping = $"{order.ShippingAddress} - ";
                if (!string.IsNullOrEmpty(order.Shipping_Ward))
                {
                    var ward = db.vn_ward.Find(order.Shipping_Ward);
                    shipping += $"{ward.type} {ward.name} - ";
                }

                if (!string.IsNullOrEmpty(order.Shipping_District))
                {
                    var ward = db.vn_district.Find(order.Shipping_District);
                    shipping += $"{ward.type} {ward.name} - ";
                }
                if (!string.IsNullOrEmpty(order.Shipping_Province))
                {
                    var ward = db.vn_province.Find(order.Shipping_Province);
                    shipping += $"{ward.type} {ward.name} - ";
                }
                context.Controller.ViewBag.Shipping = shipping;
                order.PaymentData = db.VNP_PaymentData.Where(pay => pay.OrderId == order.Id).ToList().Select(pay => new PaymentData(pay)).FirstOrDefault() ?? new PaymentData();

                webconfiguration info = UserContent.GetWebInfomation(true);
                string viewPath = "sendmail/Invoice";
                string body = AppFunc.RenderViewToString(context, viewPath, order, true);
                return SendEmail.Send(order.CustomerEmail, $"Đơn hàng của bạn đã được {(type == "payment" ? "thanh toán" : "tạo")}", body, string.Empty, string.Empty, null);
            }
            catch (Exception ex)
            {
                // ignored
            }
            return "";
        }

        public static string MessageResp(string type, string rspCode, bool inAdminPage)
        {
            switch (Ext.EnumParse<PaymentMethod>(type))
            {
                case PaymentMethod.VNPAY:
                    return VNPAY_MessageResp(rspCode, inAdminPage);
            }
            return "";
        }

        public static string VNPAY_MessageResp(string rspCode, bool inAdminPage)
        {
            if (inAdminPage)
            {
                switch (rspCode)
                {
                    case "01": return "Giao dịch đã tồn tại";
                    case "02": return "Merchant không hợp lệ (kiểm tra lại vnp_TmnCode)";
                    case "03": return "Dữ liệu gửi sang không đúng định dạng";
                    case "04": return "Khởi tạo GD không thành công do Website đang bị tạm khóa";
                    case "07": return "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường). Cần merchant xác nhận thông qua merchant admin: Từ chối/Đồng ý giao dịch";
                }
            }
            switch (rspCode)
            {
                case "05": return "Quý khách nhập sai mật khẩu thanh toán quá số lần quy định. Xin quý khách vui lòng thực hiện lại giao dịch.";
                case "06": return "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP). Xin quý khách vui lòng thực hiện lại giao dịch.";
                case "08": return "Hệ thống Ngân hàng đang bảo trì. Xin quý khách tạm thời không thực hiện giao dịch bằng thẻ/tài khoản của Ngân hàng này.";
                case "09": return "Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.";
                case "10": return "Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần.";
                case "11": return "Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.";
                case "12": return "Thẻ/Tài khoản của quý khách bị khóa.";
                case "24": return "Khách hàng hủy giao dịch.";
                case "51": return "Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.";
                case "65": return "Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.";
                case "75": return "Ngân hàng thanh toán đang bảo trì.";
                case "99": return "Lỗi từ hệ thống VNPAY";
            }
            return "Lỗi hệ thống ! Vui lòng liên hệ với chúng tôi hoặc thanh toán lại !";
        }
    }
}