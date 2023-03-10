using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml.Linq;
using Inner.Libs.Helpful;
using PKWebShop.App_Start;
using PKWebShop.Models;

namespace PKWebShop.Controllers
{
    public class SitemapController : Controller
    {
        /// <summary>
        /// time default doi voi cac page tinh, cac page bai biet duoc lay tu ngay update sau cung.
        /// </summary>
        string lastModify = "2021-6-20";

        [HttpGet]
        public ActionResult Index()
        {
            var sitemapNodes = GetSitemapNodes();
            string xml = CreateSitemapDocument(sitemapNodes);
            return this.Content(xml, "text/xml", System.Text.Encoding.UTF8);
        }


        public string CreateSitemapDocument(IEnumerable<SitemapNode> sitemapNodes)

        {
            XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XElement root = new XElement(xmlns + "urlset");
            foreach (SitemapNode sitemapNode in sitemapNodes)
            {
                XElement urlElement = new XElement(
                    xmlns + "url",
                    new XElement(xmlns + "loc", Uri.EscapeUriString(sitemapNode.Url)),
                    sitemapNode.LastModified == null ? null : new XElement(xmlns + "lastmod", sitemapNode.LastModified.Value.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:sszzz")),
                    sitemapNode.Frequency == null ? null : new XElement(xmlns + "changefreq", sitemapNode.Frequency.Value.ToString().ToLowerInvariant()),
                    sitemapNode.Priority == null ? null : new XElement(xmlns + "priority", sitemapNode.Priority.Value.ToString("F1", CultureInfo.GetCultureInfo("en-US"))));
                root.Add(urlElement);
            }

            XDocument document = new XDocument(root);
            return document.ToString();
        }


        public List<SitemapNode> GetSitemapNodes()
        {
            string host = $"{Request.Url.Scheme}://{Request.Url.Authority}";
            List<SitemapNode> nodes = new List<SitemapNode>();
            RouteTable.Routes.ForEach(route => { });
            nodes.Add(
                new SitemapNode()
                {
                    Url = host,
                    Priority = 1,
                    LastModified = DateTime.Parse(lastModify)
                });
            nodes.Add(
                new SitemapNode()
                {
                    Url = $"{host}/{UrlRewriteTemplate.LIEN_HE}",
                    Priority = 0.7,
                    LastModified = DateTime.Parse(lastModify)
                });
            
            using WebShopEntities db = new();

            // post
            foreach (var item in db.n_news.Where(x => x.Active == true).ToList())
            {
                nodes.Add(
                    new SitemapNode()
                    {
                        Url = $"{host}/{item.UrlCode}-nd{item.ReId}",
                        Priority = 0.8,
                        LastModified = item.UpdateAt.HasValue == false ? item.CreatedAt : item.UpdateAt
                    });
            }

            // categories
            foreach (var item in db.categories.ToList())
            {
                nodes.Add(
                    new SitemapNode()
                    {
                        Url = $"{host}/{item.UrlCode}-pc{item.ReId}",
                        Priority = 0.8,
                        LastModified = DateTime.Parse(lastModify),
                    });
            }
            // products
            foreach (var item in db.products.Where(x => x.IsActive != false).ToList())
            {
                nodes.Add(
                    new SitemapNode()
                    {
                        Url = $"{host}/{item.Url}-pd{item.ReId}",
                        Priority = 0.8,
                        LastModified = item.UpdateAt.HasValue == false ? item.CreateAt : item.UpdateAt
                    });
            }
            return nodes;
        }
    }

    public class SitemapNode
    {
        public SitemapFrequency? Frequency { get; set; }
        public DateTime? LastModified { get; set; }
        public double? Priority { get; set; }
        public string Url { get; set; }
    }

    public enum SitemapFrequency
    {
        Never,
        Yearly,
        Monthly,
        Weekly,
        Daily,
        Hourly,
        Always
    }
}