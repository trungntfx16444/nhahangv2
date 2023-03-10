using System.Web;
using System.Web.SessionState;

namespace PKWebShop
{
  public class KeepAlive : IHttpHandler, IRequiresSessionState
  {
    public void ProcessRequest (HttpContext context) {
      if (context.User.Identity.IsAuthenticated)
      {
        // authenticated sessions
        context.Response.ContentType = "text/plain";
        context.Response.Write("Auth: Alive");
        //  + context.Session.SessionID
      }
      else
      {
        // guest
        context.Response.ContentType = "text/plain";
        context.Response.Write("NoAuth: Alive");
        //  + context.Session.SessionID
      }
    }
    public bool IsReusable => false;
  }
}