//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PKWebShop.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("sectionfeature")]
    public partial class sectionfeature
    {
        [Key]
        public string Code { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public Nullable<bool> Status { get; set; }
        [Column(TypeName = "text")]
        public string Description { get; set; }
        public string LangCode { get; set; }
        public string ReId { get; set; }
        public int? Order { get; set; }
        public string Url { get; set; }
        public DateTime? CreateAt { get; set; }
        public Nullable<bool> Hidden { get; set; }
    }
}