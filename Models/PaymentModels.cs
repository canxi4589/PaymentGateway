namespace ApiGateway.Models
{
    // Common payment request model
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string OrderId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }

    // VNPay specific models
    public class VNPayRequest
    {
        public decimal Amount { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string OrderInfo { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }

    public class VNPayResponse
    {
        public bool Success { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class VNPayCallbackRequest
    {
        public string vnp_Amount { get; set; } = string.Empty;
        public string vnp_BankCode { get; set; } = string.Empty;
        public string vnp_BankTranNo { get; set; } = string.Empty;
        public string vnp_CardType { get; set; } = string.Empty;
        public string vnp_OrderInfo { get; set; } = string.Empty;
        public string vnp_PayDate { get; set; } = string.Empty;
        public string vnp_ResponseCode { get; set; } = string.Empty;
        public string vnp_TmnCode { get; set; } = string.Empty;
        public string vnp_TransactionNo { get; set; } = string.Empty;
        public string vnp_TransactionStatus { get; set; } = string.Empty;
        public string vnp_TxnRef { get; set; } = string.Empty;
        public string vnp_SecureHash { get; set; } = string.Empty;
    }

    // Stripe specific models
    public class StripePaymentRequest
    {
        public long Amount { get; set; } // Amount in cents
        public string Currency { get; set; } = "usd";
        public string Description { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class StripePaymentResponse
    {
        public bool Success { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    // Generic payment response
    public class PaymentResponse
    {
        public bool Success { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
    }
} 