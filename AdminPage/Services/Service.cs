namespace AdminPage.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using AdminPage.Models;
    internal class services
    {
        private static SiteLang.Language defaultLanguage = SiteLang.GetDefault();

        public static service SaveService(service sm)
        {
            using (var db = new AdminEntities())
            {
                if (!string.IsNullOrEmpty(sm.ServiceId))
                {
                    // update
                    if (string.IsNullOrWhiteSpace(sm.Name))
                    {
                        throw new Exception("Tên dịch vụ không được trống");
                    }
                    var sv = db.services.Find(sm.ServiceId);
                    sv.Name = sm.Name;
                    sv.Order = sm.Order;
                    sv.ShortDescription = sm.ShortDescription;
                    sv.Decription = sm.Decription;
                    sv.Picture = sm.Picture;
                    sv.Price = sm.Price;
                    sv.CurrencyUnit = sm.CurrencyUnit;
                    db.Entry(sv).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    //AppLB.UserContent.GetWebService(true);
                    sm = sv;
                }
                else
                {
                    // add new
                    if (string.IsNullOrWhiteSpace(sm.Name))
                    {
                        throw new Exception("Tên dịch vụ không được trống");
                    }
                    Random rd = new ();
                    sm.ServiceId = AppLB.CommonFunc.RandomNumber(DateTime.Now.ToString("yyMMddHHmmss"), rd);
                    db.services.Add(sm);
                    db.SaveChanges();

                    // cap nhat lai session
                    //AppLB.UserContent.GetWebService(true);
                }
                return sm;
            }
        }
    }
    
}