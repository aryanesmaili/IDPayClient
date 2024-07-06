namespace IDPayClass
{
    public class IDPayPaymentService
    {
        private readonly string _apikey;
        private readonly bool _isTest;
        private const string _requestNewTransactionAPI = "https://api.idpay.ir/v1.1/payment";
        private const string _paymentVerificationAPI = "https://api.idpay.ir/v1.1/payment/verify";
        private const string _paymentInquiry = "https://api.idpay.ir/v1.1/payment/inquiry";
        private readonly string _CallbackAddress;

        public IDPayPaymentService(string apikey, string callbackAddress, bool isTest = false)
        {
            _CallbackAddress = callbackAddress;
            _isTest = isTest;
            _apikey = apikey;
        }
        public async Task<string> SendPaymentRequest(PaymentRequest paymentRequest)
        {

        }
    }
    public class PaymentRequest
    {
        public required string OrderId { get; set; }
        public required int Amount { get; set; }
        public string? Description { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }

        public PaymentRequest(string OderID, int amount, string? email = null, string? phoneNumber = null, string? name = null, string? description = null)
        {
            OrderId = OderID;
            Amount = amount;
            Email = email;
            PhoneNumber = phoneNumber;
            Name = name;
            Description = description;
        }
    }
}
