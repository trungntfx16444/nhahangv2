using System;
using System.ComponentModel;

namespace PKWebShop.Models
{
    public class vendor
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public decimal? RemainingDebt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        [DefaultValue(true)]
        public bool Active { get; set; }
        public string Logo { get; set; }
        public string Description { get; set; }
    }
}