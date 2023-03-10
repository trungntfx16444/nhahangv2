using System;

namespace PKWebShop.Models
{
    public class import_ticket
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Vendor_Id { get; set; }
        //public string Product_Id { get; set; }
        //public decimal? EntryPrice { get; set; }
        public string Other_costs { get; set; }
        public decimal? Total { get; set; }
        //public int? ImportQty { get; set; }
        //public string ImportUnit { get; set; }
        public string PayStatus { get; set; }
        public string ImportStatus { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string Note { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public string ProductDetail { get; set; }
        public bool? IsStorage { get; set; }
    }
}