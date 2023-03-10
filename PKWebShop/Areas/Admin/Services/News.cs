using PKWebShop.Utils;

namespace PKWebShop.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using PKWebShop.AppLB;
    using PKWebShop.Models;
    using PKWebShop.Models.CustomizeModels;

    internal class News
    {
        private static SiteLang.Language defaultLanguage = SiteLang.GetDefault();

        internal static n_news SaveNews(n_news nm, List<string> picmore, string newReId)
        {
            using (var db = new WebShopEntities())
            {
                int i = 0;
                start_random:
                if (i < 5)
                {
                    nm.UrlCode = !string.IsNullOrEmpty(nm.UrlCode) ? nm.UrlCode : CommonFunc.ConvertNonUnicodeURL(nm.Name);
                    if (db.n_news.Any(x => x.UrlCode == nm.UrlCode && x.ReId != nm.ReId))
                    {
                        nm.UrlCode += CommonFunc.GetRandomString(1, "0123456789");
                        i++;
                        goto start_random;
                    }
                }
                else
                {
                    throw new Exception("Url bài viết đã tồn tại.");
                }

                var tags = nm.Tags?.Split(',').Select(t => new n_news_tags { tag_name = t, tag_code = CommonFunc.ConvertNonUnicodeURL(t) }).ToList();

                if (!string.IsNullOrEmpty(nm.NewsId))
                {
                    var news = db.n_news.Find(nm.NewsId);
                    news.Name = nm.Name;
                    news.UrlCode = nm.UrlCode;
                    news.Picture = nm.Picture;
                    news.ShortDescription = nm.ShortDescription;
                    Regex regex = new("<img(?!((?!/>).)* loading=\"lazy\")");
                    news.Decription = regex.Replace(nm.Decription ?? string.Empty, "<img loading=\"lazy\"");

                    news.Active = nm.Active;
                    news.UpdateBy = Authority.GetThisUser().Fullname;
                    news.UpdateAt = DateTime.Now;
                    news.Order = nm.Order;
                    var tIds = JsonConvert.DeserializeObject<List<string>>(nm.TopicId) ?? new List<string>();
                    var topics = db.n_toppic.Where(n => tIds.Contains(n.ReId));
                    news.TopicId = JsonConvert.SerializeObject(topics.Select(t => t.TopicId));
                    news.ToppicName = JsonConvert.SerializeObject(topics.Select(t => t.Name));

                    news.Tags = string.Join(",", tags?.Select(t => t.tag_name) ?? new List<string>());
                    news.Tags_code = string.Join(",", tags?.Select(t => t.tag_code) ?? new List<string>());
                    tags?.Where(t => !db.n_news_tags.Any(_t => _t.tag_code == t.tag_code)).ToList().ForEach(t =>
                    {
                        t.id = AppFunc.NewShortId();
                        db.n_news_tags.Add(t);
                    });
                    news.TitleWebsite = string.IsNullOrEmpty(nm.TitleWebsite) ? nm.Name : nm.TitleWebsite;
                    news.SEODescription = string.IsNullOrEmpty(nm.SEODescription) ? nm.ShortDescription : nm.SEODescription;
                    news.ImageAlt = string.IsNullOrEmpty(nm.ImageAlt) ? nm.Name : nm.ImageAlt;
                    db.Entry(news).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    UserContent.GetWebIntroFromNews(true);
                    if (db.n_news.Any(n => string.IsNullOrEmpty(n.UrlCode)))
                    {
                        updateNewsUrlCode();
                    }
                    nm = news;
                }
                else
                {
                    nm.NewsId = AppFunc.NewShortId();
                    nm.CreatedAt = DateTime.Now;
                    nm.CreatedBy = Authority.GetThisUser().Fullname;
                    if (string.IsNullOrEmpty(nm.LangCode))
                    {
                        nm.LangCode = defaultLanguage.Code;
                    }
                    if (string.IsNullOrEmpty(nm.ReId))
                    {
                        nm.ReId = newReId;
                    }

                    var tIds = JsonConvert.DeserializeObject<List<string>>(nm.TopicId) ?? new List<string>();
                    var topics = db.n_toppic.Where(n => tIds.Contains(n.ReId));
                    nm.TopicId = JsonConvert.SerializeObject(topics.Select(t => t.TopicId));
                    nm.ToppicName = JsonConvert.SerializeObject(topics.Select(t => t.Name));

                    //var topic = db.n_toppic.Find(nm.TopicId);
                    //nm.ToppicName = topic?.Name;

                    Regex regex = new("<img(?! loading=\"lazy\")");
                    nm.Decription = regex.Replace(nm.Decription ?? string.Empty, "<img loading=\"lazy\"");

                    nm.Tags = string.Join(",", tags?.Select(t => t.tag_name) ?? new List<string>());
                    nm.Tags_code = string.Join(",", tags?.Select(t => t.tag_code) ?? new List<string>());
                    nm.TitleWebsite = nm.TitleWebsite ?? nm.Name;
                    tags?.Where(t => !db.n_news_tags.Any(_t => _t.tag_code == t.tag_code)).ToList().ForEach(t =>
                    {
                        t.id = AppFunc.NewShortId();
                        db.n_news_tags.Add(t);
                    });
                    db.n_news.Add(nm);
                    db.SaveChanges();
                    UserContent.GetWebIntroFromNews(true);
                    if (db.n_news.Any(n => string.IsNullOrEmpty(n.UrlCode)))
                    {
                        updateNewsUrlCode();
                    }
                }
                #region UPLOAD MORE PICTURE
                var dbSave = false;
                var more_picture = db.uploadmorefiles.Where(x => x.TableId == nm.NewsId && x.TableName == "n_news").ToList();
                if (more_picture.Count() > 0)
                {
                    db.uploadmorefiles.RemoveRange(more_picture);
                    dbSave = true;
                }

                if (picmore?.Count > 0)
                {
                    foreach (var item in picmore)
                    {
                        var pic = new uploadmorefile
                        {
                            UploadId = Guid.NewGuid().ToString().Replace("-", string.Empty),
                            TableId = nm.ReId,
                            TableName = nm.GetType().Name,
                            FileName = item,
                        };
                        db.uploadmorefiles.Add(pic);
                        dbSave = true;
                    }
                }

                if (dbSave)
                {
                    db.SaveChanges();
                }
                #endregion
                return nm;
            }
        }

        internal static void updateNewsUrlCode()
        {
            var db = new WebShopEntities();
            db.n_news.Where(n => string.IsNullOrEmpty(n.UrlCode)).ToList().ForEach(n =>
            {
                n.UrlCode = AppLB.CommonFunc.ConvertNonUnicodeURL(n.Name);
            });
            db.SaveChanges();
        }
    }
}