namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using PKWebShop.AppLB;
    using PKWebShop.Models;

    [Authorize]
    public class UsersController : ExpiredCheckController
    {
        private WebShopEntities db = new ();
        private Dictionary<string, bool> access = Authority.GetAccessAuthority();

        // GET: Admin/Users
        public ActionResult Index()
        {
            if (!access.ContainsKey("update_member"))
            {
                TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                return RedirectToAction("index", "home");
            }

            var curMem = Authority.GetThisUser(false);
            var role_admin = (int)UserContent.UserRole.Admin;
            var role_manager = (int)UserContent.UserRole.Manager;
            var role_member = (int)UserContent.UserRole.Member;

            IQueryable list_user = Enumerable.Empty<user>().AsQueryable();
            if (curMem.RoleLevel == role_admin)
            {
                list_user = db.users.OrderByDescending(x => x.RoleLevel);
            }
            else if (curMem.RoleLevel == role_manager)
            {
                list_user = db.users.Where(x => x.RoleLevel <= role_manager).OrderByDescending(x => x.RoleLevel);
            }
            else
            {
                list_user = db.users.Where(x => x.RoleLevel <= role_member).OrderByDescending(x => x.RoleLevel);
            }

            ViewBag.CurMem = curMem;
            return View(list_user);
        }

        #region MEMBER MANAGEMENT
        public ActionResult Save(string id)
        {
            try
            {
                var curMem = Authority.GetThisUser(false);
                if (!string.IsNullOrEmpty(id))
                {
                    var user_ = db.users.Find(id);
                    if (user_ == null)
                    {
                        throw new Exception("Thành viên không tồn tại");
                    }

                    if (curMem.UserName != user_.UserName)
                    {
                        if (!access.ContainsKey("update_member"))
                        {
                            TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                            return RedirectToAction("index", "home");
                        }
                        else if (curMem.RoleLevel < user_.RoleLevel || (curMem.RoleLevel == user_.RoleLevel && curMem.RoleLevel != (int)UserContent.UserRole.Admin))
                        {
                            throw new Exception("Bạn không có quyền thông tin của thành viên này");
                        }
                    }

                    ViewBag.CurMem = curMem;
                    return View(user_);
                }
                else
                {
                    if (!access.ContainsKey("update_member"))
                    {
                        TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                        return RedirectToAction("index", "home");
                    }

                    ViewBag.CurMem = curMem;
                    return View(new user { Active = true });
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("index");
            }
        }

        [HttpPost]
        [ActionName("Save")]
        public ActionResult SaveSubmit(user UM)
        {
            var retypePass = string.Empty;
            try
            {
                var curMem = Authority.GetThisUser(false);
                Random rd = new ();

                if (!string.IsNullOrEmpty(UM.UserId))
                {
                    // update
                    var user = db.users.Find(UM.UserId);
                    if (curMem.UserName != user.UserName)
                    {
                        if (!access.ContainsKey("update_member"))
                        {
                            TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                            return RedirectToAction("index", "home");
                        }
                        else if (curMem.RoleLevel < user.RoleLevel || (curMem.RoleLevel == user.RoleLevel && curMem.RoleLevel != (int)UserContent.UserRole.Admin))
                        {
                            throw new Exception("Bạn không có quyền cập nhật thông tin thành viên này");
                        }
                    }

                    user.Fullname = UM.Fullname;
                    user.Avatar = Request.Unvalidated["textarea_Avatar"];
                    user.Phone = UM.Phone;
                    user.Email = UM.Email;
                    user.Address = UM.Address;
                    user.Role = UM.Role;
                    user.Active = UM.Active;
                    user.UpdateBy = User.Identity.Name;
                    user.UpdateAt = DateTime.Now;
                    user.RoleLevel = UM.RoleLevel;
                    user.Role = GetRoleName(UM.RoleLevel);
                    if (curMem.RoleLevel == (int)UserContent.UserRole.Admin && (user.Password != UM.Password))
                    {
                        user.Password = UM.Password;
                    }

                    db.Entry(user).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    if (user.UserName == curMem.UserName)
                    {
                        // reload curMem & role
                        var updateCurMem = Authority.GetThisUser();
                        Authority.GetAccessAuthority(updateCurMem, true);
                    }
                    TempData["success"] = "Cập nhật thành công";
                    return RedirectToAction("save", new { id = UM.UserId });
                }
                else
                {
                    // add new
                    if (!access.ContainsKey("update_member"))
                    {
                        TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                        return RedirectToAction("index", "home");
                    }

                    if (curMem.RoleLevel < UM.RoleLevel && curMem.RoleLevel != (int)UserContent.UserRole.Admin)
                    {
                        throw new Exception($"Bạn không có quyền để tạo thành viên có vai trò là '{GetRoleName(UM.RoleLevel)}'");
                    }

                    retypePass = Request["RetypePassword"];
                    if (string.IsNullOrEmpty(UM.UserName))
                    {
                        throw new Exception("Username không được trống");
                    }
                    else if (string.IsNullOrEmpty(UM.Password))
                    {
                        throw new Exception("Password không được trống");
                    }
                    else if (!string.IsNullOrEmpty(UM.Password) && UM.Password != retypePass)
                    {
                        throw new Exception("Password nhập lại không đúng");
                    }
                    else if (db.users.Any(u => u.UserName == UM.UserName))
                    {
                        throw new Exception("Username đã tồn tại.");
                    }

                    UM.UserName = UM.UserName;
                    UM.Avatar = Request.Unvalidated["textarea_Avatar"];
                    UM.UserId = CommonFunc.RandomNumber(DateTime.Now.ToString("yyMMddHHmmss"), rd);
                    UM.CreateBy = User.Identity.Name;
                    UM.CreateAt = DateTime.Now;
                    UM.Role = GetRoleName(UM.RoleLevel);

                    db.users.Add(UM);
                    db.SaveChanges();
                    TempData["success"] = "Thêm thành công";
                    return RedirectToAction("index");
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                TempData["Fullname"] = UM.Fullname;
                TempData["Email"] = UM.Email;
                TempData["RoleLevel"] = UM.RoleLevel;
                TempData["UserName"] = UM.UserName;
                if (!string.IsNullOrEmpty(UM.UserId))
                {
                    return RedirectToAction("save", new { id = UM.UserId });
                }
                else
                {
                    TempData["Password"] = UM.Password;
                    TempData["RetypePass"] = retypePass;
                    return RedirectToAction("save");
                }
            }
        }

        public string GetRoleName(int? roleLevel)
        {
            foreach (var item in Enum.GetValues(typeof(UserContent.UserRole)).Cast<UserContent.UserRole>())
            {
                if (roleLevel == (int)item)
                {
                    return item.ToString();
                }
            }
            return string.Empty;
        }

        public ActionResult Delete(string id)
        {
            try
            {
                if (!access.ContainsKey("update_member"))
                {
                    TempData["error"] = "Bạn không có quyền truy cập. Vui lòng liên hệ Admin.";
                    return RedirectToAction("index", "home");
                }

                var curMem = Authority.GetThisUser(false);
                var user = db.users.Find(id);
                if (user != null)
                {
                    if (curMem.RoleLevel == (int)UserContent.UserRole.Admin || curMem.RoleLevel > user.RoleLevel)
                    {
                        db.users.Remove(user);
                        db.SaveChanges();
                        TempData["success"] = "Xóa thành công";
                    }
                    else
                    {
                        throw new Exception("Bạn không có quyền xóa thành viên này");
                    }
                }
                else
                {
                    throw new Exception("Thành viên không tồn tại");
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("index");
        }
        #endregion

        #region PERMISSION MANAGEMENT
        public ActionResult PermissionManagement()
        {
            var curMem = Authority.GetThisUser(false);
            if (curMem.RoleLevel != (int)UserContent.UserRole.Admin)
            {
                return RedirectToAction("index", "home");
            }
            ViewBag.permissons = db.users_permissions.Where(p => p.Status).OrderBy(p => p.Order).ToList();
            return View(db.users_permissions_granted.ToList());
        }

        [HttpPost]
        public ActionResult PermissionSave(List<string> role)
        {
            using (var trans = db.Database.BeginTransaction())
            {
                try
                {
                    var curMem = Authority.GetThisUser(false);
                    if (curMem.RoleLevel != (int)UserContent.UserRole.Admin)
                    {
                        return RedirectToAction("index", "home");
                    }
                    var saveBD = false;
                    role = role ?? new List<string>();
                    foreach (var item in Enum.GetValues(typeof(UserContent.UserRole)).Cast<UserContent.UserRole>())
                    {
                        if ((int)item != 100)
                        {
                            foreach (var per in db.users_permissions.Where(p => p.Status).ToList())
                            {
                                var grant = db.users_permissions_granted.Where(x => x.RoleLevel == (int)item && x.PermissionCode == per.Code).FirstOrDefault();
                                if (grant != null)
                                {
                                    grant.Active = role.Contains(grant.RoleLevel + per.Code) ? true : false;
                                    db.Entry(grant).State = System.Data.Entity.EntityState.Modified;
                                }
                                else
                                {
                                    var newGrant = new users_permissions_granted
                                    {
                                        UserRole = item.ToString(),
                                        RoleLevel = (int)item,
                                        PermissionCode = per.Code,
                                        PermissionName = per.Name,
                                    };
                                    newGrant.Active = role.Contains(newGrant.RoleLevel + per.Code) ? true : false;
                                    db.users_permissions_granted.Add(newGrant);
                                }
                                saveBD = true;
                            }
                        }
                    }

                    if (saveBD)
                    {
                        db.SaveChanges();
                        trans.Commit();
                    }
                    TempData["success"] = "Cập nhật thành công";
                }
                catch (Exception e)
                {
                    trans.Dispose();
                    TempData["error"] = e.Message;
                }
                return RedirectToAction("PermissionManagement");
            }
        }
        #endregion

        #region MEMBER LOG
        public ActionResult Logs()
        {
            DateTime from = DateTime.Now.AddMonths(-6);
            ViewBag.From = new DateTime(from.Year, from.Month, 1).ToString("yyyy-MM-dd");
            ViewBag.To = DateTime.Now.ToString("yyyy-MM-dd");
            ViewBag.CurrentRole = Authority.GetThisUser(false)?.RoleLevel;
            return View();
        }

        public ActionResult LoadLogs(int Draw, int Start, int Length, DateTime? From, DateTime To, string SearchText)
        {
            try
            {
                int totalRecord = 0;
                From = DateTime.Parse($"{From:dd/MM/yyyy} 0:0:0");
                To = DateTime.Parse($"{To:dd/MM/yyyy} 23:59:59");
                SearchText = !string.IsNullOrEmpty(SearchText) ? CommonFunc.ConvertNonUnicodeURL(SearchText).Trim() : string.Empty;

                IQueryable<user_actions_log> logs;
                var curMem = Authority.GetThisUser(false);
                if (curMem.RoleLevel == (int)UserContent.UserRole.Admin)
                {
                    logs = from l in db.user_actions_log
                           where l.Action == "author"
                           && (SearchText == string.Empty || l.UserName.Contains(SearchText))
                           && From <= l.Time && l.Time <= To
                           select l;
                }
                else
                {
                    logs = from l in db.user_actions_log
                           where l.UserId == curMem.UserId && l.Action == "author"
                           && (SearchText == string.Empty || l.UserName.Contains(SearchText))
                           && From <= l.Time && l.Time <= To
                           select l;
                }

                totalRecord = logs.Count();
                logs = logs.OrderByDescending(x => x.Time).Skip(Start).Take(Length);
                var viewData = logs.ToList().Select(x => new
                {
                    username = x.UserName,
                    status = x.Description,
                    time = x.Time != null ? x.Time.Value.ToString("dd/MM/yyyy HH:mm") : string.Empty,
                });

                return Json(new
                {
                    recordsTotal = totalRecord,
                    recordsFiltered = totalRecord,
                    draw = Draw,
                    data = viewData,
                });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("index", "home");
            }
        }
        #endregion

        #region MEMBER PASSWORD - Loc Inactive 05/03/2021
        public ActionResult ResetPassword(string id)
        {
            if (!access.ContainsKey("update_member"))
            {
                return Redirect("/home");
            }
            var user = db.users.Find(id);
            string newpass = DateTime.Now.ToString("hhmmss");
            user.Password = newpass;
            db.Entry(user).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            // send email
            if (string.IsNullOrWhiteSpace(user.Email) == false)
            {
                string body = @"Mật khẩu quản trị của bạn trên <b>" + Request.Url.Authority + "</b> đã được RESET bởi quản trị viên  " + User.Identity.Name +
                    " vào lúc " + DateTime.Now.ToString("dd/MM/yyyy hh:mm tt") + ".<br/>" +
                    "Thông tin đăng nhập:<br/>" +
                    "Username:<b>" + user.UserName + "</b><br/>" +
                    "Password:<b>" + user.Password + "</b>";
                var result = SendEmail.SendEmailNotice_Vi(body, user.Email, user.Fullname, "Thông báo mật khẩu mới");
                if (string.IsNullOrWhiteSpace(result))
                {
                    TempData["success"] = "New password đã được gửi đến email của user này. New Password: <b style='color:black'>" + newpass + "</b>";
                }
                else
                {
                    TempData["error"] = "Không thể gửi thông tin PASSWORD đến email của user này. New Password: <b style='color:black'>" + newpass + "</b>";
                }
            }
            else
            {
                TempData["error"] = "Không thể gửi thông tin PASSWORD đến email của user này. New Password: <b style='color:black'>" + newpass + "</b>";
            }
            return RedirectToAction("save", new { id });
        }

        [AllowAnonymous]
        public ActionResult ChangePass() => View();

        [AllowAnonymous]
        [HttpPost]
        [ActionName("ChangePass")]
        public ActionResult ChangePassSubmit()
        {
            string curpass = Request["CurrentPass"];
            string newpass = Request["NewPass"];
            string retypepass = Request["RetypePass"];
            try
            {
                if (string.IsNullOrWhiteSpace(newpass))
                {
                    throw new Exception("Password mới không thể trống");
                }

                if (newpass.Equals(retypepass) == false)
                {
                    throw new Exception("Password xác nhận không đúng");
                }

                var user = db.users.Where(u => u.UserName.Equals(User.Identity.Name)).FirstOrDefault();
                if (user.Password.Equals(curpass) == false)
                {
                    throw new Exception("Password không đúng");
                }

                user.Password = newpass;
                db.Entry(user).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                TempData["success"] = "Mật khẩu đã được thay đổi";
                return RedirectToAction("profile");
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
                return View();
            }
        }
        #endregion

        #region MEMBER PROFILE - Loc Inactive 05/03/2021
        [AllowAnonymous]
        public new ActionResult Profile()
        {
            var user = db.users.Where(u => u.UserName.Equals(User.Identity.Name)).FirstOrDefault();
            return View(user);
        }

        [AllowAnonymous]
        public ActionResult UpdateProfile(user um)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(um.Email))
                {
                    throw new Exception("Email không thể trống");
                }

                if (string.IsNullOrWhiteSpace(um.UserName))
                {
                    throw new Exception("Username không thể trống");
                }

                if (db.users.Any(u => u.UserName.Equals(um.UserName) && u.UserId != um.UserId))
                {
                    throw new Exception("Username này đã được sử dụng");
                }

                var user = db.users.Find(um.UserId);
                user.Fullname = um.Fullname;
                user.Email = um.Email;
                user.UserName = um.UserName.Replace(" ", string.Empty);
                db.Entry(user).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                TempData["success"] = "Cập nhật thành công";
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }

            return RedirectToAction("profile");
        }
        #endregion
    }
}