namespace PKWebShop.DataAsset
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using PKWebShop.Models;

    public class DA_Service
    {
        // Khoi tao Constructors
        private WebShopEntities db;

        public DA_Service()
        {
            db = new WebShopEntities();
        }

        public DA_Service(WebShopEntities dataModel)
        {
            db = dataModel;
        }

        #region Function

        public List<product> UpdateSession_Service(string key, string service_id, List<product> lst_service, out string errMsg)
        {
            errMsg = string.Empty;
            try
            {
                if (key == "del")
                {
                    if (lst_service.Any(x => x.Id == service_id) == true)
                    {
                        var ser = lst_service.Where(x => x.Id == service_id).FirstOrDefault();
                        lst_service.Remove(ser);
                    }
                }
                else if (key == "add")
                {
                    if (lst_service.Any(x => x.Id == service_id) == false)
                    {
                        product ser = db.products.Find(service_id);
                        if (ser != null)
                        {
                            lst_service.Add(ser);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.InnerException?.Message ?? ex.Message;
            }

            return lst_service;
        }

        #endregion
    }
}