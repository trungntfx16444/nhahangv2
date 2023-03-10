namespace AdminPage.Services
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Inner.Libs.Helpful;
    using AdminPage.AppLB;
    using AdminPage.Models;
    using AdminPage.Utils;

    public class CustomerService : ServicesBase
    {
        public void LastLogin(customer cus)
        {
            cus.LastLogin = DateTime.Now.ToString("s");
            DB.Entry(cus).State = EntityState.Modified;
            DB.SaveChanges();
        }

        public customer Register(customer rcus)
        {
            try
            {
                var _cus = DB.customers.FirstOrDefault(cus => (!string.IsNullOrEmpty(rcus.Email) && cus.Email == rcus.Email) || (!string.IsNullOrEmpty(rcus.Phone) && cus.Phone == rcus.Phone));
                if (_cus != null && _cus.Active == false)
                {
                    return null;
                }
                if (_cus != null && !string.IsNullOrEmpty(_cus.Password))
                {
                    //throw new Exception("Email hoặc số điện thoại này đã có tài khoản! Hãy đăng nhập hoặc sử dụng email, sđt khác!");
                }

                if (string.IsNullOrEmpty(rcus.FullName) || string.IsNullOrWhiteSpace(rcus.FullName))
                {
                    throw new Exception("Họ tên không được trống");
                }
                if (rcus.AccountType == "Web" && (string.IsNullOrEmpty(rcus.Password) || string.IsNullOrWhiteSpace(rcus.Password)))
                {
                    throw new Exception("Password không được trống");
                }
                if (string.IsNullOrWhiteSpace(rcus.Email) && string.IsNullOrWhiteSpace(rcus.Phone))
                {
                    throw new Exception("Email hoặc Số điện thoại không được trống");
                }

                if (_cus == null)
                {
                    _cus = rcus;
                    _cus.Id = AppFunc.NewShortId();
                    _cus.Active = true;
                    _cus.Role = UserContent.UserRole.Member.Text();
                    _cus.CreateAt = DateTime.Now;
                    _cus.CreateBy = rcus.AccountType;
                }
                _cus.Avatar = rcus.Avatar;
                _cus.Email = string.IsNullOrEmpty(rcus.Email) ? _cus.Email : rcus.Email;
                _cus.Password = string.IsNullOrEmpty(rcus.Password) ? _cus.Password : rcus.Password;
                _cus.FullName = rcus.FullName ?? rcus.UserName;
                _cus.AccountType = rcus.AccountType;
                _cus.Phone = string.IsNullOrEmpty(rcus.Phone) ? _cus.Phone : rcus.Phone;
                _cus.LastLogin = DateTime.Now.ToString("s");

                // _cus.UserName = rcus.UserName ?? CommonFunc.ConvertNonUnicodeURL(rcus.FullName).Replace("-", "_") + "_" + new Random().Next(0, 999).ToString();
                DB.customers.AddOrUpdate(_cus);
                DB.SaveChanges();
                return _cus;
            }
            catch (Exception e)
            {
                if (e is AppHandleException)
                {
                    throw new AppHandleException(e.Message);
                }
                throw new AppHandleException(e.Message);
            }
        }
    }
}