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
    
    public partial class customer
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public string LastLogin { get; set; }
        public string Role { get; set; }
        public int BonusPoints { get; set; }
        public Nullable<bool> Active { get; set; }
        public Nullable<System.DateTime> CreateAt { get; set; }
        public string CreateBy { get; set; }
        public Nullable<System.DateTime> UpdateAt { get; set; }
        public string UpdateBY { get; set; }
        public Nullable<int> points { get; set; }
        public Nullable<int> points_used { get; set; }
        public string LangCode { get; set; }
        public string AccountType { get; set; }
        public string Avatar { get; set; }
    }
}
