using ApiGateway.Models;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

        public VNPayService(IConfiguration configuration)
        {
            _configuration = configuration;
            _vnpUrl = _configuration["VNPay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _vnpReturnUrl = _configuration["VNPay:ReturnUrl"] ?? "http://localhost:5000/api/payment/vnpay/callback";
            _vnpTmnCode = _configuration["VNPay:TmnCode"] ?? "";
            _vnpHashSecret = _configuration["VNPay:HashSecret"] ?? "";
        }

        public VNPayResponse CreatePaymentUrl(VNPayRequest body)
        {
            string returnUrl = "http://10.0.2.2:5096/api/payment/vnpay/callback";
            ExchangRate exchangRate = new ExchangRate();
            double exchangeRate = exchangRate.GetUsdToVndExchangeRateAsync().Result;
            var AmountInUsd = Convert.ToDouble(body.Amount, CultureInfo.InvariantCulture);

            double amountInVnd = Math.Round(exchangRate.ConvertUsdToVnd(AmountInUsd, exchangeRate));

            var vnPay = new VnPayLibrary();

            vnPay.AddRequestData("vnp_Amount", (amountInVnd * 100).ToString());
            vnPay.AddRequestData("vnp_Command", "pay");
            vnPay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnPay.AddRequestData("vnp_CurrCode", "VND");
            vnPay.AddRequestData("vnp_IpAddr", "127.0.0.1");
            vnPay.AddRequestData("vnp_Locale", "vn");
            vnPay.AddRequestData("vnp_OrderInfo", body.OrderId);
            vnPay.AddRequestData("vnp_OrderType", "other");
            vnPay.AddRequestData("vnp_ReturnUrl", returnUrl);
            vnPay.AddRequestData("vnp_TmnCode", _vnpTmnCode);
            vnPay.AddRequestData("vnp_TxnRef", Guid.NewGuid().ToString());
            vnPay.AddRequestData("vnp_Version", "2.1.0");

            string paymentUrl = vnPay.CreateRequestUrl(vnp_Url, _vnpHashSecret);
            return new VNPayResponse { PaymentUrl = paymentUrl };
        }

        public bool ValidateCallback(VNPayCallbackRequest callback)
        {
            try
            {
                var vnpParams = new Dictionary<string, string>();
                
                // Only add non-empty parameters for hash validation
                if (!string.IsNullOrEmpty(callback.vnp_Amount)) vnpParams.Add("vnp_Amount", callback.vnp_Amount);
                if (!string.IsNullOrEmpty(callback.vnp_BankCode)) vnpParams.Add("vnp_BankCode", callback.vnp_BankCode);
                if (!string.IsNullOrEmpty(callback.vnp_BankTranNo)) vnpParams.Add("vnp_BankTranNo", callback.vnp_BankTranNo);
                if (!string.IsNullOrEmpty(callback.vnp_CardType)) vnpParams.Add("vnp_CardType", callback.vnp_CardType);
                if (!string.IsNullOrEmpty(callback.vnp_OrderInfo)) vnpParams.Add("vnp_OrderInfo", callback.vnp_OrderInfo);
                if (!string.IsNullOrEmpty(callback.vnp_PayDate)) vnpParams.Add("vnp_PayDate", callback.vnp_PayDate);
                if (!string.IsNullOrEmpty(callback.vnp_ResponseCode)) vnpParams.Add("vnp_ResponseCode", callback.vnp_ResponseCode);
                if (!string.IsNullOrEmpty(callback.vnp_TmnCode)) vnpParams.Add("vnp_TmnCode", callback.vnp_TmnCode);
                if (!string.IsNullOrEmpty(callback.vnp_TransactionNo)) vnpParams.Add("vnp_TransactionNo", callback.vnp_TransactionNo);
                if (!string.IsNullOrEmpty(callback.vnp_TransactionStatus)) vnpParams.Add("vnp_TransactionStatus", callback.vnp_TransactionStatus);
                if (!string.IsNullOrEmpty(callback.vnp_TxnRef)) vnpParams.Add("vnp_TxnRef", callback.vnp_TxnRef);

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

        private string RemoveVietnameseDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Remove Vietnamese diacritics and special characters
            var vietnameseMap = new Dictionary<string, string>
            {
                {"àáạảãâầấậẩẫăằắặẳẵ", "a"},
                {"ÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴ", "A"},
                {"èéẹẻẽêềếệểễ", "e"},
                {"ÈÉẸẺẼÊỀẾỆỂỄ", "E"},
                {"ìíịỉĩ", "i"},
                {"ÌÍỊỈĨ", "I"},
                {"òóọỏõôồốộổỗơờớợởỡ", "o"},
                {"ÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠ", "O"},
                {"ùúụủũưừứựửữ", "u"},
                {"ÙÚỤỦŨƯỪỨỰỬỮ", "U"},
                {"ỳýỵỷỹ", "y"},
                {"ỲÝỴỶỸ", "Y"},
                {"đ", "d"},
                {"Đ", "D"}
            };

            foreach (var mapping in vietnameseMap)
            {
                foreach (char vietnameseChar in mapping.Key)
                {
                    text = text.Replace(vietnameseChar, mapping.Value[0]);
                }
            }

            // Remove special characters but keep alphanumeric, spaces, and basic punctuation
            text = Regex.Replace(text, @"[^\w\s\.\-]", "");
            
            return text.Trim();
        }
    }
} 