﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Routing;
using Inner.Libs.Helpful;
using PKWebShop.AppLB;
using PKWebShop.Enums;
using PKWebShop.Models;
using PKWebShop.Models.DTO;
using PKWebShop.Utils;

namespace PKWebShop.Services
{
    public class PackageServices : ServicesBase
    {
        static bool fixedset = true;

        public PackageInfo WebPackInfo()
        {
            // var currentMem = Authority.GetThisUser();
            var ternantId = DB.webconfigurations.FirstOrDefault().Id;
            PackageInfo info = new PackageInfo();
            // Get package active and still effective
            var today = DateTime.Today;
            var packageValid = DB.Package.AsEnumerable()
              .Where(pac => pac.TenantId == ternantId)
              .Where(pac => pac.Status == ActiveStatus.Active.Code<string>())
              .Where(pac => pac.EffectiveDate!.Value <= today && today <= pac.ExpirationDate!.Value).ToList();
            var webPackActive = packageValid.FirstOrDefault(wp => wp.PackageType == PackageType.WEB_PACKAGE.Code<string>());
            if (webPackActive == null || info.IsPackageOther(webPackActive.Code))
            {
                info.WebPackageKey = webPackActive?.Code;
                return info;
            }
            // Web package regist
            var packageBaseSetting = DB.PackageSetting.FirstOrDefault(pac => pac.WebPackage == webPackActive.Code);

            // Web package info
            FillWebPacInfo(ref info, webPackActive);
            // Disk package info
            FillDiskCapacityInfo(ref info, packageValid, packageBaseSetting!);
            // Other feature
            FillOtherFeature(ref info, packageValid, packageBaseSetting!);

            if (fixedset)
            {
                info.GiftCode = false;
                info.ShippingFee = false;
                info.PaymentOnline = false;
                info.MembershipPoints = false;
                info.SocialLogin = false;
                info.MultiLanguage = false;
                info.Warehouse = false;
                info.Dept = false;
            }

            return info;
        }
        public PackageInfo packageInfoSS()
        {
            var rs = HttpContext.Current.Session["webpack"] = HttpContext.Current.Session["webpack"] ?? WebPackInfo();
            return rs as PackageInfo;
        }
        private void FillWebPacInfo(ref PackageInfo info, Package webPackActive)
        {
            // Web package info
            info.WebPackage = Ext.EnumParse<WebPackageType>(webPackActive.Code).Text();
            info.WebPackageKey = webPackActive.Code;
            info.WarExpired = (int)(webPackActive.ExpirationDate!.Value.Date - DateTime.Today.Date).TotalDays <= Constant.WarningDate;
        }

        private void FillDiskCapacityInfo(ref PackageInfo info, List<Package> packageValid, PackageSetting packageBaseSetting)
        {
            // var baseDisk = packageBaseSetting.DiskSize;
            var addMoreDisk = packageValid.Where(pac => pac.PackageType == PackageType.HOST_DISK_SIZE.Code<string>()).Sum(pac => pac.Value) ?? 0;
            info.DiskCapacity = addMoreDisk;
            info.DiskUsing = AppFunc.CountDiskSizeUsing();
        }

        private void FillOtherFeature(ref PackageInfo info, List<Package> packageValid, PackageSetting baseSetting)
        {
            info.GiftCode = baseSetting.GiftCode || packageValid.Any(pac => pac.PackageType == PackageType.GIFT_CODE.Code<string>());
            info.ShippingFee = baseSetting.ShippingFee || packageValid.Any(pac => pac.PackageType == PackageType.SHIPPING_FEE.Code<string>());
            info.PaymentOnline = baseSetting.PaymentOnline || packageValid.Any(pac => pac.PackageType == PackageType.PAYMENT_ONLINE.Code<string>());
            if (info.PaymentOnline)
            {
                info.PayType = Ext.EnumParse<PaymentMethod>(packageValid.FirstOrDefault(pac => pac.PackageType == PackageType.PAYMENT_ONLINE.Code<string>())?.Code ?? PaymentMethod.COD.Code<string>());
            }
            info.MembershipPoints = baseSetting.MembershipPoints || packageValid.Any(pac => pac.PackageType == PackageType.MEMBERSHIP_POINTS.Code<string>());
            info.SocialLogin = baseSetting.SocialLogin || packageValid.Any(pac => pac.PackageType == PackageType.SOCIAL_LOGIN.Code<string>());
            info.Warehouse = baseSetting.Warehouse || packageValid.Any(pac => pac.PackageType == PackageType.WAREHOUSE.Code<string>());
            info.Dept = baseSetting.Dept || packageValid.Any(pac => pac.PackageType == PackageType.DEPT.Code<string>());
            // info.MultiLanguage = baseSetting.MultiLanguage || packageValid.Any(pac => pac.PackageType == PackageType.MULTI_LANGUAGE.Code<string>());
            // info.MessengerChat = baseSetting.MessengerChat || packageValid.Any(pac => pac.PackageType == PackageType.MESSENGER_CHAT.Code<string>());
            // info.OrderContact = baseSetting.OrderContact || packageValid.Any(pac => pac.PackageType == PackageType.ORDER_CONTACT.Code<string>());
        }

        public Dictionary<string, List<Package>> PackageAvaiable()
        {
            var tenantId = DB.webconfigurations.FirstOrDefault()?.Id ?? "1";
            var today = DateTime.Today;
            var packageValid = DB.Package.AsEnumerable()
              .Where(pac => pac.TenantId == tenantId)
              .Where(pac => pac.Status != ActiveStatus.InActive.Code<string>())
              // .Where(pac => pac.EffectiveDate <= today && today <= pac.ExpirationDate)
              .GroupBy(pac => pac.PackageType).ToDictionary(k => k.Key, v => v.ToList());
            return packageValid;
        }

    }
}