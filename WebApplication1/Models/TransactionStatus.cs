namespace WebApplication1.Models
{
    public enum TransactionStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4,
        Refunded = 5,
        PartiallyRefunded = 6,
        Expired = 7
    }

    public enum TransactionType
    {
        Recharge = 0,
        Usage = 1,
        Refund = 2,
        Bonus = 3,
        Penalty = 4,
        Adjustment = 5
    }

    public enum PaymentMethod
    {
        CreditCard = 0,
        PayPal = 1,
        BankTransfer = 2,
        Cryptocurrency = 3,
        GiftCard = 4,
        Wallet = 5,
        
        // Vietnamese Payment Methods
        MoMo = 10,
        ZaloPay = 11,
        VNPay = 12,
        OnePay = 13,
        ShopeePay = 14,
        
        // Banking Gateways
        VietcomBank = 20,
        TechcomBank = 21,
        BIDV = 22,
        VietinBank = 23,
        ACB = 24,
        TPBank = 25,
        MB = 26,
        Sacombank = 27,
        
        // International Banking
        Visa = 30,
        MasterCard = 31,
        JCB = 32,
        AmericanExpress = 33
    }

    public enum PaymentGateway
    {
        Stripe = 0,
        PayPal = 1,
        
        // Vietnamese Gateways
        MoMo = 10,
        ZaloPay = 11,
        VNPay = 12,
        OnePay = 13,
        ShopeePay = 14,
        
        // Banking Gateways
        VietQR = 20,
        InternetBanking = 21,
        ATMCard = 22,
        
        // Manual/Offline
        BankTransfer = 30,
        Cash = 31
    }

    public enum Currency
    {
        USD = 0,
        VND = 1,
        EUR = 2,
        JPY = 3,
        CNY = 4,
        THB = 5,
        SGD = 6
    }
}
