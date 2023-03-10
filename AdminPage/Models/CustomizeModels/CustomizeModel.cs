using AdminPage.App_Start;
using System.Collections.Generic;

namespace AdminPage.Models.CustomizeModels
{
    //public class code_name
    //{
    //    public string code { get; set; }

    //    public string name { get; set; }
    //}

    public static class SiteSEO
    {
        public static List<SEO_Meta> New_SEO_Meta()
        {
            var list_SEO = new List<SEO_Meta>();
            list_SEO.Add(new SEO_Meta { code = code_SEO.TrangChu, url = string.Empty });
            list_SEO.Add(new SEO_Meta { code = code_SEO.SanPham, url = UrlRewriteTemplate.SAN_PHAM });
            list_SEO.Add(new SEO_Meta { code = code_SEO.TinTuc, url = UrlRewriteTemplate.BAI_VIET });
            return list_SEO;
        }

        public static class code_SEO
        {
            public static string TrangChu = "trangchu";
            public static string SanPham = "sanpham";
            public static string TinTuc = "tintuc";
        }

        public class SEO_Meta
        {
            public string code { get; set; }
            public string meta_title { get; set; }
            public string meta_desc { get; set; }
            public string meta_keyword { get; set; }
            public string url { get; set; }
            public string meta_extend { get; set; }
        }
    }

}