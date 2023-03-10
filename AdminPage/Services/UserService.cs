namespace AdminPage.Services
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Inner.Libs.Helpful;
    using AdminPage.AppLB;
    using AdminPage.Models;
    using AdminPage.Utils;

    public class UserService : ServicesBase
    {
        public void LastLogin(user user)
        {
            user.LastLogin = DateTime.Now;
            DB.Entry(user).State = EntityState.Modified;
            DB.SaveChanges();
        }

        public user VerifyUserLogin(string email, string password, bool isAdmin)
        {
            var user = DB.users.FirstOrDefault(u => u.Email.Equals(email) && u.Password.Equals(password));
            if (user == null || user.Active == false)
            {
                return null;
            }

            if (isAdmin && user.Role == UserContent.UserRole.Member.Text())
            {
                return null;
            }

            return user;
        }

        public user FindById(string userId)
        {
            return DB.users.FirstOrDefault(m => m.UserId == userId && m.Active == true);
        }

        public user Register(user ruser)
        {
            try
            {
                var user1 = DB.users.FirstOrDefault(user => user.Email == ruser.Email);
                if (user1 != null && user1.Active == false)
                {
                    return null;
                }
                if (user1 == null)
                {
                    user1 = ruser;
                    user1.UserId = AppFunc.NewShortId();
                    user1.Active = true;
                    user1.Role = UserContent.UserRole.Member.Text();
                    user1.CreateAt = DateTime.Now;
                    user1.CreateBy = ruser.AccountType;
                }
                user1.Avatar = ruser.Avatar;
                user1.Email = ruser.Email;
                user1.Fullname = ruser.Fullname ?? ruser.UserName;
                user1.UserName = ruser.UserName ?? ruser.Fullname;
                user1.AccountType = ruser.AccountType;
                user1.LastLogin = DateTime.Now;
                DB.users.AddOrUpdate(user1);
                DB.SaveChanges();
                return user1;
            }
            catch (Exception e)
            {
                if (e is AppHandleException)
                {
                    throw new AppHandleException(e.Message);
                }
                throw new AppHandleException(e.Message);
            }
        }
    }
}