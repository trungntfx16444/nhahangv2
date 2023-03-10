using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PKWebShop.Models
{
    using System.ComponentModel.DataAnnotations;

    public abstract class Entity
    {
        [MaxLength(50)]
        public string Id { get; set; } = null!;
    }

    public abstract class Payment : Entity
    {
        [MaxLength(50)]
        public string CustomerId { get; set; } = null!;
        public string OrderId { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        [ForeignKey("OrderId")]
        public order Order { get; set; } = null!;
    }
}
