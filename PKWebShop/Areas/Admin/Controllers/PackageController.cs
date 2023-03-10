using System.Web.Mvc;
using PKWebShop.Services;
using PKWebShop.Utils;

namespace PKWebShop.Areas.Admin.Controllers
{
  public class PackageController : ExpiredCheckController
    {
    // GET
    public ActionResult Index()
    {
      return View();
    }

    public ActionResult AvaiablePackage()
    {
      var rs = AppFunc.RenderViewToString(ControllerContext, "_tableData", new PackageServices().PackageAvaiable(), true);
      return Json(new object[] { rs }, JsonRequestBehavior.AllowGet);
    }
  }
}