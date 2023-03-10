namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Web.Mvc;
    using Newtonsoft.Json;
    using PKWebShop.AppLB;
    using PKWebShop.Areas.Admin.CustomizeModel;
    using PKWebShop.Areas.Admin.Services;
    using PKWebShop.Models;
    using PKWebShop.Services;
    using PKWebShop.Utils;

    [Authorize]
    public class MenuMgrController : ExpiredCheckController
    {
        WebShopEntities _db = new WebShopEntities();
        const string session_price = "session_product_price";
        private Dictionary<string, bool> access = Authority.GetAccessAuthority();
        private SiteLang.Language default_language = SiteLang.GetDefault();
        // GET: Admin/MenuMgr
        #region menuGORY UPGRADE - LOC UPDATE 22/07/2021
        public ActionResult Index(string langCode)
        {
            try
            {
                var listLanguage = SiteLang.GetListLangs();
                langCode = string.IsNullOrEmpty(langCode) ? listLanguage.FirstOrDefault()?.Code : langCode;
                var listmenuByLang = _db.menus.Where(x => x.LangCode == langCode).OrderBy(x => x.Order).ToList();
                ViewBag.LangCode = langCode;
                ViewBag.ListLang = listLanguage;
                return View(listmenuByLang);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("index", "home");
            }
        }

        public JsonResult LoadmenuParent(string langCode)
        {
            try
            {
                var listLanguage = SiteLang.GetListLangs();
                if (string.IsNullOrEmpty(langCode))
                {
                    langCode = listLanguage.FirstOrDefault()?.Code;
                }
                var listmenu1 = _db.menus.Where(x => string.IsNullOrEmpty(x.ParentMenuId) && x.LangCode == langCode).OrderBy(x => x.Order).Select(x => new { x.Id, x.Name, x.ParentMenuId }).ToList();
                var list_menu_lv1 = listmenu1.Select(m => m.Id).ToList();
                var listmenu2 = _db.menus.Where(x => list_menu_lv1.Contains(x.ParentMenuId) && x.LangCode == langCode).OrderBy(x => x.Order).Select(x => new { x.Id, x.Name, x.ParentMenuId}).ToList();
                return Json(new object[] { true, listmenu1, listmenu2 });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        public JsonResult GetmenuDetail(string reId, string langCode)
        {
            try
            {
                if (string.IsNullOrEmpty(reId))
                {
                    throw new Exception("Không tìm thấy danh mục này");
                }

                langCode = string.IsNullOrEmpty(langCode) ? SiteLang.GetDefault()?.Code : langCode;
                var menu = _db.menus.FirstOrDefault(x => x.Id == reId && x.LangCode == langCode);
                if (menu == null)
                {
                    throw new Exception("Không tìm thấy danh mục này");
                }

                return Json(new object[] { true, menu });
            }
            catch (Exception ex)
            {
                return Json(new object[] { false, ex.Message });
            }
        }

        Random R = new();
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Savemenu(menu data)
        {
            using (var trans = _db.Database.BeginTransaction())
            {
                try
                {
                    if (!access.ContainsKey("update_product"))
                    {
                        throw new Exception("Bạn không có quyền truy cập. Vui lòng liên hệ Admin");
                    }

                    data.LangCode = string.IsNullOrEmpty(data.LangCode) ? SiteLang.GetDefault().Code : data.LangCode;
                    if (string.IsNullOrEmpty(data.Id))
                    {
                        #region ADD NEW
                        string new_reId = AppFunc.NewShortId();
                        var menu = new menu
                        {
                            Id = AppFunc.NewShortId(),
                            Name = data.Name,
                            ParentMenuId = data.ParentMenuId,
                            URL = data.URL,
                            Order = data.Order < 0 ? ((_db.menus?.Max(x => x.Order) ?? 0) + 1) : data.Order,
                            Hidden = data.Hidden ?? false,
                            LangCode = data.LangCode,

                        };

                        if (!string.IsNullOrEmpty(data.ParentMenuId))
                        {
                            var menuParent = _db.menus.FirstOrDefault(x => x.Id == data.ParentMenuId);
                            if (menuParent != null)
                            {
                                menu.ParentMenuId = menuParent.Id;
                                menu.ParentMenuName = menuParent.Name;
                            }
                        }

                        _db.menus.Add(menu);

                        #endregion
                        TempData["success"] = "Thêm danh mục thành công";
                    }
                    else
                    {
                        #region UPDATE
                        var menu = _db.menus.FirstOrDefault(x => x.Id == data.Id);
                        if (menu == null)
                        {
                            throw new Exception("Không tìm thấy danh mục");
                        }

                        menu.Name = data.Name;
                        menu.Order = data.Order;
                        menu.URL = data.URL;
                        menu.Hidden = data.Hidden ?? false;
                        menu.ParentMenuId = null;
                        menu.ParentMenuName = null;

                        if (!string.IsNullOrEmpty(data.ParentMenuId))
                        {
                            var menuParent = _db.menus.FirstOrDefault(x => x.Id == data.ParentMenuId);
                            if (menuParent != null)
                            {
                                menu.ParentMenuId = menuParent.Id;
                                menu.ParentMenuName = menuParent.Name;
                            }
                        }
                        _db.Entry(menu).State = System.Data.Entity.EntityState.Modified;
                        #endregion
                        TempData["success"] = "Cập nhật danh mục thành công";
                    }
                    _db.SaveChanges();
                    trans.Commit();

                    // cap nhat lai session
                    UserContent.GetWebMenu(true);
                }
                catch (Exception ex)
                {
                    trans.Dispose();
                    TempData["error"] = ex.Message;
                }
                return RedirectToAction("Index");
            }
        }

        public JsonResult Deletemenu(string reId, string langCode)
        {
            using (var tran = _db.Database.BeginTransaction())
            {
                try
                {
                    var menuDel = _db.menus.Where(x => x.Id == reId).ToList();
                    if (menuDel == null || menuDel.Count == 0)
                    {
                        throw new Exception("Danh mục sản phẩm không tồn tại");
                    }

                    //if (_db.menugories.Any(x => x.ParentId == menuDel.Id))
                    //{
                    //    throw new Exception("Vui lòng xóa các danh mục con trước khi xóa danh mục cha");
                    //}

                    var listmenuDel = new List<menu>();
                    foreach (var item in menuDel)
                    {
                        listmenuDel.Add(item);
                        var childs = _db.menus.Where(x => x.ParentMenuId == item.Id).ToList();
                        if (childs != null && childs.Count > 0)
                        {
                            foreach (var child in childs)
                            {
                                listmenuDel.Add(child);
                                var last_childs = _db.menus.Where(x => x.ParentMenuId == child.Id).ToList();
                                if (last_childs != null && last_childs.Count > 0)
                                {
                                    listmenuDel.AddRange(last_childs);
                                }
                            }
                        }
                    }

                    var listmenuDel_ReId = listmenuDel.Select(c => c.Id);
                    foreach (var item in _db.products.Where(x => listmenuDel_ReId.Contains(x.Id)).ToList())
                    {
                        item.CategoryId = string.Empty;
                        item.CategoryName = string.Empty;
                        _db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                    }

                    _db.menus.RemoveRange(listmenuDel);
                    _db.SaveChanges();
                    tran.Commit();

                    // cap nhat lai session
                    UserContent.GetWebMenu(true);
                    TempData["success"] = "Xóa thành công";
                    return Json(new object[] { true, "Xóa thành công" });
                }
                catch (Exception ex)
                {
                    tran.Dispose();
                    return Json(new object[] { false, ex.Message });
                }
            }
        }

        public JsonResult SaveSort(List<string> list_id)
        {
            using (var trans = _db.Database.BeginTransaction())
            {
                try
                {
                    if (!access.ContainsKey("update_product"))
                    {
                        throw new Exception("Bạn không có quyền thay đổi. Vui lòng liên hệ Admin.");
                    }

                    var menus = _db.menus.ToList();
                    menu menu_parentLv1 = null;
                    menu menu_parentLv2 = null;

                    foreach (var item in SiteLang.GetListLangs())
                    {
                        for (int i = 0; i < list_id.Count; i++)
                        {
                            var menu = menus.FirstOrDefault(x => x.Id == list_id[i].Split('|')[1] && x.LangCode == item.Code);
                            if (menu == null)
                            {
                                throw new Exception("Không tìm thấy Id" + list_id[i].Split('|')[1]);
                            }

                            if (list_id[i].Contains("1|"))
                            {
                                menu.ParentMenuId = null;
                                menu.ParentMenuName = null;
                                //menu.Level = 1;
                                menu_parentLv1 = menu;
                            }
                            else if (list_id[i].Contains("2|"))
                            {
                                menu.ParentMenuId = menu_parentLv1.Id;
                                menu.ParentMenuName = menu_parentLv1.Name;
                                //menu.Level = menu_parentLv1.Level + 1;
                                menu_parentLv2 = menu;
                            }
                            else if (list_id[i].Contains("3|"))
                            {
                                menu.ParentMenuId = menu_parentLv2.Id;
                                menu.ParentMenuName = menu_parentLv2.Name;
                                //menu.Level = menu_parentLv2.Level + 1;
                            }
                            menu.Order = i;
                            _db.Entry(menu).State = System.Data.Entity.EntityState.Modified;
                        }
                    }
                    _db.SaveChanges();
                    trans.Commit();
                    TempData["success"] = "Lưu vị trí thành công";
                    return Json(new object[] { true, "Lưu vị trí thành công" });
                }
                catch (Exception e)
                {
                    trans.Dispose();
                    return Json(new object[] { false, e.Message });
                }
            }
        }
        #endregion
    }
}