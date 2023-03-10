namespace PKWebShop.Models.CustomizeModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    public class Products_SectionView
    {
        public sectionfeaturedetail section_detail { get; set; }

        public IEnumerable<product> list_products { get; set; }
        public List<uploadmorefile> pics { get; internal set; }
    }

    public class ProductsHome
    {
        public sectionfeaturedetail Section_detail { get; set; }
        public List<uploadmorefile> Pics { get; internal set; }
        public List<CateProduct> Cate_product { get; set; }
    }

    public class CateProduct
    {
        public category Cate { get; set; }
        public List<product> Products { get; set; }
    }

    public class submit_product
    {
        public string Product_Id { get; set; }
        public string Vendor_Id { get; set; }
        public int EntryPrice { get; set; }
        public int ImportQty { get; set; }
        public int QtyExchange { get; set; }
        public string ImportUnit { get; set; }
    }
}