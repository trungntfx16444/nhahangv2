namespace PKWebShop.Models
{
    using System;

    public class expense
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Vendor_Id { get; set; }
        public string Vendor_Name { get; set; }
        public string ImportTicket_Id { get; set; }
        public decimal? Total { get; set; }
        public string PaymentMethod { get; set; }
        public string Reason { get; set; }
        public string Note { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}