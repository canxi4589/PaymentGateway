using ApiGateway.Models;
using Stripe;
using Stripe.Checkout;

namespace ApiGateway.Services
{
    public class StripeService
    {
        private readonly IConfiguration _configuration;
        private readonly string _stripeSecretKey;
        private readonly string _stripePublishableKey;

        public StripeService(IConfiguration configuration)
        {
            _configuration = configuration;
            _stripeSecretKey = _configuration["Stripe:SecretKey"] ?? "";
            _stripePublishableKey = _configuration["Stripe:PublishableKey"] ?? "";
            
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        public async Task<StripePaymentResponse> CreatePaymentSessionAsync(StripePaymentRequest request)
        {
            try
            {
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = request.Amount,
                                Currency = request.Currency,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Payment",
                                    Description = request.Description,
                                },
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    CustomerEmail = request.CustomerEmail,
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return new StripePaymentResponse
                {
                    Success = true,
                    SessionId = session.Id,
                    PaymentUrl = session.Url,
                    Message = "Payment session created successfully"
                };
            }
            catch (Exception ex)
            {
                return new StripePaymentResponse
                {
                    Success = false,
                    Message = $"Error creating payment session: {ex.Message}"
                };
            }
        }

        public async Task<Session?> GetSessionAsync(string sessionId)
        {
            try
            {
                var service = new SessionService();
                return await service.GetAsync(sessionId);
            }
            catch
            {
                return null;
            }
        }

        public async Task<PaymentIntent?> GetPaymentIntentAsync(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                return await service.GetAsync(paymentIntentId);
            }
            catch
            {
                return null;
            }
        }

        public string GetPublishableKey()
        {
            return _stripePublishableKey;
        }
    }
} 