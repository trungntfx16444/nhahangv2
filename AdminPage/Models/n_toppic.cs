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
    
    public partial class n_toppic
    {
        public string TopicId { get; set; }
        public string Name { get; set; }
        public string ParentTopicId { get; set; }
        public string ParentTopicName { get; set; }
        public Nullable<System.DateTime> CreatedAt { get; set; }
        public Nullable<bool> ShowMenu { get; set; }
        public Nullable<bool> ShowNews { get; set; }
        public Nullable<int> Order { get; set; }
        public string CreateBy { get; set; }
        public Nullable<System.DateTime> UpdateAt { get; set; }
        public string UpdateBy { get; set; }
        public string URL { get; set; }
        public string RelatedTopicId { get; set; }
        public string RelatedTopicName { get; set; }
        public Nullable<bool> IsDefault { get; set; }
        public string LangCode { get; set; }
        public string ReId { get; set; }
        public string Seo_desc { get; set; }
        public string Seo_title { get; set; }
    }
}
