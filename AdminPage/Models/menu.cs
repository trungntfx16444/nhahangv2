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
    
    public partial class menu
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string URL { get; set; }
        public Nullable<bool> Fixed { get; set; }
        public string ParentMenuId { get; set; }
        public string ParentMenuName { get; set; }
        public Nullable<int> Order { get; set; }
        public string LangCode { get; set; }
        public Nullable<bool> ShowCategory { get; set; }
        public Nullable<bool> Hidden { get; set; }
    }
}