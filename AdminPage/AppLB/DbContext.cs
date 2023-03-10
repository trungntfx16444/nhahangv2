using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Newtonsoft.Json;
using AdminPage.AppLB;
using AdminPage.Models;

public class MyContext : DbContext
{
    public MyContext(string nameOrConnectionString)
        : base(nameOrConnectionString)
    {
    }

    public override int SaveChanges()
    {
        var changeSet = ChangeTracker.Entries();
        user user =Authority.GetThisUser();
        if (user != null && changeSet.Count() > 0)
        {
            foreach (var entry in changeSet.Where(c => c.State != EntityState.Unchanged && c.Entity.GetType() != typeof(user_actions_log) && c.Entity.GetType() != typeof(viewpagetracker)))
            {
                var @new = entry.State != EntityState.Deleted ? entry.CurrentValues.ToObject() : null;
                var old = entry.State != EntityState.Added ? entry.OriginalValues.ToObject() : null;
                List<Variance> rt = entry.State == EntityState.Modified ? old.DetailedCompare(@new) : new List<Variance>();
                using (var db = new AdminEntities())
                {
                    db.user_actions_log.Add(new user_actions_log
                    {
                        Action = entry.State.ToString(),
                        EntityType = entry.Entity.GetType().ToString(),
                        EntityValue = JsonConvert.SerializeObject(@new ?? old),
                        Time = DateTime.UtcNow,
                        UserId = user.UserId,
                        UserName = user.UserName,
                        ChangedDetails = JsonConvert.SerializeObject(rt),
                    });
                    db.SaveChanges();
                }
            }
        }
        return base.SaveChanges();
    }
}
