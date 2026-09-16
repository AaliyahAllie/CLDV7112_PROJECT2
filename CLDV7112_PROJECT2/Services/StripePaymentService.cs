using Stripe;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT2.Services
{
    /// <summary>
    /// Service managing Stripe PaymentIntent creation and payment verification.
    /// Integrates Stripe payment gateway with ASP.NET Core web storefront.
    /// </summary>
    public class StripePaymentService
    {
        // Creates a PaymentIntent and returns client_secret for browser payment processing
        public async Task<PaymentIntent> CreatePaymentIntentAsync(long amountInCents, string currency = "zar")
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = currency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                }
            };
            var service = new PaymentIntentService();
            return await service.CreateAsync(options);
        }

        // Retrieves a PaymentIntent to verify successful card payment confirmation
        public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
        {
            var service = new PaymentIntentService();
            return await service.GetAsync(paymentIntentId);
        }
    }
}
