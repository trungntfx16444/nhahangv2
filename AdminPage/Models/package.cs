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
    
    public partial class package
    {
        public string Id { get; set; }
        public string TenantId { get; set; }
        public string PackageType { get; set; }
        public string Status { get; set; }
        public string Code { get; set; }
        public Nullable<decimal> Value { get; set; }
        public Nullable<System.DateTime> EffectiveDate { get; set; }
        public Nullable<System.DateTime> ExpirationDate { get; set; }
    }
}