using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Inner.Libs.Helpful;
using PKWebShop.Enums;

namespace PKWebShop.Models.DTO
{
    using System;
    using PKWebShop.Utils;

    public class PackageInfo
    {
        public string? WebPackage { get; set; }

        public string? WebPackageKey { get; set; } = "web_default";

        public bool IsExpired { get; set; } = false;

        public bool WarExpired { get; set; } = false;

        public decimal DiskCapacity { get; set; } = -1;

        public decimal DiskUsing { get; set; } = -1;

        public decimal DiskUsePercent => Math.Round(DiskUsing / (DiskCapacity <= 0 ? 1 : DiskCapacity) * 100, 2);

        public bool NotEnoughSpace => DiskUsing >= DiskCapacity && DiskCapacity > 0;

        public bool WarrningSpace => DiskUsing >= DiskCapacity - (DiskCapacity * Constant.WarningDiskCapacity) && DiskCapacity > 0;

        public bool GiftCode { get; set; }

        public bool ShippingFee { get; set; }

        public bool PaymentOnline { get; set; }

        public bool MembershipPoints { get; set; }

        public bool SocialLogin { get; set; }

        public bool MultiLanguage { get; set; }
        
        public bool Warehouse { get; set; }
        
        public bool Dept { get; set; }

        // public bool OrderContact { get; set; }
        // public bool MessengerChat { get; set; }

        public PaymentMethod PayType;
        internal bool customerLogin;

        public Func<bool> CanNotUseWeb => () =>
        {
            return IsExpired || NotEnoughSpace;
        };
        
        
        public List<string> PackageOther()
        {
            return new List<string>{
                "web_shop_default",
                "web_news",
            };
        }
    
        public bool IsPackageOther(string? pack = null)
        {
            return PackageOther().Contains((pack ?? WebPackageKey)!);
        }

        public bool IsDefaultShop()
        {
            return WebPackageKey == "web_shop_default";
        }

        public bool IsNewsSite()
        {
            return WebPackageKey == "web_news";
        }
    }
}