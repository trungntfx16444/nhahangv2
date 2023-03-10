namespace PKWebShop.Areas.Admin.Controllers
{
    using System;
    using System.Data.Entity;
    using System.IO;
    using System.Linq;

    // using System.Linq.Dynamic;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.Mvc;
    using Newtonsoft.Json;
    using PKWebShop.Models;
    using PKWebShop.Utils;

    public class UploadController : ExpiredCheckController
    {
        /// <summary>
        /// uploads, tra ve duong dan tuong doi cua file.
        /// </summary>
        /// <param name="strPath">folder luu tru tren server.</param>
        /// <param name="inputFileName">input file name.</param>
        /// <param name="prefix">prefix.</param>
        /// <param name="fileName">Output duong dan tuong doi tap tin.</param>
        public void UploadAttachFile(string strPath, string inputFileName, string prefix, out string fileName)
        {
            fileName = string.Empty;
            try
            {
                #region check path

                DirectoryInfo d = new (Server.MapPath(strPath));
                if (!d.Exists)
                {
                    d.Create();
                }

                #endregion

                HttpPostedFileBase file = HttpContext.Request.Files[inputFileName];
                if (string.IsNullOrWhiteSpace(file?.FileName) == false)
                {
                    string pathFileName = Regex.Replace(file.FileName, "[ ?#$&(){}~!]", string.Empty);
                    string strFileName;
                    if (!string.IsNullOrWhiteSpace(prefix))
                    {
                        strFileName = prefix + "_" + Path.GetFileName(pathFileName);
                    }
                    else
                    {
                        strFileName = Path.GetFileName(pathFileName);
                    }
                    string fullPath = Path.Combine(Server.MapPath(strPath), strFileName);
                    file.SaveAs(fullPath);

                    fileName = Path.Combine(strPath, strFileName);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public FileResult DownloadFile(string id)
        {
            WebShopEntities db = new ();
            uploadmorefile morefile = db.uploadmorefiles.Find(id);

            string sPath = Path.Combine(Server.MapPath(morefile.FileName));
            return File(sPath, "application/octet-stream", morefile.FileName.Substring(morefile.FileName.LastIndexOf("/") + 1));
        }

        public JsonResult MoreFileDelete(string id, string sPath)
        {
            try
            {
                WebShopEntities db = new ();
                uploadmorefile morefile = db.uploadmorefiles.Find(id);
                sPath = Path.Combine(Server.MapPath(sPath), morefile.FileName);

                FileInfo f = new (sPath);
                if (f.Exists)
                {
                    f.Delete();
                }

                db.Entry(morefile).State = EntityState.Deleted;
                db.SaveChanges();
                return Json(true);
            }
            catch (Exception)
            {
                return Json(false);
            }
        }

        #region upload multiple files

        /// <summary>
        /// upload [morefiles], save vao table UploadMoreFiles.
        /// </summary>
        /// <param name="tableid"></param>
        /// <param name="tablename"></param>
        /// <param name="filestotal"></param>
        /// <param name="strPath"></param>
        /// <param name="prefix"></param>
        protected void UploadMoreFiles(string tableid, string tablename, int filestotal, string strPath = "/Upload/Other", string prefix = "")
        {
            try
            {
                WebShopEntities db = new ();
                for (int i = 1; i <= filestotal; i++)
                {
                    string f = "morefiles_" + i;
                    HttpPostedFileBase file = HttpContext.Request.Files[f];

                    if (file != null && file.FileName != string.Empty)
                    {
                        if (file.ContentLength > 20971520)
                        {
                            throw new Exception("File upload content length larger than allowed is 20 MB");
                        }

                        string fileName = Regex.Replace(file.FileName, "[ ,?#$&(){}~!]", string.Empty);
                        fileName = Path.GetFileName(fileName);
                        if (!string.IsNullOrWhiteSpace(prefix))
                        {
                            fileName = prefix + "_" + Path.GetFileName(fileName);
                        }
                        var directory = new DirectoryInfo(Server.MapPath(strPath));
                        if (!directory.Exists)
                        {
                            directory.Create();
                        }
                        string fullPath = Path.Combine(directory.FullName, fileName);
                        file.SaveAs(fullPath);

                        uploadmorefile up = new ()
                        {
                            UploadId = AppFunc.NewShortId(),
                            FileName = Path.Combine(strPath, fileName),
                            TableId = tableid,
                            TableName = tablename,
                        };
                        db.uploadmorefiles.Add(up);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        protected uploadmorefile[] get_tablefile(string tableid, string tablename)
        {
            WebShopEntities db = new ();
            var rs = db.uploadmorefiles.Where(x => x.TableId == tableid && x.TableName == tablename).ToArray();
            return rs;
        }

        /// <summary>
        /// upload va get fullpath attach files string. khong save vao db.[morefiles].
        /// </summary>
        /// <param name="filestotal"></param>
        /// <param name="strPath"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        protected string UploadMoreFiles(int filestotal, string strPath = "/Upload/Other", string prefix = "")
        {
            try
            {
                string attachFiles = string.Empty;
                for (int i = 1; i <= filestotal; i++)
                {
                    string f = "morefiles_" + i;
                    if (string.IsNullOrWhiteSpace(prefix) == false)
                    {
                        f = prefix + "-" + f;
                    }

                    HttpPostedFileBase file = HttpContext.Request.Files[f];

                    if (file.FileName != string.Empty)
                    {
                        string fileName = Regex.Replace(file.FileName, "[ ,!?$~#&^(){}]", string.Empty);
                        fileName = Path.GetFileName(fileName);
                        string fullPath = Path.Combine(Server.MapPath(strPath), fileName);
                        file.SaveAs(fullPath);
                        attachFiles += fullPath + ";";
                    }
                }

                return attachFiles;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}
