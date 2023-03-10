namespace AdminPage.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Linq.Dynamic;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Web;
    using System.Web.Mvc;
    using Newtonsoft.Json;
    using AdminPage.Models;
    using AdminPage.Utils;

    public class testController : Controller
    {
        [Authorize]
        // GET: Admin/test
        public string Index()
        {
            try
            {

                //var db = new AdminEntities();
                //var pics = db.uploadmorefiles.Where(u => u.TableName == "products").ToList();
                //var ps = db.products.ToList();
                ////List<string> olds = new List<string>();
                //foreach (var p in pics)
                //{
                //    //olds.Add(p.FileName);
                //    //    p.FileName = converttojpg(p.FileName);
                //    //    db.Entry(p).State = System.Data.Entity.EntityState.Modified;
                //    create_mini_images(p.FileName);
                //}
                //foreach (var p in ps)
                //{
                //    //olds.Add(p.Picture);
                //    //    p.Picture = converttojpg(p.Picture);
                //    //    db.Entry(p).State = System.Data.Entity.EntityState.Modified;
                //    create_mini_images(p.Picture);
                //}
                //remove(olds);
                //-- convert to jpg
                //foreach (var p in pics)
                //{
                //    olds.Add(p.FileName);
                //    p.FileName = converttojpg(p.FileName);
                //    db.Entry(p).State = System.Data.Entity.EntityState.Modified;
                //}
                //foreach (var p in ps)
                //{
                //    olds.Add(p.Picture);
                //    p.Picture = converttojpg(p.Picture);
                //    db.Entry(p).State = System.Data.Entity.EntityState.Modified;
                //}
                //db.SaveChanges();
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            return "hoàn tất";
        }
        public bool create_mini_images(string url)
        {
            url = Uri.UnescapeDataString(url);
            string full_url = Server.MapPath(url);
            string ext = Path.GetExtension(url).ToLower();
            string name = Path.GetFileNameWithoutExtension(url);
            string path = Path.GetDirectoryName(url);
            List<(int w, int h)> sizes = new List<(int, int)> { (85, 85), (98, 98), (160, 160), (280, 280), (387, 387) };
            if (System.IO.File.Exists(full_url))
            {
                Image png = Image.FromFile(full_url);
                DirectoryInfo d = new(Server.MapPath(path + "/_resize/"));
                if (!d.Exists)
                {
                    d.Create();
                }
                foreach (var s in sizes)
                {
                    var newpath = path + $"/_resize/{name}_{s.w}x{s.h}.jpg";
                    var result = ResizeImage(png, new Size {Width = s.w, Height = s.h });
                    result.Save(Server.MapPath(newpath), ImageFormat.Jpeg);
                    result.Dispose();
                }
                png.Dispose();
                //System.IO.File.Delete(full_url);
            }
            return true;
        }

        public Image ResizeImage(Image imgToResize, Size destinationSize)
        {
            var originalWidth = imgToResize.Width;
            var originalHeight = imgToResize.Height;

            //how many units are there to make the original length
            var hRatio = (float)originalHeight / destinationSize.Height;
            var wRatio = (float)originalWidth / destinationSize.Width;

            //get the shorter side
            var ratio = Math.Min(hRatio, wRatio);

            var hScale = Convert.ToInt32(destinationSize.Height * ratio);
            var wScale = Convert.ToInt32(destinationSize.Width * ratio);

            //start cropping from the center
            var startX = (originalWidth - wScale) / 2;
            var startY = (originalHeight - hScale) / 2;

            //crop the image from the specified location and size
            var sourceRectangle = new Rectangle(startX, startY, wScale, hScale);

            //the future size of the image
            var bitmap = new Bitmap(destinationSize.Width, destinationSize.Height);

            //fill-in the whole bitmap
            var destinationRectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            //generate the new image
            using (var g = Graphics.FromImage(bitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(imgToResize, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);
            }

            return bitmap;

        }
        public string converttojpg(string url)
        {
            url = Uri.UnescapeDataString(url);
            string full_url = Server.MapPath(url);
            string ext = Path.GetExtension(url).ToLower();
            string name = Path.GetFileNameWithoutExtension(url);
            string path = Path.GetDirectoryName(url);
            var newpath = path + @"/" + name + ".jpg";
            if (ext != ".jpg" && System.IO.File.Exists(full_url))
            {
                Console.WriteLine(url);

                Image png = Image.FromFile(full_url);
                png.Save(Server.MapPath(newpath), ImageFormat.Jpeg);
                png.Dispose();
                //System.IO.File.Delete(full_url);
                return newpath;
            }
            return newpath;
        }
        public bool remove(List<string> files)
        {
            foreach (var f in files)
            {
                var _f = Uri.UnescapeDataString(f);
                System.IO.File.Delete(Server.MapPath(_f));
            }
            return true;
        }
        public JsonResult updateCategoryUrlcode()
        {
            var db = new AdminEntities();
            db.categories.ToList().ForEach(cate =>
            {
                cate.UrlCode = AppLB.CommonFunc.ConvertNonUnicodeURL(cate.CategoryName);
                db.Entry(cate).State = System.Data.Entity.EntityState.Modified;
            });
            db.SaveChanges();
            return Json(true);
        }
        public void setup_morelang()
        {
            try
            {
                var db = new AdminEntities();

                //db.n_parent_topic.Where(s => s.LangCode == "vi").ToList().ForEach(s =>
                //{
                //    var new_item = JsonConvert.DeserializeObject<n_parent_topic>(JsonConvert.SerializeObject(s));
                //    new_item.LangCode = "en";
                //    new_item.Name = FastTranslate(new_item.Name);
                //    new_item.Id = AppFunc.NewShortId();
                //    db.n_parent_topic.Add(new_item);
                //    db.n_toppic.Where(t => t.LangCode == "vi" && t.ParentTopicId == s.ReId).ToList().ForEach(f =>
                //    {
                //        var new_item2 = JsonConvert.DeserializeObject<n_toppic>(JsonConvert.SerializeObject(f));
                //        new_item2.LangCode = "en";
                //        new_item2.Name = FastTranslate(new_item2.Name);
                //        new_item2.ParentTopicName = FastTranslate(new_item2.ParentTopicName);
                //        new_item2.URL = AppLB.CommonFunc.ConvertNonUnicodeURL(new_item2.Name);
                //        new_item2.Seo_title = new_item2.Name;
                //        new_item2.TopicId = AppFunc.NewShortId();
                //        db.n_toppic.Add(new_item2);
                //    });
                //    db.SaveChanges();
                //});
                //db.n_news.Where(s => s.LangCode == "vi").ToList().ForEach(s =>
                //{
                //    var new_item = JsonConvert.DeserializeObject<n_news>(JsonConvert.SerializeObject(s));
                //    new_item.LangCode = "en";
                //    new_item.Name = new_item.TitleWebsite = FastTranslate(new_item.Name);
                //    new_item.ParentTopicName = FastTranslate(new_item.ParentTopicName);
                //    new_item.ShortDescription = new_item.SEODescription = FastTranslate(new_item.ShortDescription);
                //    new_item.NewsId = AppFunc.NewShortId();
                //    db.n_news.Add(new_item);
                //    db.SaveChanges();
                //});

            }
            catch (Exception e)
            {
                throw;
            }

        }
        internal static string FastTranslate(string content)
        {
            Thread.Sleep(1000);
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }
            var result = new Dictionary<string, List<string>>();

            var listContent = new List<string>();


            if (!string.IsNullOrEmpty(content))
            {
                listContent.Insert(0, content);
            }

            var fromLanguage = "vi";
            var toLanguage = "en";
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}";
            var webClient = new WebClient
            {
                Encoding = System.Text.Encoding.UTF8,
            };
            var response = webClient.DownloadString($"{url}&dt=t&q={HttpUtility.UrlEncode(content)}");
            response = response.Substring(4, response.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
            return response;
        }
    }
}