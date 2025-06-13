using ApiGateway.Models;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ApiGateway.Services
{
    public class VNPayService
    {
        private readonly IConfiguration _configuration;
        private readonly string _vnpUrl;
        private readonly string _vnpReturnUrl;
        private readonly string _vnpTmnCode;
        private readonly string _vnpHashSecret;

        public VNPayService(IConfiguration configuration)
        {
            _configuration = configuration;
            _vnpUrl = _configuration["VNPay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _vnpReturnUrl = _configuration["VNPay:ReturnUrl"] ?? "http://localhost:5000/api/payment/vnpay/callback";
            _vnpTmnCode = _configuration["VNPay:TmnCode"] ?? "";
            _vnpHashSecret = _configuration["VNPay:HashSecret"] ?? "";
        }

        public VNPayResponse CreatePaymentUrl(VNPayRequest request)
        {
            try
            {
                var vnpParams = new Dictionary<string, string>
                {
                    {"vnp_Version", "2.1.0"},
                    {"vnp_Command", "pay"},
                    {"vnp_TmnCode", _vnpTmnCode},
                    {"vnp_Amount", ((long)(request.Amount * 100)).ToString()},
                    {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")},
                    {"vnp_CurrCode", "VND"},
                    {"vnp_IpAddr", request.IpAddress},
                    {"vnp_Locale", "vn"},
                    {"vnp_OrderInfo", request.OrderInfo},
                    {"vnp_OrderType", "other"},
                    {"vnp_ReturnUrl", _vnpReturnUrl},
                    {"vnp_TxnRef", request.OrderId}
                };

                var queryString = BuildQueryString(vnpParams);
                var secureHash = CreateSecureHash(queryString, _vnpHashSecret);
                var paymentUrl = $"{_vnpUrl}?{queryString}&vnp_SecureHash={secureHash}";

                return new VNPayResponse
                {
                    Success = true,
                    PaymentUrl = paymentUrl,
                    TransactionId = request.OrderId,
                    Message = "Payment URL created successfully"
                };
            }
            catch (Exception ex)
            {
                return new VNPayResponse
                {
                    Success = false,
                    Message = $"Error creating payment URL: {ex.Message}"
                };
            }
        }

        public bool ValidateCallback(VNPayCallbackRequest callback)
        {
            try
            {
                var vnpParams = new Dictionary<string, string>
                {
                    {"vnp_Amount", callback.vnp_Amount},
                    {"vnp_BankCode", callback.vnp_BankCode},
                    {"vnp_BankTranNo", callback.vnp_BankTranNo},
                    {"vnp_CardType", callback.vnp_CardType},
                    {"vnp_OrderInfo", callback.vnp_OrderInfo},
                    {"vnp_PayDate", callback.vnp_PayDate},
                    {"vnp_ResponseCode", callback.vnp_ResponseCode},
                    {"vnp_TmnCode", callback.vnp_TmnCode},
                    {"vnp_TransactionNo", callback.vnp_TransactionNo},
                    {"vnp_TransactionStatus", callback.vnp_TransactionStatus},
                    {"vnp_TxnRef", callback.vnp_TxnRef}
                };

                var queryString = BuildQueryString(vnpParams);
                var secureHash = CreateSecureHash(queryString, _vnpHashSecret);

                return secureHash.Equals(callback.vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private string BuildQueryString(Dictionary<string, string> parameters)
        {
            var sortedParams = parameters
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .OrderBy(p => p.Key)
                .ToList();

            var queryString = string.Join("&", sortedParams.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"));
            return queryString;
        }

        private string CreateSecureHash(string data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hash = hmac.ComputeHash(dataBytes);
                return Convert.ToHexString(hash).ToLower();
            }
        }
    }
} 