using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Inner.Libs.Helpful;
using PKWebShop.Enums;
using PKWebShop.Utils;

namespace PKWebShop.Models
{
  public partial class Package : Entity
  {
    public string TenantId { get; set; } = null!;
    [MaxLength(20)] public string PackageType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Code { get; set; }
    public decimal? Value { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
  }

  public partial class Package
  {
    /****************************************************************************************************************/
    [NotMapped]
    public PackageType Type => Ext.EnumParse<PackageType>(PackageType);
    [NotMapped]
    public bool IsActive => Ext.EnumParse<ActiveStatus>(Status) == ActiveStatus.Active;
    [NotMapped]
    public bool IsPending => Ext.EnumParse<ActiveStatus>(Status) == ActiveStatus.Pending;
    [NotMapped]
    public bool IsInActive => Ext.EnumParse<ActiveStatus>(Status) == ActiveStatus.InActive;
    [NotMapped]
    public bool IsExpiration => Ext.EnumParse<ActiveStatus>(Status) == ActiveStatus.Expiration || ((ExpirationDate ?? DateTime.Today).Date - DateTime.Today.Date).TotalDays <= 0;
    [NotMapped]
    public bool IsWarning => ((ExpirationDate ?? DateTime.Today).Date - DateTime.Today.Date).TotalDays <= Constant.WarningDate;
    /****************************************************************************************************************/
    [NotMapped]
    public PaymentMethod Pay => Ext.EnumParse<PaymentMethod>(Code);
    /****************************************************************************************************************/
  }

  public partial class PackageSetting : Entity
  {
    [MaxLength(20)]
    public string WebPackage { get; set; } = null!;
    public decimal DiskSize { get; set; }
    public bool GiftCode { get; set; }
    public bool ShippingFee { get; set; }
    public bool PaymentOnline { get; set; }
    public bool MembershipPoints { get; set; }
    public bool Warehouse { get; set; }
    public bool Dept { get; set; }
    public bool Vendor { get; set; }
    public bool Comment { get; set; }
    
    // Messenger chat
    public bool MessengerChat { get; set; }
    // Login facebook/google
    public bool SocialLogin { get; set; }
    // Multi language
    public bool MultiLanguage { get; set; }
    // Button order contact
    public bool OrderContact { get; set; }
    
    [NotMapped]
    public WebPackageType Type => Ext.EnumParse<WebPackageType>(WebPackage);
  }
}