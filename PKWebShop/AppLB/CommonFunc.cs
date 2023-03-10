namespace PKWebShop.AppLB
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.Mvc;
    using PKWebShop.Models;

    public class CommonFunc
    {
        /// <summary>
        /// chuyen doi ngay dd/MM/yyyy sang MM/dd/yyyy.
        /// </summary>
        /// <param name="datevi">dd/MM/yyyy.</param>
        /// <returns></returns>
        internal static DateTime DateVi_To_DateEn(string datevi)
        {
            var dateViArr = datevi.Split(new char[] { '/' });
            string dateEn = string.Join("/", dateViArr[1], dateViArr[0], dateViArr[2]);
            return DateTime.Parse(dateEn);
        }

        internal static string getTopBackground()
        {
            string path = HttpContext.Current.Request.Url.AbsolutePath;
            string feature = UserContent.Web_Feature.TopBackground.ToString();
            var db = new WebShopEntities();
            var curlang = Services.SiteLang.GetCurrentLang();
            var bg = db.sectionfeaturedetails.Where(f => f.SectionCode == feature).OrderByDescending(f => f.LangCode == curlang);
            sectionfeaturedetail topbg = null;
            while (topbg == null)
            {
                topbg = bg.Where(f => f.URL == path).FirstOrDefault();
                int last = path.LastIndexOf('/');
                if (last == -1)
                {
                    break;
                }
                else
                {
                    path = path.Substring(0, last);
                }
            }
            return topbg?.Picture;
        }

        internal static string SearchCommand(string tableName, string search, string column, params string[] columns)
        {
            if (string.IsNullOrEmpty(search))
            {
                return $"SELECT * FROM `{tableName}`";
            }
            search = ConvertNonUnicodeURL(search, false).Replace("-", "%");
            string sqlcommand = $"SELECT * FROM `{tableName}` WHERE ( REPLACE(LOWER(`{column}`),'đ','d') LIKE '%{search}%'";
            foreach (var c in columns)
            {
                sqlcommand += $" or REPLACE(LOWER(`{c}`), 'đ', 'd') LIKE '%{search}%'";
            }
            sqlcommand += ")";
            return sqlcommand;
        }

        internal static string GetRandomString(int length, string RangeChars = "abcdefghijklmnopqrstuvwxyz1234567890")
        {
            Random rand = new();
            string rs = string.Empty;
            for (int i = 0; i < length; i++)
            {
                int num = rand.Next(0, RangeChars.Length - 1);
                rs += RangeChars[num];
            }
            return rs;
        }
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = new byte[0];
            try
            {
                plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            }
            catch { }
            return Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64EncodedData = "")
        {
            var base64EncodedBytes = new byte[0];
            try
            {
                base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            }
            catch { }
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        /// <summary>
        /// Ham tra ve ngay dau, cuoi thang theo ngay bat ky.
        /// </summary>
        /// <param name="currentDate">Ngay bat ky trong thang.</param>
        /// <param name="BeginDateOfMonth">Tra ve ngay dau tien cua thang.</param>
        /// <param name="EndDateOfMonth">Tra ve ngay cuoi cung cua thang.</param>
        internal static void GetBeginEndDateOfMonth(DateTime currentDate, out DateTime BeginDateOfMonth, out DateTime EndDateOfMonth)
        {
            string bDate = currentDate.Month.ToString() + "/1/" + currentDate.Year.ToString() + " 0:0:0";
            string eDate = currentDate.Month.ToString() + "/" + DateTime.DaysInMonth(currentDate.Year, currentDate.Month).ToString() + "/" + currentDate.Year.ToString() + " 23:59:59";
            DateTime.TryParse(bDate, out DateTime beginDate);
            DateTime.TryParse(eDate, out DateTime endDate);
            BeginDateOfMonth = beginDate;
            EndDateOfMonth = endDate;
        }

        internal static string RenderRazorViewToString(string viewName, object model, ControllerBase cb)
        {
            cb.ViewData.Model = model;
            using var sw = new StringWriter();
            var viewResult = ViewEngines.Engines.FindPartialView(cb.ControllerContext, viewName);
            var viewContext = new ViewContext(cb.ControllerContext, viewResult.View, cb.ViewData, cb.TempData, sw);
            viewResult.View.Render(viewContext, sw);
            viewResult.ViewEngine.ReleaseView(cb.ControllerContext, viewResult.View);
            return sw.GetStringBuilder().ToString();
        }

        /// <summary>
        /// Ham tra ve ngay dau tien(Monday) va ngay cuoi tuan(Sunday).
        /// </summary>
        /// <param name="currentDate">Ngay bat ky trong tuan.</param>
        /// <param name="beginWeek">Tra ve ngay dau tien.</param>
        /// <param name="endWeek">Tra ve ngay cuoi tuan.</param>
        internal static void GetBeginEndWeek(DateTime currentDate, out DateTime beginWeek, out DateTime endWeek)
        {
            switch (currentDate.DayOfWeek)
            {
                case DayOfWeek.Friday:
                    beginWeek = currentDate.AddDays(-4);
                    endWeek = currentDate.AddDays(2);
                    break;
                case DayOfWeek.Monday:
                    beginWeek = currentDate;
                    endWeek = currentDate.AddDays(6);
                    break;
                case DayOfWeek.Saturday:
                    beginWeek = currentDate.AddDays(-5);
                    endWeek = currentDate.AddDays(1);
                    break;
                case DayOfWeek.Sunday:
                    beginWeek = currentDate.AddDays(-6);
                    endWeek = currentDate;
                    break;
                case DayOfWeek.Thursday:
                    beginWeek = currentDate.AddDays(-3);
                    endWeek = currentDate.AddDays(3);
                    break;
                case DayOfWeek.Tuesday:
                    beginWeek = currentDate.AddDays(-1);
                    endWeek = currentDate.AddDays(5);
                    break;
                case DayOfWeek.Wednesday:
                    beginWeek = currentDate.AddDays(-2);
                    endWeek = currentDate.AddDays(4);
                    break;
                default:
                    beginWeek = currentDate;
                    endWeek = currentDate;
                    break;
            }
        }

        /// <summary>
        /// chuyen doi cac chu cai tieng viet co dau sang khong dau.
        /// </summary>
        /// <param name="inputS"></param>
        /// <param name="include_space"></param>
        /// <param name="mysqlRegexp"></param>
        /// <returns></returns>
        public static string ConvertNonUnicodeURL(string inputS, bool include_space = false, bool mysqlRegexp = false)
        {
            if (string.IsNullOrEmpty(inputS))
            {
                return inputS;
            }
            string outputS = inputS.ToLower();
            outputS = Regex.Replace(outputS, "[!?&/.,%+]", "-");
            outputS = outputS.Replace("//", "-");
            if (mysqlRegexp)
            {
                outputS = outputS.Replace(" ", ".*");
            }
            else
            {
                if (!include_space)
                {
                    outputS = outputS.Replace(" ", "-");
                }
                outputS = Regex.Replace(outputS, "-+", "-");
            }

            if (string.IsNullOrWhiteSpace(outputS) == false)
            {
                outputS = Regex.Replace(outputS, "-+", "-");
                var arrString = inputS.Split(new char[] { });
                outputS = Regex.Replace(outputS, "a|á|à|ả|ã|ạ|â|ă|ẩ|ẫ|ậ|ầ|ấ|ă|ằ|ắ|ẳ|ẵ|ặ", mysqlRegexp ? "[aáàảãạâăẩẫậầấăằắẳẵặ]" : "a");
                outputS = Regex.Replace(outputS, "o|ó|ò|ỏ|õ|ọ|ô|ố|ồ|ổ|ỗ|ộ|ơ|ớ|ờ|ở|ỡ|ợ", mysqlRegexp ? "[oóòỏõọôốồổỗộơớờởỡợ]" : "o");
                outputS = Regex.Replace(outputS, "e|é|è|ẻ|ẽ|ẹ|ê|ế|ề|ể|ễ|ệ", mysqlRegexp ? "[eéèẻẽẹêếềểễệ]" : "e");
                outputS = Regex.Replace(outputS, "u|ú|ù|ủ|ũ|ụ|ư|ứ|ừ|ử|ữ|ự", mysqlRegexp ? "[uúùủũụưứừửữự]" : "u");
                outputS = Regex.Replace(outputS, "i|í|ì|ỉ|ĩ|ị", mysqlRegexp ? "[iíìỉĩị]" : "i");
                outputS = Regex.Replace(outputS, "y|ý|ỳ|ỷ|ỹ|ỵ", mysqlRegexp ? "[yýỳỷỹỵ]" : "y");
                outputS = Regex.Replace(outputS, "d|đ", mysqlRegexp ? "[dđ]" : "d");
            }
            return outputS;
        }

        public static bool IsDebug()
        {
#if DEBUG
            return true;
#else
      return false;
#endif
        }

        internal static Dictionary<string, List<string>> Translate(string content, List<string> listContent, string fromLanguage, string toLanguage)
        {
            var result = new Dictionary<string, List<string>>();
            try
            {
                if (listContent == null)
                {
                    listContent = new List<string>();
                }

                if (!string.IsNullOrEmpty(content))
                {
                    listContent.Insert(0, content);
                }

                fromLanguage = string.IsNullOrEmpty(fromLanguage) ? "vi" : fromLanguage;
                toLanguage = string.IsNullOrEmpty(toLanguage) ? "en" : toLanguage;
                if (fromLanguage != toLanguage && listContent.Count > 0)
                {
                    var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}";
                    var webClient = new WebClient
                    {
                        Encoding = System.Text.Encoding.UTF8,
                    };

                    var response = string.Empty;
                    var listText = new List<string>();
                    foreach (var text in listContent)
                    {
                        response = webClient.DownloadString($"{url}&dt=t&q={HttpUtility.UrlEncode(text)}");
                        response = response.Substring(4, response.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
                        listText.Add(response);
                    }
                    result.Add(string.Empty, listText);
                }
                else
                {
                    result.Add(string.Empty, listContent);
                }
            }
            catch (Exception ex)
            {
                result.Add(ex.Message, null);
            }
            return result;
        }

        /// <summary>
        /// xoa the image trong content.
        /// </summary>
        /// <param name="_str"></param>
        /// <returns></returns>
        internal static string RemoveImageTag(string _str)
        {
            while (_str.IndexOf("<img") != -1)
            {
                int start = _str.IndexOf("<img"); // lay vi tri the mo
                string str_first = _str.Substring(0, start); // lay phan noi dung truoc the mo

                string str_last = _str.Substring(start); // lay phan noi dung sau the mo
                int end = str_last.IndexOf("/>"); // lay vi tri the dong (bat dau tu phan noi dung sau the mo)
                str_last = str_last.Substring(end + 2); // lay phan noi dung sau the dong

                _str = str_first + str_last; // chuoi moi = phan noi dung truoc the mo + phan noi dung sau the dong
            }

            return _str;
        }

        /// <summary>
        /// tinh thong so page va recordsperpage de phan trang.
        /// </summary>
        /// <param name="totalRecords"></param>
        /// <param name="page"></param>
        /// <param name="rpp"></param>
        /// <param name="pagecount"></param>
        /// <param name="skip"></param>
        internal static void PagedList(int totalRecords, ref int page, int rpp, out int pagecount, out int skip)
        {
            // check page
            pagecount = Math.Max((int)Math.Ceiling((decimal)totalRecords / rpp), 1);
            page = Math.Min(page, pagecount);
            skip = (page - 1) * rpp;
            return;
        }

        /// <summary>
        /// date remain.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        internal static string DateTimeRemain(DateTime dt)
        {
            TimeSpan ts = DateTime.Now - dt;
            if (ts.Days == 0 && ts.Hours == 0 && ts.Minutes == 0)
            {
                return "a few seconds ago";
            }
            else if (ts.Days == 0 && ts.Hours == 0)
            {
                return ts.Minutes + " minutes ago";
            }
            else if (ts.Days == 0)
            {
                return ts.Hours + " hours " + ts.Minutes + " minutes ago";
            }
            else if (ts.Days > 365)
            {
                return Math.Round((decimal)(ts.Days / 365), 0, MidpointRounding.ToEven).ToString() + " years ago";
            }
            else if (ts.Days > 30)
            {
                return Math.Round((decimal)(ts.Days / 30), 0, MidpointRounding.ToEven).ToString() + " months ago";
            }
            return ts.Days + " days ago"; // +ts.Hours + " hours " + ts.Minutes + " minutes ago";
        }

        internal static bool IsValidEmail(string emailAddress)
        {
            try
            {
                return Regex.IsMatch(
                    emailAddress,
                    @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,24}))$",
                    RegexOptions.IgnoreCase,
                    TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        internal static string GetIDStr(int id)
        {
            string result = string.Empty;
            if (id < 10)
            {
                result = "000" + id.ToString();
            }
            else if (id < 100)
            {
                result = "00" + id.ToString();
            }
            else if (id < 1000)
            {
                result = "0" + id.ToString();
            }
            else
            {
                result = id.ToString();
            }

            return result;
        }

        internal static string GetCardTypeByCardNumber(string cardNumber)
        {
            // 'American Express       34, 37            15
            // 'Discover               6011              16
            // 'Master Card            51 to 55          16
            // 'Visa                   4                 13, 16

            // visa 
            Regex checkVisa = new("^(?:4[0-9]{12}(?:[0-9]{3})?)$");
            if (checkVisa.IsMatch(cardNumber) == true)
            {
                return "visa";
            }

            // amex
            Regex checkAmex = new("^(3[47][0-9]{13})$");
            if (checkAmex.IsMatch(cardNumber) == true)
            {
                return "amex";
            }

            // discover
            Regex checkDiscover = new("^(6011[0-9]{12})$");
            if (checkDiscover.IsMatch(cardNumber))
            {
                return "discover";
            }

            // mc
            Regex checkMC = new("^((?:51|55)[0-9]{14})$");
            if (checkMC.IsMatch(cardNumber))
            {
                return "mastercard";
            }

            return "Other";
        }

        /// <summary>
        /// save image from string 64bit.
        /// </summary>
        /// <param name="stringImage"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        internal static bool SaveBinaryImage(string stringImage, string filename)
        {
            var appPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            string path = Path.Combine(appPath, @"Upload\BinaryImage\");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            try
            {
                using (MemoryStream ms = new(Convert.FromBase64String(stringImage)))
                {
                    using (System.Drawing.Bitmap bm2 = new(ms))
                    {
                        bm2.Save(path + filename);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static string RandomNumber(string number_str, Random rd)
        {
            try
            {
                number_str = string.IsNullOrWhiteSpace(number_str) ? string.Empty : number_str;
                string number_rd_str = rd.Next(1, 9999).ToString().PadLeft(4, '0');

                return number_str + number_rd_str;
            }
            catch (Exception ex)
            {
                return "error: " + ex.Message;
            }
        }

        internal static string RemoveAllTag(string inputS)
        {
            string outputS = string.Empty;
            if (string.IsNullOrWhiteSpace(inputS) == false)
            {
                var arrString = inputS.Split(new char[] { });
                outputS = Regex.Replace(inputS, "<h1>|</h1>|<h2>|</h2>|<h3>|</h3>|<h4>|</h4>|<h5>|</h5>|<h6>|</h6>|<ul>|</ul>|<ol>|</ol>|<li>|</li>|<br />|<b>|</b>", string.Empty);
            }
            return outputS;
        }

        /// <summary>
        /// Sử dụng cho việc tạo mục lục tự động theo tag h2, h3.
        /// </summary>
        /// <param name="inputS">content.</param>
        /// <returns>string.</returns>
        public static string GetContentTagH2H3(string inputS)
        {
            string outputS = string.Empty;
            if (!string.IsNullOrEmpty(inputS))
            {
                Regex regex = new Regex("<h[23](.*?)>(.*?)</h[23]>");
                var h2h3 = regex.Matches(inputS); // Lấy thẻ h2,h3 trong chuỗi.
                int i = 0;
                foreach (var item in h2h3)
                {
                    string content = item.ToString();
                    // Lấy ra các attr trong tag - vd:<h2 class='abc' style='color:red'>VIDU</h2> => "class='abc' style='color:red'"
                    var content_replace = regex.Match(content);
                    if (content_replace.Groups[1] != null && !string.IsNullOrEmpty(content_replace.Groups[1]?.Value))
                    {
                        content = content.Replace(content_replace.Groups[1]?.Value, string.Empty); // Loại bỏ các attr tìm thấy.
                        // vd:<h2 class='abc' style='color:red'>VIDU</h2> => <h2>VIDU</h2>
                    }

                    if (content.Contains("<h2>"))
                    {
                        content = content.Replace("<h2>", $"<h2><a href='#ct_{i}'>").Replace("</h2>", "</a></h2>");
                    }
                    else
                    {
                        content = content.Replace("<h3>", $"<h3><a href='#ct_{i}'>").Replace("</h3>", "</a></h3>");
                    }
                    outputS += content + "|";
                    i++;
                }
            }
            return outputS?.TrimEnd('|');
        }

        public static string CleanPhoneNumber(string phoneNumber)
        {
            return Regex.Replace(phoneNumber ?? string.Empty, @"[^\d]", string.Empty);
        }
        public static string converttojpg(string url)
        {
            url = Uri.UnescapeDataString(url);
            string full_url = HttpContext.Current.Server.MapPath(url);
            string ext = Path.GetExtension(url).ToLower();
            if (ext == ".png" && System.IO.File.Exists(full_url))
            {
                Console.WriteLine(url);
                string name = Path.GetFileNameWithoutExtension(url);
                string path = Path.GetDirectoryName(url);
                var newpath = path + @"/" + name + ".jpg";
                Image png = Image.FromFile(full_url);
                png.Save(HttpContext.Current.Server.MapPath(newpath), ImageFormat.Jpeg);
                png.Dispose();
                File.Delete(full_url);
                return newpath;
            }
            return url;
        }
        public static bool create_mini_images(string url)
        {
            if (url != "/Content/admin/img/no_image.jpg")
            {
                url = Uri.UnescapeDataString(url);
                string full_url = HttpContext.Current.Server.MapPath(url);
                string ext = Path.GetExtension(url).ToLower();
                string name = Path.GetFileNameWithoutExtension(url);
                string path = Path.GetDirectoryName(url);
                List<(int w, int h)> sizes = new List<(int, int)> { (85, 85), (98, 98), (160, 160), (280, 280), (387, 387) };
                if (System.IO.File.Exists(full_url))
                {
                    Image png = Image.FromFile(full_url);
                    DirectoryInfo d = new(HttpContext.Current.Server.MapPath(path + "/_resize/"));
                    if (!d.Exists)
                    {
                        d.Create();
                    }
                    foreach (var s in sizes)
                    {
                        var newpath = path + $"/_resize/{name}_{s.w}x{s.h}.jpg";
                        var result = ResizeImage(png, new Size { Width = s.w, Height = s.h });
                        result.Save(HttpContext.Current.Server.MapPath(newpath), ImageFormat.Jpeg);
                        result.Dispose();
                    }
                    png.Dispose();
                    //System.IO.File.Delete(full_url);
                }
                
            }
            return true;
        }

        public static Image ResizeImage(Image imgToResize, Size destinationSize)
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
    }

    internal static class extentions
    {
        internal static List<Variance> DetailedCompare<T>(this T val1, T val2)
        {
            List<Variance> variances = new();
            FieldInfo[] fi = val1.GetType().GetRuntimeFields().ToArray();
            foreach (FieldInfo f in fi)
            {
                Variance v = new();
                v.Name = f.Name.Replace(">k__BackingField", string.Empty).Replace("<", string.Empty);
                v.Old = StripHTML(f.GetValue(val1));
                v.New = StripHTML(f.GetValue(val2));
                if (v.Name != "UpdateAt" && v.Old?.ToString() != v.New?.ToString())
                {
                    v.New = sub(v.New);
                    v.Old = sub(v.Old);
                    variances.Add(v);
                }
            }
            return variances;
        }

        internal static object StripHTML(object input)
        {
            if (input is string)
            {
                return Regex.Replace(input.ToString(), "<.*?>", string.Empty);
            }
            return input;
        }

        internal static object sub(object input)
        {
            if (input is string && input.ToString().Length > 100)
            {
                return input.ToString().Substring(0, 100) + "...";
            }
            return input;
        }

    }

    public class Variance
    {
        public string Name { get; set; }

        public object Old { get; set; }

        public object New { get; set; }
    }
}