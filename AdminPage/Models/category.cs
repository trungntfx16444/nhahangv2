//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AdminPage.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class category
    {
        public string Id { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string LangCode { get; set; }
        public string ReId { get; set; }
        public Nullable<int> Order { get; set; }
        public string UrlCode { get; set; }
        public string ParentId { get; set; }
        public string ParentName { get; set; }
        public Nullable<int> Level { get; set; }
        public Nullable<bool> Active { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDesc { get; set; }
        public string MetaKeyWord { get; set; }
        public Nullable<bool> Sellable { get; set; }
        public string VendorId { get; set; }
        public string SubTitle { get; set; }
    }
}
