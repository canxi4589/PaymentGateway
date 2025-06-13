using ApiGateway.Models;
using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly VNPayService _vnpayService;
        private readonly StripeService _stripeService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(VNPayService vnpayService, StripeService stripeService, ILogger<PaymentController> logger)
        {
            _vnpayService = vnpayService;
            _stripeService = stripeService;
            _logger = logger;
        }

        // VNPay Endpoints
        [HttpPost("vnpay/create")]
        public IActionResult CreateVNPayPayment([FromBody] VNPayRequest request)
        {
            try
            {
                // Get client IP address
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                request.IpAddress = ipAddress;

                var result = _vnpayService.CreatePaymentUrl(request);
                
                if (result.Success)
                {
                    return Ok(new PaymentResponse
                    {
                        Success = true,
                        PaymentUrl = result.PaymentUrl,
                        TransactionId = result.TransactionId,
                        Provider = "VNPay",
                        Message = result.Message
                    });
                }
                
                return BadRequest(new PaymentResponse
                {
                    Success = false,
                    Message = result.Message,
                    Provider = "VNPay"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment");
                return StatusCode(500, new PaymentResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Provider = "VNPay"
                });
            }
        }

        [HttpGet("vnpay/callback")]
        public IActionResult VNPayCallback([FromQuery] VNPayCallbackRequest callback)
        {
            try
            {
                var isValid = _vnpayService.ValidateCallback(callback);
                
                if (isValid && callback.vnp_ResponseCode == "00")
                {
                    // Payment successful
                    _logger.LogInformation($"VNPay payment successful for order: {callback.vnp_TxnRef}");
                    return Ok(new
                    {
                        Success = true,
                        Message = "Payment successful",
                        OrderId = callback.vnp_TxnRef,
                        TransactionId = callback.vnp_TransactionNo,
                        Amount = callback.vnp_Amount,
                        PayDate = callback.vnp_PayDate
                    });
                }
                else
                {
                    // Payment failed
                    _logger.LogWarning($"VNPay payment failed for order: {callback.vnp_TxnRef}, Response code: {callback.vnp_ResponseCode}");
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Payment failed or invalid",
                        OrderId = callback.vnp_TxnRef,
                        ResponseCode = callback.vnp_ResponseCode
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay callback");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        // Stripe Endpoints
        [HttpPost("stripe/create")]
        public async Task<IActionResult> CreateStripePayment([FromBody] StripePaymentRequest request)
        {
            try
            {
                var result = await _stripeService.CreatePaymentSessionAsync(request);
                
                if (result.Success)
                {
                    return Ok(new PaymentResponse
                    {
                        Success = true,
                        PaymentUrl = result.PaymentUrl,
                        SessionId = result.SessionId,
                        Provider = "Stripe",
                        Message = result.Message
                    });
                }
                
                return BadRequest(new PaymentResponse
                {
                    Success = false,
                    Message = result.Message,
                    Provider = "Stripe"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe payment");
                return StatusCode(500, new PaymentResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Provider = "Stripe"
                });
            }
        }

        [HttpGet("stripe/session/{sessionId}")]
        public async Task<IActionResult> GetStripeSession(string sessionId)
        {
            try
            {
                var session = await _stripeService.GetSessionAsync(sessionId);
                
                if (session != null)
                {
                    return Ok(new
                    {
                        Success = true,
                        SessionId = session.Id,
                        PaymentStatus = session.PaymentStatus,
                        CustomerEmail = session.CustomerEmail,
                        AmountTotal = session.AmountTotal,
                        Currency = session.Currency,
                        PaymentIntentId = session.PaymentIntentId
                    });
                }
                
                return NotFound(new { Success = false, Message = "Session not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving Stripe session: {sessionId}");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpGet("stripe/config")]
        public IActionResult GetStripeConfig()
        {
            try
            {
                return Ok(new
                {
                    PublishableKey = _stripeService.GetPublishableKey()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Stripe config");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        // Generic payment endpoint for unified interface
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentRequest request, [FromQuery] string provider = "vnpay")
        {
            try
            {
                switch (provider.ToLower())
                {
                    case "vnpay":
                        var vnpayRequest = new VNPayRequest
                        {
                            Amount = request.Amount,
                            OrderId = request.OrderId,
                            OrderInfo = request.Description,
                            ReturnUrl = request.ReturnUrl,
                            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
                        };
                        
                        var vnpayResult = _vnpayService.CreatePaymentUrl(vnpayRequest);
                        return Ok(new PaymentResponse
                        {
                            Success = vnpayResult.Success,
                            PaymentUrl = vnpayResult.PaymentUrl,
                            TransactionId = vnpayResult.TransactionId,
                            Provider = "VNPay",
                            Message = vnpayResult.Message
                        });

                    case "stripe":
                        var stripeRequest = new StripePaymentRequest
                        {
                            Amount = (long)(request.Amount * 100), // Convert to cents
                            Currency = request.Currency.ToLower(),
                            Description = request.Description,
                            CustomerEmail = request.CustomerEmail,
                            SuccessUrl = request.ReturnUrl,
                            CancelUrl = request.CancelUrl
                        };
                        
                        var stripeResult = await _stripeService.CreatePaymentSessionAsync(stripeRequest);
                        return Ok(new PaymentResponse
                        {
                            Success = stripeResult.Success,
                            PaymentUrl = stripeResult.PaymentUrl,
                            SessionId = stripeResult.SessionId,
                            Provider = "Stripe",
                            Message = stripeResult.Message
                        });

                    default:
                        return BadRequest(new PaymentResponse
                        {
                            Success = false,
                            Message = "Unsupported payment provider",
                            Provider = provider
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating payment with provider: {provider}");
                return StatusCode(500, new PaymentResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Provider = provider
                });
            }
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Services = new
                {
                    VNPay = "Available",
                    Stripe = "Available"
                }
            });
        }
    }
} 