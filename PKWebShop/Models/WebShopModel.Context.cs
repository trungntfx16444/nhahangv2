using System.Data.Entity;

namespace PKWebShop.Models
{
    public partial class WebShopEntities : DbContext
    {
        public WebShopEntities() : base("name=WebShopEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //throw new UnintentionalCodeFirstException();
        }

        public virtual DbSet<category> categories { get; set; }
        public virtual DbSet<customer_request> customer_request { get; set; }
        public virtual DbSet<customer_score> customer_score { get; set; }
        public virtual DbSet<customer> customers { get; set; }
        public virtual DbSet<introduce> introduces { get; set; }
        public virtual DbSet<loyalcustomer> loyalcustomers { get; set; }
        public virtual DbSet<menu> menus { get; set; }
        public virtual DbSet<n_news> n_news { get; set; }
        public virtual DbSet<n_news_tags> n_news_tags { get; set; }
        public virtual DbSet<n_parent_topic> n_parent_topic { get; set; }
        public virtual DbSet<n_toppic> n_toppic { get; set; }
        public virtual DbSet<order> orders { get; set; }
        public virtual DbSet<orders_detail> orders_detail { get; set; }
        public virtual DbSet<policy> policies { get; set; }
        public virtual DbSet<product_properties> product_properties { get; set; }
        public virtual DbSet<ProductOptionPrice> ProductOptionPrice { get; set; }
        public virtual DbSet<product_option> product_options { get; set; }
        public virtual DbSet<product_option_value> product_option_values { get; set; }
        public virtual DbSet<sectionfeature> sectionfeatures { get; set; }
        public virtual DbSet<sectionfeaturedetail> sectionfeaturedetails { get; set; }
        public virtual DbSet<service> services { get; set; }
        public virtual DbSet<tag> tags { get; set; }
        public virtual DbSet<uploadmorefile> uploadmorefiles { get; set; }
        public virtual DbSet<user_actions_log> user_actions_log { get; set; }
        public virtual DbSet<users_permissions> users_permissions { get; set; }
        public virtual DbSet<users_permissions_granted> users_permissions_granted { get; set; }
        public virtual DbSet<viewpagetracker> viewpagetrackers { get; set; }
        public virtual DbSet<vn_district> vn_district { get; set; }
        public virtual DbSet<vn_province> vn_province { get; set; }
        public virtual DbSet<vn_ward> vn_ward { get; set; }
        public virtual DbSet<webconfiguration> webconfigurations { get; set; }
        public virtual DbSet<whychooseu> whychooseus { get; set; }
        public virtual DbSet<popupad> popupads { get; set; }
        public virtual DbSet<product> products { get; set; }
        public virtual DbSet<cart_detail> cart_details { get; set; }
        public virtual DbSet<vendor> vendors { get; set; }
        public virtual DbSet<import_ticket> import_tickets { get; set; }
        public virtual DbSet<expense> expense { get; set; }
        public virtual DbSet<receipts> receipts { get; set; }
        public virtual DbSet<user> users { get; set; }

        public virtual DbSet<giasu_class> giasu_class { get; set; }
        public virtual DbSet<giasu_option> giasu_option { get; set; }
        public virtual DbSet<giasu_teacher> giasu_teacher { get; set; }

        public virtual DbSet<Package> Package { get; set; }
        public virtual DbSet<PackageSetting> PackageSetting { get; set; }
        
        public virtual DbSet<BonusPointConfig> BonusPointConfig { get; set; }
        public virtual DbSet<gift_code> gift_code { get; set; }
        public virtual DbSet<shipping_config> shipping_config { get; set; }

        public virtual DbSet<comments> comments { get; set; }
        
        public virtual DbSet<VNP_PaymentData> VNP_PaymentData { get; set; }
    }
}
