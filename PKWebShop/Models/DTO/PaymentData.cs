using System.ComponentModel.DataAnnotations.Schema;
using Inner.Libs.Helpful;
using PKWebShop.Enums;

namespace PKWebShop.Models
{
  public partial class PaymentData : Payment
  {
    public string Paytype { get; set; } = PaymentMethod.COD.Code<string>();
    public string BankTransactionNo { get; set; } = "";
    public string TransactionNo { get; set; } = "";
    public long Amount { get; set; }
    public string BankCode { get; set; } = "";
    public string CardType { get; set; } = "";
    // Comment
    public string Comment { get; set; } = "";
    // ~ Order id
    public string TransactionRef { get; set; } = "";
    public string PayDate { get; set; } = "";
    public string ResponseCode { get; set; } = "";
    
    //
    public PaymentData(){}
    //
    public PaymentData(Payment payData)
    {
      CustomerId = payData.CustomerId;
      OrderId = payData.OrderId;
      CreatedAt = payData.CreatedAt;
      UpdatedAt = payData.UpdatedAt;
      Order = payData.Order;
    }

    // VNPAY 
    public PaymentData(VNP_PaymentData payData) : this((Payment)payData)
    {
      Paytype = PaymentMethod.VNPAY.Code<string>();
      BankTransactionNo = payData.vnp_BankTranNo;
      TransactionNo = payData.vnp_TransactionNo;
      Amount = payData.vnp_Amount;
      BankCode = payData.vnp_BankCode;
      CardType = payData.vnp_CardType;
      Comment = payData.vnp_OrderInfo;
      TransactionRef = payData.vnp_TxnRef;
      PayDate = payData.vnp_PayDate;
      ResponseCode = payData.vnp_ResponseCode;
    }
  }
}