using System.Net;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace IDPayClass
{
    /// <summary>
    /// Provides methods for interacting with the IDPay payment gateway.
    /// </summary>
    public class IDPayClient
    {
        private readonly string _apiKey = string.Empty;
        private readonly bool _isTest;
        private readonly string _requestNewTransactionAPI;
        private readonly string _paymentVerificationAPI;
        private readonly string _paymentInquiryAPI;
        private readonly string _transactionsListAPI;

        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="IDPayClient"/> class.
        /// </summary>
        /// <param name="apiKey">The API key used for authenticating with IDPay.</param>
        /// <param name="httpClient">An instance of <see cref="HttpClient"/> to send HTTP requests.</param>
        /// <param name="isTest">Indicates whether the service operates in sandbox (test) mode.</param>
        public IDPayClient(IDPayConfig config, HttpClient httpClient)
        {
            _isTest = config.IsTest;
            _apiKey = config.ApiKey;
            _httpClient = httpClient;
            _requestNewTransactionAPI = config.RequestNewTransactionAPI;
            _paymentVerificationAPI = config.PaymentVerificationAPI;
            _paymentInquiryAPI = config.PaymentInquiryAPI;
            _transactionsListAPI = config.TransactionsListAPI;

            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
            if (_isTest) // If we're in test mode.
                _httpClient.DefaultRequestHeaders.Add("X-SANDBOX", "1");
            _httpClient = httpClient;
        }

        /// <summary>
        /// Sends a payment request to the IDPay payment gateway.
        /// </summary>
        /// <param name="requestInfo">The payment request information.</param>
        /// <returns>
        /// a <see cref="CreatePaymentResponse"/> object with the payment response details.
        /// </returns>
        public async Task<CreatePaymentResponse> RequestPaymentAsync(CreatePaymentRequest requestInfo)
        {
            HttpRequestMessage request = new(HttpMethod.Post, _requestNewTransactionAPI)
            { Content = new StringContent(JsonSerializer.Serialize(requestInfo), Encoding.UTF8, "application/json") };

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            CreatePaymentResponse? paymentResponse;
            paymentResponse = await JsonSerializer.DeserializeAsync<CreatePaymentResponse>(await response.Content.ReadAsStreamAsync());

            paymentResponse!.Success = response.StatusCode == HttpStatusCode.Created;

            return paymentResponse;
        }

        /// <summary>
        /// Verifies a transaction with the IDPay payment gateway.
        /// </summary>
        /// <param name="requestInfo">The transaction verification request information.</param>
        /// <returns>
        /// a <see cref="VerifyTransactionResponse"/> object with the transaction verification response details.
        /// </returns>
        public async Task<VerifyTransactionResponse> VerifyTransactionAsync(VerifyTransactionRequest requestInfo)
        {
            HttpRequestMessage request = new(HttpMethod.Post, _paymentVerificationAPI)
            { Content = new StringContent(JsonSerializer.Serialize(requestInfo), Encoding.UTF8, "application/json") };

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            VerifyTransactionResponse? paymentResponse;
            paymentResponse = await JsonSerializer.DeserializeAsync<VerifyTransactionResponse>(await response.Content.ReadAsStreamAsync());

            return paymentResponse!;
        }

        /// <summary>
        /// Retrieves the status of a specific transaction from the IDPay payment gateway.
        /// </summary>
        /// <param name="requestInfo">The transaction inquiry request information.</param>
        /// <returns>
        /// a <see cref="TransactionInquiryResponse"/> object with the transaction status details.
        /// </returns>
        public async Task<TransactionInquiryResponse> CheckTransactionStatusAsync(TransactionInquiryRequest requestInfo)
        {
            HttpRequestMessage request = new(HttpMethod.Post, _paymentInquiryAPI)
            { Content = new StringContent(JsonSerializer.Serialize(requestInfo), Encoding.UTF8, "application/json") };

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            TransactionInquiryResponse? paymentResponse;
            paymentResponse = await JsonSerializer.DeserializeAsync<TransactionInquiryResponse>(await response.Content.ReadAsStreamAsync());

            return paymentResponse!;
        }

        /// <summary>
        /// Retrieves a list of transactions from the IDPay payment gateway.
        /// </summary>
        /// <param name="requestInfo">The transaction list request information.</param>
        /// <returns>
        /// The result contains a <see cref="TransactionListResponse"/> object with the list of transactions.
        /// </returns>
        public async Task<TransactionListResponse> GetTransactionListAsync(TransactionListRequest requestInfo)
        {
            HttpRequestMessage request = new(HttpMethod.Post, _transactionsListAPI)
            { Content = new StringContent(JsonSerializer.Serialize(requestInfo), Encoding.UTF8, "application/json") };

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            TransactionListResponse? paymentResponse;
            paymentResponse = await JsonSerializer.DeserializeAsync<TransactionListResponse>(await response.Content.ReadAsStreamAsync());

            return paymentResponse!;
        }
    }

    /// <summary>
    /// this class holds the configuration that the class works by.
    /// </summary>
    public class IDPayConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public bool IsTest { get; set; } = false;
        public string RequestNewTransactionAPI { get; set; } = "https://api.idpay.ir/v1.1/payment";
        public string PaymentVerificationAPI { get; set; } = "https://api.idpay.ir/v1.1/payment/verify";
        public string PaymentInquiryAPI { get; set; } = "https://api.idpay.ir/v1.1/payment/inquiry";
        public string TransactionsListAPI { get; set; } = "https://api.idpay.ir/v1.1/payment/transactions";
    }

    /// <summary>
    /// Represents an object containing the info required to request a transaction from IDPay
    /// </summary>
    public class CreatePaymentRequest()
    {
        private string orderId = string.Empty;
        private string? description;
        private string? name;
        private string? phoneNumber;
        private string? email;
        private string callBackAddress = string.Empty;
        private int amount;

        /// <summary>
        /// Gets or sets the unique identifier for the order.
        /// Will throw <see cref="ArgumentException"/> if the length of the input is more than 50 characters.
        /// </summary>
        public required string OrderId
        {
            get => orderId;
            set
            {
                if (value.Length > 50)
                    throw new ArgumentException($"Entered value is more than 255 characters.");
                orderId = value.Trim();
            }
        }

        /// <summary>
        /// Gets or sets the amount for the payment. 
        /// Will throw <see cref="ArgumentException"/> if the value is more than 500_000_000 or less than 1_000.
        /// </summary>
        public required int Amount
        {
            get => amount;
            set
            {
                if (value > 500_000_000 || value < 1_000)
                    throw new ArgumentException($"Amount {value} is not in valid range (1,000 to 500,000,000 Rials).");
                amount = value;
            }
        }

        /// <summary>
        /// Gets or sets the description of the payment.
        /// Will throw <see cref="ArgumentException"/> if the length of the input is more than 255 characters.
        /// </summary>
        public string? Description
        {
            get => description;
            set
            {
                if (value?.Length > 255)
                    throw new ArgumentException($"Entered value is more than 255 characters.");
                description = value?.Trim();
            }
        }

        /// <summary>
        /// Gets or sets the name of the payer. 
        /// Will throw <see cref="ArgumentException"/> if the length of the input is more than 255 characters.
        /// </summary>
        public string? Name
        {
            get => name;
            set
            {
                if (value?.Length > 255)
                    throw new ArgumentException($"Entered value is more than 255 characters.");
                name = value?.Trim();
            }
        }

        /// <summary>
        /// Gets or sets the phone number of the payer.
        /// Will throw <see cref="ArgumentException"/> if the length of the input is more than 11 characters.
        /// </summary>
        public string? PhoneNumber
        {
            get => phoneNumber;
            set
            {
                if (value?.Length > 11)
                    throw new ArgumentException($"Entered value is more than 11 characters.");
                phoneNumber = value?.Trim();
            }
        }

        /// <summary>
        /// Gets or sets the email of the payer. 
        /// Will throw <see cref="ArgumentException"/> if the length of the input is more than 255 characters.
        /// </summary>
        public string? Email
        {
            get => email;
            set
            {
                if (value?.Length > 255)
                    throw new ArgumentException($"Entered value is more than 255 characters.");
                email = value?.Trim();
            }
        }

        /// <summary>
        /// The Callback address that user Will be redirected to after the transaction is done. 
        /// Will throw <see cref="ArgumentException"/> if the length of the input is more than 2048 characters.
        /// </summary>
        public required string CallbackAddress
        {
            get => callBackAddress;
            set
            {
                if (value.Length > 2048)
                    throw new ArgumentException($"Entered value is more than 255 characters.");
                callBackAddress = value.Trim();
            }
        }
    }

    /// <summary>
    /// Represents IDPay's Response to requesting payment.
    /// </summary>
    public class CreatePaymentResponse
    {
        private string transactionID = string.Empty;
        private string paymentLink = string.Empty;
        private string errorMessage = string.Empty;

        /// <summary>
        /// Indicates Operation's status.
        /// </summary>
        [JsonIgnore]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or Sets the Transaction ID Generated by IDPay.
        /// </summary>
        [JsonPropertyName("id")]
        public string TransactionID
        {
            get => transactionID;
            set => transactionID = value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Gets or Sets the Link to Payment Webpage.
        /// </summary>
        [JsonPropertyName("link")]
        public string PaymentLink
        {
            get => paymentLink;
            set => paymentLink = value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Gets or Sets the Error Response Code.
        /// </summary>
        [JsonPropertyName("error_code")]
        public int? ErrorCode { get; set; } // Nullable to handle absence of value.

        /// <summary>
        /// Gets or Sets the Error Message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message
        {
            get => errorMessage;
            set => errorMessage = value?.Trim() ?? string.Empty;
        }
    }

    /// <summary>
    /// placeholder for IdPay's response in Post method
    /// </summary>
    public class PostTransactionResponse
    {
        private string _id = string.Empty;
        private string _orderId = string.Empty;
        private string _cardNo = string.Empty;
        private string _hashedCardNo = string.Empty;

        /// <summary>
        /// Status of the transaction.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Tracking ID of the transaction provided by IDPay.
        /// </summary>
        [JsonPropertyName("track_id")]
        public long TrackId { get; set; }

        /// <summary>
        /// Unique key of the transaction received during transaction creation.
        /// </summary>
        public required string Id
        {
            get => _id;
            set => _id = value.Trim();
        }

        /// <summary>
        /// Order ID provided by the merchant during transaction creation.
        /// </summary>
        [JsonPropertyName("order_id")]
        public required string OrderId
        {
            get => _orderId;
            set => _orderId = value.Trim();
        }

        /// <summary>
        /// Amount registered during transaction creation.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Payer's card number in the format 123456******1234.
        /// </summary>
        [JsonPropertyName("card_no")]
        public required string CardNo
        {
            get => _cardNo;
            set => _cardNo = value.Trim();
        }

        /// <summary>
        /// Hashed payer's card number using SHA256.
        /// </summary>
        [JsonPropertyName("hashed_card_no")]
        public required string HashedCardNo
        {
            get => _hashedCardNo;
            set => _hashedCardNo = value.Trim();
        }

        /// <summary>
        /// DateTime of the transaction payment.
        /// </summary>
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// Placeholder for the request to verify a transaction.
    /// </summary>
    public class VerifyTransactionRequest
    {
        private string transactionID = string.Empty;
        private string orderID = string.Empty;

        /// <summary>
        /// Unique ID of the transaction.
        /// </summary>
        [JsonPropertyName("id")]
        public required string TransactionID { get => transactionID; set => transactionID = value.Trim(); }

        /// <summary>
        /// Order ID sent in payment creation stage.
        /// </summary>
        [JsonPropertyName("order_id")]
        public required string OrderID { get => orderID; set => orderID = value.Trim(); }
    }

    /// <summary>
    /// placeholder for verify response.
    /// </summary>
    public class VerifyTransactionResponse
    {
        private string _id = string.Empty;
        private string _orderId = string.Empty;

        /// <summary>
        /// Status of the transaction.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Tracking ID of the transaction provided by IDPay.
        /// </summary>
        [JsonPropertyName("track_id")]
        public long TrackId { get; set; }

        /// <summary>
        /// Unique key of the transaction received during transaction creation.
        /// </summary>
        public required string Id
        {
            get => _id;
            set => _id = value.Trim();
        }

        /// <summary>
        /// Order ID provided by the merchant during transaction creation.
        /// </summary>
        [JsonPropertyName("order_id")]
        public required string OrderId
        {
            get => _orderId;
            set => _orderId = value.Trim();
        }

        /// <summary>
        /// Amount registered during transaction creation.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Datetime of the transaction creation.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Payment information related to the transaction.
        /// </summary>
        public required PaymentInfo Payment { get; set; }

        /// <summary>
        /// Verification information related to the transaction.
        /// </summary>
        public required VerifyInfo Verify { get; set; }
    }

    /// <summary>
    /// Placeholder to check the status of a transaction.
    /// </summary>
    public class TransactionInquiryRequest : VerifyTransactionRequest { }

    /// <summary>
    /// Placeholder for Inquiry Response.
    /// </summary>
    public class TransactionInquiryResponse
    {
        private string _id = string.Empty;
        private string _orderId = string.Empty;

        /// <summary>
        /// Status of the transaction.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Tracking ID of the transaction provided by IDPay.
        /// </summary>
        [JsonPropertyName("track_id")]
        public long TrackId { get; set; }

        /// <summary>
        /// Unique key of the transaction received during transaction creation.
        /// </summary>
        public required string Id
        {
            get => _id;
            set => _id = value.Trim();
        }

        /// <summary>
        /// Order ID provided by the merchant during transaction creation.
        /// </summary>
        [JsonPropertyName("order_id")]
        public required string OrderId
        {
            get => _orderId;
            set => _orderId = value.Trim();
        }

        /// <summary>
        /// Amount registered during transaction creation.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Transaction fee information.
        /// </summary>
        public WageInfo? Wage { get; set; }

        /// <summary>
        /// DateTime of the transaction creation.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Payer information related to the transaction.
        /// </summary>
        public PayerInfo? Payer { get; set; }

        /// <summary>
        /// Payment information related to the transaction.
        /// </summary>
        public required PaymentInfo Payment { get; set; }

        /// <summary>
        /// Verification information related to the transaction.
        /// </summary>
        public VerifyInfo? Verify { get; set; }

        /// <summary>
        /// Settlement information related to the transaction.
        /// </summary>
        public SettlementInfo? Settlement { get; set; }
    }

    /// <summary>
    /// Represents a request to retrieve transaction list with optional filters and pagination
    /// </summary>
    public class TransactionListRequest
    {
        private string? _id;
        private string? _orderId;
        private string? _trackId;
        private string? _paymentCardNo;
        private string? _paymentHashedCardNo;

        /// <summary>
        /// Page number starting from 0 (default: 0)
        /// </summary>
        [JsonPropertyName("page")]
        public int? Page { get; set; }

        /// <summary>
        /// Number of records per page (default: 25)
        /// </summary>
        [JsonPropertyName("page_size")]
        public int? PageSize { get; set; }

        /// <summary>
        /// Transaction ID received during order creation.
        /// Will throw <see cref="ArgumentException"/> if the length of the input is invalid.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id
        {
            get => _id;
            set
            {
                if (value?.Length > 50)
                    throw new ArgumentException("Transaction ID cannot be longer than 50 characters.");
                _id = value?.Trim();
            }
        }

        /// <summary>
        /// Merchant's order ID.
        /// Will throw <see cref="ArgumentException"/> if the length of the input is more than 50 characters.
        /// </summary>
        [JsonPropertyName("order_id")]
        public string? OrderId
        {
            get => _orderId;
            set
            {
                if (value?.Length > 50)
                    throw new ArgumentException("Order ID cannot be longer than 50 characters.");
                _orderId = value?.Trim();
            }
        }

        /// <summary>
        /// Transaction amount in Rials. Must be between 1,000 and 500,000,000 Rials.
        /// Will throw <see cref="ArgumentException"/> if the value is outside valid range.
        /// </summary>
        public int? Amount
        {
            get => _amount;
            set
            {
                if (value.HasValue && (value > 500_000_000 || value < 1_000))
                    throw new ArgumentException($"Amount {value} is not in valid range (1,000 to 500,000,000 Rials).");
                _amount = value;
            }
        }
        private int? _amount;

        /// <summary>
        /// Array of transaction statuses to filter by
        /// </summary>
        public int[]? Status { get; set; }

        /// <summary>
        /// IDPay tracking ID
        /// </summary>
        [JsonPropertyName("track_id")]
        public string? TrackId
        {
            get => _trackId;
            set => _trackId = value?.Trim();
        }

        /// <summary>
        /// Payer's card number in format 123456******1234
        /// </summary>
        [JsonPropertyName("payment_card_no")]
        public string? PaymentCardNo
        {
            get => _paymentCardNo;
            set => _paymentCardNo = value?.Trim();
        }

        /// <summary>
        /// SHA256 hash of payer's card number
        /// </summary>
        [JsonPropertyName("payment_hashed_card_no")]
        public string? PaymentHashedCardNo
        {
            get => _paymentHashedCardNo;
            set => _paymentHashedCardNo = value?.Trim();
        }

        /// <summary>
        /// Payment date range filter
        /// </summary>
        [JsonPropertyName("payment_date")]
        public DateRangeFilter? PaymentDate { get; set; }

        /// <summary>
        /// Settlement date range filter
        /// </summary>
        [JsonPropertyName("settlement_date")]
        public DateRangeFilter? SettlementDate { get; set; }
    }

    /// <summary>
    /// Placeholder for TransactionListResponse.
    /// </summary>
    public class TransactionListResponse
    {
        private string _id = string.Empty;
        private string _orderId = string.Empty;

        /// <summary>
        /// Status of the transaction
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// IDPay tracking ID
        /// </summary>
        [JsonPropertyName("track_id")]
        public long TrackId { get; set; }

        /// <summary>
        /// Unique transaction ID received during creation
        /// </summary>
        public required string Id
        {
            get => _id;
            set => _id = value.Trim();
        }

        /// <summary>
        /// Order ID provided by merchant during transaction creation
        /// </summary>
        [JsonPropertyName("order_id")]
        public required string OrderId
        {
            get => _orderId;
            set => _orderId = value.Trim();
        }

        /// <summary>
        /// Transaction amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Transaction fee information
        /// </summary>
        public required WageInfo Wage { get; set; }

        /// <summary>
        /// Transaction creation date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Payer information
        /// </summary>
        public required PayerInfo Payer { get; set; }

        /// <summary>
        /// Payment information
        /// </summary>
        public required PaymentInfo Payment { get; set; }

        /// <summary>
        /// Verification information
        /// </summary>
        public required VerifyInfo Verify { get; set; }

        /// <summary>
        /// Settlement information
        /// </summary>
        public required SettlementInfo Settlement { get; set; }
    }

    /// <summary>
    /// Represents a date range filter with minimum and maximum timestamps
    /// </summary>
    public class DateRangeFilter
    {
        /// <summary>
        /// Minimum timestamp in the range
        /// </summary>
        [JsonPropertyName("min")]
        public long Min { get; set; }

        /// <summary>
        /// Maximum timestamp in the range
        /// </summary>
        [JsonPropertyName("max")]
        public long Max { get; set; }
    }

    public class PaymentInfo
    {
        private string _trackId = string.Empty;
        private string _cardNo = string.Empty;
        private string _hashedCardNo = string.Empty;

        /// <summary>
        /// Tracking ID of the payment.
        /// </summary>
        [JsonPropertyName("track_id")]
        public string TrackId
        {
            get => _trackId;
            set => _trackId = value.Trim();
        }

        /// <summary>
        /// Amount to be paid.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Payer's card number in the format 123456******1234.
        /// </summary>
        [JsonPropertyName("card_no")]
        public string CardNo
        {
            get => _cardNo;
            set => _cardNo = value.Trim();
        }

        /// <summary>
        /// Hashed payer's card number.
        /// </summary>
        [JsonPropertyName("hashed_card_no")]
        public string HashedCardNo
        {
            get => _hashedCardNo;
            set => _hashedCardNo = value.Trim();
        }

        /// <summary>
        /// DateTime of the payment.
        /// </summary>  
        public DateTime Date { get; set; }
    }

    public class VerifyInfo
    {
        /// <summary>
        /// DateTime of the transaction verification.
        /// </summary>
        public DateTime Date { get; set; }
    }

    public class WageInfo
    {
        private string _by = string.Empty;
        private string _type = string.Empty;

        /// <summary>
        /// Fee charged from (payee or payer).
        /// </summary>
        [JsonPropertyName("by")]
        public string By
        {
            get => _by;
            set => _by = value.Trim();
        }

        /// <summary>
        /// Type of fee (amount, percent, stair).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type
        {
            get => _type;
            set => _type = value.Trim();
        }

        /// <summary>
        /// Fee amount.
        /// </summary>
        public decimal Amount { get; set; }
    }

    public class PayerInfo
    {
        private string _name = string.Empty;
        private string _phone = string.Empty;
        private string _mail = string.Empty;
        private string _desc = string.Empty;

        /// <summary>
        /// Name of the payer.
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value.Trim();
        }

        /// <summary>
        /// Phone number of the payer.
        /// </summary>
        public string Phone
        {
            get => _phone;
            set => _phone = value.Trim();
        }

        /// <summary>
        /// Email of the payer.
        /// </summary>
        [JsonPropertyName("mail")]
        public string Email
        {
            get => _mail;
            set => _mail = value.Trim();
        }

        /// <summary>
        /// Description provided by the payer.
        /// </summary>
        [JsonPropertyName("desc")]
        public string Description
        {
            get => _desc;
            set => _desc = value.Trim();
        }
    }

    public class SettlementInfo
    {
        /// <summary>
        /// Tracking ID of the settlement.
        /// </summary>
        [JsonPropertyName("track_id")]
        public long TrackId { get; set; }

        /// <summary>
        /// Settlement amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// DateTime of the settlement to merchant's bank account.
        /// </summary>
        public DateTime Date { get; set; }
    }

    public class AccountInfo
    {
        private string _id = string.Empty;

        /// <summary>
        /// Account settlement tracking ID
        /// </summary>
        [JsonPropertyName("track_id")]
        public required string Id
        {
            get => _id;
            set => _id = value.Trim();
        }
    }

    public class WalletInfo
    {
        private string _id = string.Empty;

        /// <summary>
        /// Wallet settlement tracking ID
        /// </summary>
        [JsonPropertyName("track_id")]
        public required string Id
        {
            get => _id;
            set => _id = value.Trim();
        }
    }
}
