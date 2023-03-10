namespace AdminPage.AppLB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Security;
    using AdminPage.Models;

    public class Authority
    {
        public static user GetThisUser(bool reload = true)
        {
            try
            {
                if (HttpContext.Current.User.Identity.IsAuthenticated == true)
                {
                    if (HttpContext.Current.Session["user"] == null || reload == true)
                    {
                        var a = HttpContext.Current.Request.Cookies["admin_user"];
                        FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(a?.Value);
                        string[] info = ticket.UserData?.Split(new char[] { '|' });
                        string username = info[0];
                        string password = info[1];
                        var user = new AdminEntities().users.Where(u => u.UserName == username && u.Password == password).FirstOrDefault();
                        if (user != null)
                        {
                            return user;
                        }
                    }
                    else
                    {
                        return HttpContext.Current.Session["user"] as user;
                    }
                }
            }
            catch
            {
            }
            FormsAuthentication.SignOut();
            HttpContext.Current.Session["user"] = null;
            // HttpContext.Current.User.Identity.IsAuthenticated = false;
            return null;
        }

        public static Dictionary<string, bool> GetAccessAuthority(user curMem = null, bool reload = false)
        {
            var access = new Dictionary<string, bool>();
            try
            {
                if (curMem == null)
                {
                    curMem = GetThisUser();
                    if (curMem == null)
                    {
                        return access;
                    }
                }
                string memberRole = curMem.Role;

                if (HttpContext.Current.Session["session_access"] == null || reload == true)
                {
                    var db = new AdminEntities();
                    if (memberRole == UserContent.UserRole.Admin.ToString())
                    {
                        foreach (var item in db.users_permissions.ToList())
                        {
                            access.Add(item.Code, true);
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(memberRole))
                    {
                        foreach (var item in db.users_permissions_granted.Where(p => p.UserRole.Equals(memberRole.Trim())).OrderBy(p => p.PermissionCode).ToList())
                        {
                            access.Add(item.PermissionCode, true);
                        }
                    }
                    return access;
                }
                else
                {
                    return HttpContext.Current.Session["session_access"] as Dictionary<string, bool>;
                }
            }
            catch (Exception)
            {
                return access;
            }
        }
    }
}