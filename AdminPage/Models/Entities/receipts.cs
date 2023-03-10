using System;

namespace AdminPage.Models
{
    public class receipts : Entity
    {
        public string? CustomerId { get; set; }
        public string? OrderId { get; set; }
        public string? Note { get; set; }
        
        public string? PaymentStatus { get; set; }
        public decimal PaymentAmount { get; set; }
        public string? PaymentMethod { get; set; }

        public DateTime? ReceiptsAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}