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
    public partial class vn_district
    {
        [Key]
        public string districtid { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string location { get; set; }
        public string provinceid { get; set; }
    }
}
