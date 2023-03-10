namespace AdminPage.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Web;
    //using AdminPage.Resources;

    /// <summary>
    /// Class ngôn ngữ của trang.
    /// </summary>
    public static class SiteLang
    {
        /// <summary>
        /// List ngôn ngữ.
        /// </summary>
        private static readonly List<Language> Languages = new ()
        {
            new Language { Name = "VIE", Code = "vi", Icon = "flag-icon-vn", FullName = "Tiếng Việt" },
            // new Language { Name = "ENG", Code = "en", Icon = "flag-icon-gb", FullName = "Tiếng Anh" },
            // new Language { Name = "French", Code = "fr", Icon = "flag-icon-fr" },
        };

        private static string currentLang;

        /// <summary>
        /// Check ngôn ngữ tồn tại.
        /// </summary>
        /// <param name="lang"> code. </param>
        /// <returns> true/false. </returns>
        public static bool IsLanguageAvailable(string lang)
        {
            return Languages.Any(l => l.Code.Equals(lang));
        }

        public static string GetCurrentLang()
        {
            return currentLang;
        }

        public static Language GetLang(string lang)
        {
            return Languages.FirstOrDefault(l => l.Code.Equals(lang));
        }

        public static List<Language> GetListLangs()
        {
            return Languages.ToList();
        }

        public static Language GetDefault()
        {
            return Languages.FirstOrDefault();
        }

        public static int getOrder(string lang)
        {
            if (string.IsNullOrEmpty(lang))
            {
                return 0;
            }
            else
            {
                int index = Languages.FindIndex(l => l.Code == lang);
                return index != -1 ? index : int.MaxValue;
            }
        }

        //public static string SetLanguage(string lang)
        //{
        //    try
        //    {
        //        if (!Languages.Any(l => l.Code.Equals(lang)))
        //        {
        //            lang = Languages[0].Code;
        //        }

        //        var culInfo = new CultureInfo(lang);
        //        Thread.CurrentThread.CurrentUICulture = culInfo;
        //        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(culInfo.Name);
        //        HttpCookie langCookie = new ("culture", lang);
        //        langCookie.Expires = DateTime.Now.AddYears(1);
        //        HttpContext.Current.Response.Cookies.Add(langCookie);
        //        Resource.Culture = culInfo;
        //        currentLang = lang;
        //        return lang;
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        public class Language
        {
            public string Name { get; set; }

            public string Code { get; set; }

            public string Icon { get; internal set; }

            public string FullName { get; set; }
        }
    }
}