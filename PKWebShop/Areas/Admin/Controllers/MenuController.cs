using PKWebShop.Utils;

namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using PKWebShop.AppLB;
    using PKWebShop.Models;
    using PKWebShop.Models.CustomizeModels;

    public class MenuController : Controller
    {
        private WebShopEntities _db = new ();
        private Dictionary<string, bool> access = Authority.GetAccessAuthority();

        // GET: Admin/Menu
        public ActionResult Index(string lang)
        {
            using (var db = new DBLangCustom())
            {
                db.setLanguage(lang);

                var model = (from menu in db.menus
                             where string.IsNullOrEmpty(menu.ParentMenuId)
                             join m in from s_menu in db.menus where !string.IsNullOrEmpty(s_menu.ParentMenuId) group s_menu by s_menu.ParentMenuId into g_sub select g_sub
                                       on menu.Id equals m.Key into gs
                             from child in gs.DefaultIfEmpty()
                             select new MenuView
                             {
                                 menu = menu,
                                 child = child.OrderBy(o => o.Order).ToList(),
                             }).OrderBy(o => o.menu.Order).ToList();
                return View(model);
            }
        }

        public JsonResult save(menu m)
        {
            try
            {
                if (!access.ContainsKey("update_menu"))
                {
                    throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin.");
                }

                if (string.IsNullOrEmpty(m.Id))
                {
                    var new_menu = new menu
                    {
                        Id = AppFunc.NewShortId(),
                        Name = m.Name,
                        Order = (_db.menus.Max(a => a.Order) ?? 0) + 1,
                        ParentMenuId = m.ParentMenuId,
                        ParentMenuName = _db.menus.Find(m.ParentMenuName)?.Name,
                        URL = m.URL,
                        LangCode = PKWebShop.Services.SiteLang.GetLang(m.LangCode).Code,
                    };
                    _db.menus.Add(new_menu);
                    _db.SaveChanges();
                    return Json(new object[] { true, "Đã thêm menu mới", new_menu });
                }
                else
                {
                    var edit_menu = _db.menus.Find(m.Id);
                    if (edit_menu == null)
                    {
                        throw new Exception("Không tìm thấy menu");
                    }

                    edit_menu.Name = m.Name;
                    edit_menu.URL = m.URL;
                    _db.Entry(edit_menu).State = System.Data.Entity.EntityState.Modified;
                    _db.SaveChanges();
                    return Json(new object[] { true, "Đã lưu thay đổi", edit_menu });
                }
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }

        public JsonResult getMenu(string id)
        {
            try
            {
                var m = _db.menus.Find(id);
                if (m == null)
                {
                    throw new Exception("Không tìm thấy Id" + id);
                }

                return Json(new object[] { true, m });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }

        public JsonResult DeleteMenu(string id)
        {
            try
            {
                if (!access.ContainsKey("update_menu"))
                {
                    throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin.");
                }

                var me = _db.menus.Find(id);
                if (me == null)
                {
                    throw new Exception("Không tìm thấy Id" + id);
                }

                var c_m = _db.menus.Where(m => m.ParentMenuId == me.Id).AsQueryable();
                _db.menus.RemoveRange(c_m);
                _db.menus.Remove(me);
                _db.SaveChanges();
                return Json(new object[] { true, "Đã xóa menu " + me.Name });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }

        public JsonResult SaveSort(List<string> list_id)
        {
            try
            {
                if (!access.ContainsKey("update_menu"))
                {
                    throw new Exception("Bạn không có quyền thay đổi. Vui lòng liên hệ Admin.");
                }

                var menus = _db.menus.ToList();
                menu parent = null;
                for (int i = 0; i < list_id.Count; i++)
                {
                    var menu = menus.Find(m => m.Id == list_id[i]);
                    if (string.IsNullOrEmpty(menu.ParentMenuId))
                    {
                        parent = menu;
                    }
                    else
                    {
                        menu.ParentMenuId = parent.Id;
                        menu.ParentMenuName = parent.Name;
                    }
                    if (menu == null)
                    {
                        throw new Exception("Không tìm thấy Id" + list_id[i]);
                    }

                    menu.Order = i;
                    _db.Entry(menu).State = System.Data.Entity.EntityState.Modified;
                }

                _db.SaveChanges();
                return Json(new object[] { true, "Đã lưu vị trí" });
            }
            catch (Exception e)
            {
                return Json(new object[] { false, e.Message });
            }
        }

        public JsonResult ShowCategory(string id, bool? show_cate, bool? hide_menu)
        {
            try
            {
                var menu = _db.menus.FirstOrDefault(x => x.Id == id);
                if (menu == null)
                {
                    throw new Exception("Không tìm thấy menu");
                }
                menu.ShowCategory = show_cate ?? false;
                menu.Hidden = hide_menu ?? false;
                _db.Entry(menu).State = System.Data.Entity.EntityState.Modified;
                _db.SaveChanges();
                return Json(new object[] { true, "Lưu thay đổi thành công" });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }
    }
}