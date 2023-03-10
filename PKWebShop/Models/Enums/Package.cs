namespace PKWebShop.Enums
{
    using Inner.Libs.Helpful;

    // Enum for status of package customer can use
    public enum ActiveStatus
    {
        UNKNOWN,

        [EnumAttr("active", "Đang sử dụng")]
        Active,

        [EnumAttr("pending", "Chưa kích hoạt")]
        Pending,

        [EnumAttr("in_active", "Dừng sử dụng")]
        InActive,

        [EnumAttr("expiration", "Hết hạn sử dụng")]
        Expiration,
    }

    // Enum for package web 
    public enum WebPackageType
    {
        UNKNOWN,

        // Web package
        [EnumAttr("web_basic", "Gói cơ bản")]
        WEB_PACKAGE_BASIC,

        [EnumAttr("web_standard", "Gói tiêu chuẩn")]
        WEB_PACKAGE_STANDARD,

        [EnumAttr("web_professional", "Gói chuyên nghiệp")]
        WEB_PACKAGE_PROFESSIONAL,

        [EnumAttr("web_premium", "Gói cao cấp")]
        WEB_PACKAGE_PREMIUM,
    }

    // Enum for defined package of webpage 
    public enum PackageType
    {
        [EnumAttr("web_package", "Gói website")]
        WEB_PACKAGE,

        [EnumAttr("host_disk_size", "Dung lượng")]
        HOST_DISK_SIZE,

        [EnumAttr("social_login", "Login với Facebook/Google")]
        SOCIAL_LOGIN,

        [EnumAttr("gift_code", "Mã giảm giá")]
        GIFT_CODE,
        
        [EnumAttr("membership_points", "Tích điểm thành viên")]
        MEMBERSHIP_POINTS,

        [EnumAttr("shipping_fee", "Cài đặt phí ship")]
        SHIPPING_FEE,

        [EnumAttr("payment_online", "Thanh toán Online")]
        PAYMENT_ONLINE,

        [EnumAttr("warehouse", "Quản lý kho")]
        WAREHOUSE,

        [EnumAttr("dept", "Quản lý công nợ")]
        DEPT,

        /*
        [EnumAttr("multi_language", "Đa ngôn ngữ")]
        MULTI_LANGUAGE,

        [EnumAttr("messenger_chat", "Liên hệ messenger")]
        MESSENGER_CHAT,

        [EnumAttr("order_contact", "Nút liên hệ đặt hàng")]
        ORDER_CONTACT,
        */
    }
    public enum PaymentStatus
    {
        [EnumAttr("paid_a_part", "Thanh toán 1 phần")]
        PaidAPart,
        [EnumAttr("fully_part", "Thanh toán toàn bộ")]
        FullyPaid,
    }

    public enum MCodeType
    {
        [EnumAttr("payment_type", "")]
        PAYMENT_TYPE,
    }

    public enum PaymentMethod
    {
        [EnumAttr("cod", "COD")]
        COD,
        [EnumAttr("vnpay", "VNPAY")]
        VNPAY,
        [EnumAttr("transfer", "Chuyển khoản")]
        TRANSFER,
        [EnumAttr("other", "Hình thức khác")]
        OTHER,
    }
    public enum ImportTicket_PaymentStatus
    {
        [EnumAttr("Phiếu tạm")]
        Unpaid = 0,
        [EnumAttr("Thanh toán 1 phần")]
        PaidAPart = 1,
        [EnumAttr("Thanh toán hoàn tất")]
        FullyPaid = 2,
    }
    public enum ImportTicket_ImportStatus
    {
        [EnumAttr("Chưa nhận")]
        NotYetImported = 0,
        [EnumAttr("Đã nhận")]
        Imported = 1,
    }
}