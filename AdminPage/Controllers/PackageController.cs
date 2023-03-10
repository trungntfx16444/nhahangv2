using System.Web.Mvc;
using AdminPage.Services;
using AdminPage.Utils;

namespace AdminPage.Controllers
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