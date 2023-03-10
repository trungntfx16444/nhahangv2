using System.Threading;

namespace AdminPage.Utils
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Hosting;
    using System.Web.Mvc;
    using Newtonsoft.Json;
    using Models;
    using System.Security.Cryptography;
    using System.Text;

    public class AppFunc
    {
        /// <summary>
        /// StringDisplay.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static string StringDisplay(object filter)
        {
            return JsonConvert.SerializeObject(filter, Formatting.None, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-dd",
            });
        }

        /// <summary>
        /// New Short Id.
        /// </summary>
        /// <returns></returns>
        public static string NewShortId(DateTime? date = null)
        {
            Thread.Sleep(1);
            var ticks = (date ?? new DateTime(2021, 1, 1)).Ticks;
            var ans = DateTime.UtcNow.Ticks - ticks;
            var uniqueId = ans.ToString("x");
            return uniqueId;
        }

        public static string RenderViewToString(ControllerContext context, string viewPath, object model = null, bool partial = false)
        {
            // first find the ViewEngine for this view
            ViewEngineResult viewEngineResult = null;
            if (partial)
            {
                viewEngineResult = ViewEngines.Engines.FindPartialView(context, viewPath);
            }
            else
            {
                viewEngineResult = ViewEngines.Engines.FindView(context, viewPath, null);
            }

            if (viewEngineResult == null)
            {
                throw new FileNotFoundException("View cannot be found.");
            }

            // get the view and attach the model to view data
            var view = viewEngineResult.View;
            context.Controller.ViewData.Model = model;

            string result = null;

            using (var sw = new StringWriter())
            {
                var ctx = new ViewContext(context, view, context.Controller.ViewData, context.Controller.TempData, sw);
                view.Render(ctx, sw);
                result = sw.ToString();
            }

            return result;
        }

        public static string YoutubeEmbed(string link = "")
        {
            if (string.IsNullOrEmpty(link) || link.Contains("youtu") == false)
            {
                return string.Empty;
            }

            if (link.Trim().StartsWith("<iframe"))
            {
                var reg = new Regex("\\ssrc=(\\S*)\\s");
                if (reg.IsMatch(link))
                {
                    link = reg.Split(link)[1];
                }

                link = link.Replace("'", string.Empty).Replace("\"", string.Empty).Replace("src=", string.Empty).Trim();
            }
            var id = link.Split('/').Last();
            if (id.Contains("watch"))
            {
                id = id.Split('&').First().Split('=').Last();
            }
            return $"https://www.youtube.com/embed/{id}";
        }

        public static decimal CountDiskSizeUsing()
        {
            var dbSize = DatabaseSize();
            var uploadDirSize = UploadDirSize();
            return Math.Round(dbSize + uploadDirSize + 250, 2);
        }

        private static decimal DatabaseSize()
        {
            string sizeUsing = "0";
            using (var ctx = new AdminEntities())
            {
                var query =
                "SELECT SUM(data_length + index_length) / 1024 / 1024 AS Size " +
                "FROM information_schema.TABLES " +
                $"WHERE table_schema = '{ctx.Database.Connection.Database}' " +
                "GROUP BY table_schema";
                sizeUsing = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
            }
            return decimal.Parse(sizeUsing ?? "0");
        }

        private static decimal UploadDirSize()
        {
            var sizeUsing = FindFolderSize(new DirectoryInfo(HttpContext.Current.Server.MapPath("~/Upload")));
            return (decimal)sizeUsing;
        }

        private static double FindFolderSize(DirectoryInfo d, int u = 2, int r = 2)
        {
            double divider = Math.Pow(1024, 2);
            double size = 0;
            foreach (FileInfo f in d.GetFiles())
            {
                size += Convert.ToDouble(f.Length) / divider;
            }

            foreach (DirectoryInfo c in d.GetDirectories())
            {
                size += FindFolderSize(c);
            }

            return Math.Round(size, r);
        }
        
        
        public static string Md5(string sInput)
        {
            HashAlgorithm algorithmType = default(HashAlgorithm);
            ASCIIEncoding enCoder = new ASCIIEncoding();
            byte[] valueByteArr = enCoder.GetBytes(sInput);
            byte[] hashArray = null;
            // Encrypt Input string 
            algorithmType = new MD5CryptoServiceProvider();
            hashArray = algorithmType.ComputeHash(valueByteArr);
            //Convert byte hash to HEX
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashArray)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }
        public static string Sha256(string data)
        {
            using (var sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(data));

                // Convert byte array to a string   
                var builder = new StringBuilder();
                foreach (var t in bytes)
                {
                    builder.Append(t.ToString("x2"));
                }
                return builder.ToString();
            }
        }
        public static string GetIpAddress()
        {
            string ipAddress;
            try
            {
                ipAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                if (string.IsNullOrEmpty(ipAddress) || (ipAddress.ToLower() == "unknown"))
                    ipAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            }
            catch (Exception ex)
            {
                ipAddress = "Invalid IP:" + ex.Message;
            }

            return ipAddress;
        }
    }
}