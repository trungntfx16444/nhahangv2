using AdminPage.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AdminPage.Controllers
{
    public class ExpiredCheckController : Controller
    {
        // GET: ExpiredCheck
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Request.Path == "/thong-bao-het-han" || Session["notExpried"]?.ToString() == "true")
            {
                return;
            }
            if (new AdminPage.Models.AdminEntities().packages.FirstOrDefault(p => p.PackageType == "web_package")?.ExpirationDate < DateTime.Now.Date)
            {
                filterContext.Result = new RedirectResult("/thong-bao-het-han");
                return;
            }
            else
            {
                Session["notExpried"] = "true";
            }
        }
    }
}