namespace PKWebShop.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class comments
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; }

        [MaxLength(50)]
        public string MappingId { get; set; }

        [MaxLength(100)]
        public string Type { get; set; }

        [MaxLength(50)]
        public string PeopleId { get; set; }

        [MaxLength(200)]
        public string PeopleName { get; set; }

        public string Content { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public string MoreInfo { get; set; }

        public double? StarNumber { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }
}