namespace PKWebShop.Models
{
    using System.Linq;

    public class DBLangCustom : WebShopEntities
    {
        
        public string LangCode = Services.SiteLang.GetCurrentLang();
        private readonly string defaultCode = Services.SiteLang.GetDefault()?.Code;

        internal void setLanguage(string langCode)
        {
            LangCode = Services.SiteLang.IsLanguageAvailable(langCode) ? langCode : Services.SiteLang.GetDefault().Code;
        }
        
        // lấy ra danh sách các món category dự trên langcode và điều kiện sellable là 1;
        public virtual IQueryable<category> categories => base.categories.Where(c => (c.LangCode ?? defaultCode) == LangCode && c.Sellable == true).OrderBy(x => x.Order);

        // public virtual IQueryable<customer_score> customer_score_c { get { return base.customer_score.Where(c => (c.LangCode ?? Default_code) == LangCode); } }
        // public virtual IQueryable<customer> customers_c { get { return base.customers.Where(c => (c.LangCode ?? Default_code) == LangCode); } }
        // public virtual IQueryable<giasu_class> giasu_class_c { get { return base.giasu_class.Where(c => (c.LangCode ?? Default_code) == LangCode); } }
        // public virtual IQueryable<giasu_option> giasu_option_c { get { return base.giasu_option.Where(c => (c.LangCode ?? Default_code) == LangCode); } }
        // public virtual IQueryable<giasu_teacher> giasu_teacher_c { get { return base.giasu_teacher.Where(c => (c.LangCode ?? Default_code) == LangCode); } }

        // lấy ra danh sách gift code dựa trên langcode
        public virtual IQueryable<gift_code> gift_code => base.gift_code.Where(c => (c.LangCode ?? defaultCode) == LangCode);
        
        // lấy ra danh sách giới thiệu dựa trên langcode
        public virtual IQueryable<introduce> introduces => base.introduces.Where(c => (c.LangCode ?? defaultCode) == LangCode);


        // public virtual IQueryable<loyalcustomer> loyalcustomers_c { get { return base.loyalcustomers.Where(c => (c.LangCode ?? Default_code) == LangCode); } }
        // lấy ra danh sách menu dựa trên langcode và hidden == fasle (0)
        public virtual IQueryable<menu> menus => base.menus.Where(c => (c.LangCode ?? defaultCode) == LangCode && c.Hidden != true);
        
        // lấy ra danh sách tin tức dựa trên langcode
        public virtual IQueryable<n_news> n_news => base.n_news.Where(c => (c.LangCode ?? defaultCode) == LangCode);
        
        // lấy ra danh sách new tage dựa trên lagcode
        public virtual IQueryable<n_news_tags> n_news_tags => base.n_news_tags.Where(c => (c.LangCode ?? defaultCode) == LangCode);
        
        // lấy ra danh sách topic gốc topic (cha) dựa trên langcode
        public virtual IQueryable<n_parent_topic> n_parent_topic => base.n_parent_topic.Where(c => (c.LangCode ?? defaultCode) == LangCode);
        
        // lây ra danh sách  n topic  dựa trên langcode
        public virtual IQueryable<n_toppic> n_toppic => base.n_toppic.Where(c => (c.LangCode ?? defaultCode) == LangCode);

        // public virtual IQueryable<order> orders_c { get { return base.orders.Where(c => (c.LangCode ?? Default_code) == LangCode); } }
        // public virtual IQueryable<orders_detail> orders_detail { get { return base.orders_detail.Where(c => (c.LangCode ?? Default_code) == LangCode); } }
        // public virtual IQueryable<policy> policies_c { get { return base.policies.Where(c => (c.LangCode ?? Default_code) == LangCode); } }

        // lấy ra danh sách popup ads dựa trên langcode
        public virtual IQueryable<popupad> popupads => base.popupads.Where(c => (c.LangCode ?? defaultCode) == LangCode);

        // lấy ra danh sách các thuộc tính của sản phậm dựa vào langcode
        public virtual IQueryable<product_properties> product_properties => base.product_properties.Where(c => (c.LangCode ?? defaultCode) == LangCode);

        // lấy ra danh sách giá của các sản phẩm dựa vào langcode 
        public virtual IQueryable<ProductOptionPrice> ProductOptionPrice => base.ProductOptionPrice.Where(c => (c.LangCode ?? defaultCode) == LangCode);
        
        // lấy ra danh sách sản phẩm dựa vào langcode với sellable = true;
        public virtual IQueryable<product> products => base.products.Where(c => (c.LangCode ?? defaultCode) == LangCode && c.IsActive == true && c.Sellable == true);
        
        // lấy ra danh sách các phần tính năng của trang web dựa trên langcode và hidden = false
        public virtual IQueryable<sectionfeature> sectionfeatures => base.sectionfeatures.Where(c => (c.LangCode ?? defaultCode) == LangCode && c.Hidden != true);
        
        // lấy ra danh sách các các chi tiết của session dựa vào langcode
        public virtual IQueryable<sectionfeaturedetail> sectionfeaturedetails => base.sectionfeaturedetails.Where(c => (c.LangCode ?? defaultCode) == LangCode);
        
        // lấy ra danh sách các serivces dựa vào langcode
        public virtual IQueryable<service> services => base.services.Where(c => (c.LangCode ?? defaultCode) == LangCode);

        // lấy ra danh sách các tab dựa vào langcode
        public virtual IQueryable<tag> tags => base.tags.Where(c => (c.LangCode ?? defaultCode) == LangCode);

        // public virtual IQueryable<uploadmorefile> uploadmorefiles_c { get { return base.uploadmorefiles.Where(c => (c.LangCode ?? Default_code) == LangCode); } }
        // public virtual IQueryable<user> users_c { get { return base.users.Where(c => (c.LangCode ?? Default_code) == LangCode); } }
        // public virtual IQueryable<viewpagetracker> viewpagetrackers_c { get { return base.viewpagetrackers.Where(c => (c.LangCode ?? Default_code) == LangCode); } }
        
        // lấy ra danh sách các cài đặt của trang web dựa trên langcode
        public virtual IQueryable<webconfiguration> webconfigurations => base.webconfigurations.Where(c => (c.LangCode ?? defaultCode) == LangCode);

        // lấy ra danh sách các bài why choose us dựa trên langcode
        public virtual IQueryable<whychooseu> whychooseus => base.whychooseus.Where(c => (c.LangCode ?? defaultCode) == LangCode);
    }
}