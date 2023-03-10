using System.ComponentModel.DataAnnotations.Schema;

namespace AdminPage.Models
{
    public partial class VNP_PaymentData : Payment
    {
        public string vnp_BankTranNo { get; set; } = null!;
        // ~ Order id
        public string vnp_TxnRef { get; set; } = null!;
        public string vnp_TransactionNo { get; set; } = null!;

        public long vnp_Amount { get; set; }
        public string vnp_BankCode { get; set; } = null!;
        public string vnp_CardType { get; set; } = null!;
        // Comment
        public string vnp_OrderInfo { get; set; } = null!;
        public string vnp_PayDate { get; set; } = null!;
        public string vnp_ResponseCode { get; set; } = null!;
        public string vnp_SecureHashType { get; set; } = null!;
    }
}