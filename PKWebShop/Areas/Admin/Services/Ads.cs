namespace PKWebShop.Services
{
    using System;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using PKWebShop.AppLB;
    using PKWebShop.Models;
    using PKWebShop.Utils;

    internal class Ads
    {
        private static SiteLang.Language defaultLanguage = SiteLang.GetDefault();

        internal static popupad Save(popupad nm)
        {
            using (var db = new WebShopEntities())
            {
                if (string.IsNullOrWhiteSpace(nm.Title))
                {
                    throw new Exception("Tiêu đề không được trống.");
                }

                popupad ads = null;
                if (string.IsNullOrEmpty(nm.Id))
                {
                    string reId = AppFunc.NewShortId();
                    foreach (var item in SiteLang.GetListLangs())
                    {
                        var result = CommonFunc.Translate(nm.Title, null, "vi", item.Code).FirstOrDefault();
                        if (result.Key != string.Empty)
                        {
                            throw new Exception(result.Key);
                        }

                        var new_ads = new popupad()
                        {
                            Id = AppFunc.NewShortId(),
                            Title = result.Value.First(),
                            PopupAdsPicture = nm.PopupAdsPicture,
                            PopupAdsFrom = nm.PopupAdsFrom,
                            PopupAdsTo = nm.PopupAdsTo,
                            PopupAdsURL = nm.PopupAdsURL,
                            Description = nm.Description,
                            Active = nm.Active,
                            LangCode = item.Code,
                            ReId = reId,
                        };
                        db.popupads.Add(new_ads);
                        if (item.Code == "vi")
                        {
                            ads = new_ads;
                        }
                    }
                }
                else
                {
                    ads = db.popupads.Find(nm.Id);
                    if (ads == null)
                    {
                        throw new Exception("Popup quảng cáo không tồn tại");
                    }
                    ads.Title = nm.Title;
                    ads.PopupAdsPicture = nm.PopupAdsPicture;
                    ads.PopupAdsFrom = nm.PopupAdsFrom;
                    ads.PopupAdsTo = nm.PopupAdsTo;
                    ads.PopupAdsURL = nm.PopupAdsURL;
                    ads.Description = nm.Description;
                    ads.Active = nm.Active;
                    db.Entry(ads).State = System.Data.Entity.EntityState.Modified;

                    foreach (var item in db.popupads.Where(x => x.ReId == ads.ReId && x.Id != ads.Id).ToList())
                    {
                        item.PopupAdsFrom = ads.PopupAdsFrom;
                        item.PopupAdsTo = ads.PopupAdsTo;
                        item.Active = ads.Active;
                        db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                    }
                }
                db.SaveChanges();
                return ads;
            }
        }
    }
}