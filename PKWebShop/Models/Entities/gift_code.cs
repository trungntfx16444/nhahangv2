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

    public partial class gift_code
    {
        [Key]
        public string code { get; set; }
        public string name { get; set; }
        public Nullable<System.DateTime> start_date { get; set; }
        public Nullable<System.DateTime> end_date { get; set; }
        public Nullable<decimal> price { get; set; }
        public decimal? GrandTotalFrom { get; set; }
        public Nullable<float> percent { get; set; }
        public Nullable<bool> active { get; set; }
        public string LangCode { get; set; }
    }
}
