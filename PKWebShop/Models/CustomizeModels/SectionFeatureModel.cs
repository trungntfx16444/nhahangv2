namespace PKWebShop.Models.CustomizeModels
{
    using PKWebShop.AppLB;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class sFeatures_view
    {
        // tạo một đối tượng tính năng chứa dữ liệu từ model sectionfeature
        public sectionfeature Feature;
        
        public List<featuredetail_view> Details;
        public dynamic Data;
    }
    public class featuredetail_view
    {
        // tạo một đối tượng Detail chứa dữ liệu từ bảng sectionfeaturedeal
        public sectionfeaturedetail Detail;
        // tạo một list dữ liệu chứa ds các 
        public List<uploadmorefile> Files;

        public dynamic Data { get; set; }
    }
    public class SectionFeatureModel
    {
        public string SectionCode { get; set; }

        public string SectionTitle { get; set; }

        public string SectionDescription { get; set; }

        public string Description { get; set; }

        public string Picture { get; set; }

        public string Title { get; set; }

        public string URL { get; set; }

        public int? VolumeNumber { get; set; }
    }

    public class SessionOrderProducts
    {
        public string Id { get; set; }

        public string Picture { get; set; }

        public string ProductName { get; set; }

        public string Price { get; set; }

        public int Quantity { get; set; }

        public string Total { get; set; }
    }

    public class sectionfeaturedetails_view
    {
        public string seccode { get; set; }

        public string secname { get; set; }

        public string id { get; set; }

        public string title { get; set; }

        public string subtitle { get; set; }

        public string desc { get; set; }

        public string descRegex { get; set; }

        public int? volume { get; set; }

        public string pic { get; set; }

        public string url { get; set; }
    }
}