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
    
    public partial class orders_detail
    {
        public string Id { get; set; }
        public string OrderId { get; set; }
        public string ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public Nullable<double> Price { get; set; }
        public Nullable<int> Quantity { get; set; }
        public string ServiceId { get; set; }
        public string ServiceName { get; set; }
        public Nullable<System.DateTime> CreateAt { get; set; }
        public string LangCode { get; set; }
        public string ParentPropertiesId { get; set; }
        public string PropertiesId { get; set; }
        public string PriceOptId { get; set; }
        public string PriceOptId1 { get; set; }
        public string PriceOptId2 { get; set; }
    }
}
