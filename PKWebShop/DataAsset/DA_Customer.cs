namespace PKWebShop.DataAsset
{
    using System;
    using System.Linq;
    using System.Web;
    using PKWebShop.Models;

    public class DA_Customer
    {
        // Khoi tao Constructors
        private WebShopEntities db;

        public DA_Customer()
        {
            db = new WebShopEntities();
        }

        public DA_Customer(WebShopEntities dataModel)
        {
            db = dataModel;
        }

        #region Function
        public customer CustomerRegister(customer data, out string errMsg)
        {
            errMsg = string.Empty;
            try
            {
                Random rd = new ();
                var cus = db.customers.Where(c => c.Phone == data.Phone).FirstOrDefault();
                if (cus == null)
                {
                    cus = new customer();
                    cus = data;
                    cus.Id = AppLB.CommonFunc.RandomNumber(DateTime.Now.ToString("yyMMddHHmmss"), rd);
                    cus.Active = true;
                    cus.CreateAt = DateTime.Now;
                    cus.CreateBy = "System";
                    db.customers.Add(cus);
                    db.SaveChanges();
                    return cus;
                }
                else
                {
                    throw new Exception("Số điện thoại đã tồn tại.");
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.InnerException?.Message ?? ex.Message;
                return null;
            }
        }

        public customer CustomerLogin(customer cus, out string errMsg)
        {
            errMsg = string.Empty;
            try
            {
                HttpCookie ck = new ("Customer");
                ck.Value = AppLB.SecurityLibrary.Encrypt(cus.Phone + "|" + cus.Email + "|" + cus.Password);
                ck.Expires = DateTime.Now.AddDays(15);
                HttpContext.Current.Response.Cookies.Add(ck);
                return cus;
            }
            catch (Exception ex)
            {
                errMsg = ex.InnerException?.Message ?? ex.Message;
                return null;
            }
        }

        public customer CustomerLogin(string email, string password, out string errMsg)
        {
            errMsg = string.Empty;
            try
            {
                var cus = db.customers.FirstOrDefault(c => c.Email == email && c.Password == password && c.Active == true);
                if (cus != null)
                {
                    return CustomerLogin(cus, out errMsg);
                }
                else
                {
                    throw new Exception("Số điện thoại hoặc mật khẩu không đúng");
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.InnerException?.Message ?? ex.Message;
                return null;
            }
        }
        #endregion
    }
}