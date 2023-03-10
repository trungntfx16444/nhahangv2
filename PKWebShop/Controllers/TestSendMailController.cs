using System;
using System.Linq;
using System.Web.Mvc;
using PKWebShop.Models;
using PKWebShop.Services;

namespace PKWebShop.Controllers
{
  public class TestSendMailController : Controller
  {
    // GET
    public ActionResult Index(string id)
    {
      try
      {
        WebShopEntities db = new();
        var order = db.orders.Find(id);
        if (order != null)
        {
          ViewBag.Title = $"Đơn hàng #{id}";
          var language_default = SiteLang.GetDefault();

          var listProductItem = db.orders_detail.Where(x => x.OrderId == id).ToList();
          string listParentProp = string.Join(",", listProductItem.GroupBy(x => x.ParentPropertiesId).Select(x => x.Key).Distinct());
          string listChildProp = string.Join(",", listProductItem.GroupBy(x => x.PropertiesId).Select(x => x.Key).Distinct());

          var listProp = (from x in db.product_properties
            where (listParentProp.Contains(x.ReId) && x.LangCode == language_default.Code)
                  || (listChildProp.Contains(x.ReId) && x.LangCode == language_default.Code)
            orderby x.name
            select x).ToList();

          ViewBag.ListProp = listProp;
          ViewBag.ListOrderDetail = listProductItem;

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

          ViewBag.Shipping = shipping;
          order.PaymentData = db.VNP_PaymentData.Where(pay => pay.OrderId == order.Id).ToList().Select(pay => new PaymentData(pay)).FirstOrDefault() ?? new PaymentData();
          return View("sendmail/Invoice", order);
        }
        else
        {
          throw new Exception("Đơn hàng không tồn tại.");
        }
      }
      catch (Exception ex)
      {
        TempData["error"] = ex.Message;
        return RedirectToAction("index");
      }

    }
  }
}