using System.Net;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using static Pexita.Utility.Exceptions.PaymentException;
using System.Net.Http;

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
        private static readonly HttpClient _httpClient = new();

        public IDPayPaymentService(string apikey, string callbackAddress, bool isTest = false)
        {
            _CallbackAddress = callbackAddress;
            _isTest = isTest;
            _apikey = apikey;
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apikey);

            // If in test mode, add a header to indicate sandbox mode.
            if (_isTest)
            {
                _httpClient.DefaultRequestHeaders.Add("X-SANDBOX", "1");
            }
        }

        /// <summary>
        /// starts the process by asking IDPay for a payment instance synchronously by using the info provided
        /// </summary>
        /// <param name="paymentRequestInfo"></param>
        /// <returns>Transaction ID and Payment Webpage Link if successful, throws relevant Exception otherwise. </returns>
        /// <exception cref="Exception"></exception>
        public PaymentRequestResponse SendPaymentRequest(PaymentRequest paymentRequestInfo)
        {

            // Prepare the request body as a JSON object.
            Dictionary<string, dynamic> requestBody = new()
            {
                { "order_id", paymentRequestInfo.OrderId },
                { "amount", paymentRequestInfo.Amount },
                { "name", !string.IsNullOrEmpty(paymentRequestInfo.Name) ? paymentRequestInfo.Name : "" },
                { "phone", !string.IsNullOrEmpty(paymentRequestInfo.PhoneNumber) && IsPhoneNumber(paymentRequestInfo.PhoneNumber) ? paymentRequestInfo.PhoneNumber : "" },
                { "mail", !string.IsNullOrEmpty(paymentRequestInfo.Email) && IsEmail(paymentRequestInfo.Email) ? paymentRequestInfo.Email : "" },
                { "desc", paymentRequestInfo.Description ?? "" },
                { "callback", _CallbackAddress }
            };
            string requestBodyJson = JsonSerializer.Serialize(requestBody);

            // Create the HTTP content from the request body.
            HttpContent content = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

            // Send a POST request to the IDPay API to create a new transaction.
            HttpResponseMessage response = _httpClient.PostAsync(_requestNewTransactionAPI, content).GetAwaiter().GetResult();

            // Check the status code of the response.
            if (response.StatusCode == HttpStatusCode.Created) // 201   // means transaction has successfully been created 
            {
                // Deserialize the response JSON to extract the payment creation success details.
                PaymentCreationSuccessResponse? deserializedResponse = JsonSerializer.Deserialize<PaymentCreationSuccessResponse>(response.Content.ReadAsStream());

                PaymentRequestResponse payment_response = new(deserializedResponse.Id, deserializedResponse.Link);

                // Return the payment link to redirect the user to the payment page.
                return payment_response;
            }
            else
            {
                // If the request was not successful, deserialize the error response and handle it.
                var errorResponse = JsonSerializer.Deserialize<IDPayErrorResponse>(response.Content.ReadAsStream());
                PaymentExceptionManager(response.StatusCode, errorResponse!);

                // Throw an exception with the error message.
                throw new Exception($"{response.StatusCode}: {errorResponse.Message}");
            }
        }
        /// <summary>
        /// starts the process by asking IDPay for a payment instance asynchronously by using the info provided
        /// </summary>
        /// <param name="paymentRequestInfo"></param>
        /// <returns>Transaction ID and Payment Webpage Link if successful, throws relevant Exception otherwise. </returns>
        /// <exception cref="Exception"></exception>
        public async Task<PaymentRequestResponse> SendPaymentRequestAsync(PaymentRequest paymentRequestInfo)
        {

            // Prepare the request body as a JSON object.
            Dictionary<string, dynamic> requestBody = new()
                {
                    { "order_id", paymentRequestInfo.OrderId },
                    { "amount", paymentRequestInfo.Amount },
                    { "name", !string.IsNullOrEmpty(paymentRequestInfo.Name) ? paymentRequestInfo.Name : "" },
                    { "phone", !string.IsNullOrEmpty(paymentRequestInfo.PhoneNumber) && IsPhoneNumber(paymentRequestInfo.PhoneNumber) ? paymentRequestInfo.PhoneNumber : "" },
                    { "mail", !string.IsNullOrEmpty(paymentRequestInfo.Email) && IsEmail(paymentRequestInfo.Email) ? paymentRequestInfo.Email : "" },
                    { "desc", paymentRequestInfo.Description ?? "" },
                    { "callback", _CallbackAddress }
                };
            string requestBodyJson = JsonSerializer.Serialize(requestBody);

            // Create the HTTP content from the request body.
            HttpContent content = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

            // Send a POST request to the IDPay API to create a new transaction.
            HttpResponseMessage response = await _httpClient.PostAsync(_requestNewTransactionAPI, content);

            // Check the status code of the response.
            if (response.StatusCode == HttpStatusCode.Created) // 201   // means transaction has successfully been created 
            {
                // Deserialize the response JSON to extract the payment creation success details.
                PaymentCreationSuccessResponse? deserializedResponse = await JsonSerializer.DeserializeAsync<PaymentCreationSuccessResponse>(await response.Content.ReadAsStreamAsync());

                PaymentRequestResponse payment_response = new(deserializedResponse.Id, deserializedResponse.Link);

                // Return the payment link to redirect the user to the payment page.
                return payment_response;
            }
            else
            {
                // If the request was not successful, deserialize the error response and handle it.
                var errorResponse = await JsonSerializer.DeserializeAsync<IDPayErrorResponse>(await response.Content.ReadAsStreamAsync());
                PaymentExceptionManager(response.StatusCode, errorResponse!);

                // Throw an exception with the error message.
                throw new Exception($"{response.StatusCode}: {errorResponse.Message}");
            }
        }

        /// <summary>
        /// Validates the outcome of a payment based on the response received from the IDPay API Asynchronously.
        /// </summary>
        /// <param name="idpayResponse">The response received from the IDPay API.</param>
        /// <returns>a <see cref="PaymentVerificationOutcome"/> object. if the verification fails, <see cref="PaymentVerificationOutcome.Time"/> will be set to null.</returns>
        public static async Task<PaymentVerificationOutcome> PaymentOutcomeValidationAsync(PaymentOutcomeValidationResponse idpayResponse)
        {
            PaymentVerificationOutcome outcome = new(false, null);
            // Prepare the request body as a JSON object.
            Dictionary<string, string> requestBody = new()
                {
                    {"id", idpayResponse.TransactionID!},
                    {"order_id", idpayResponse.OrderID!}
                };

            string requestBodyJson = JsonSerializer.Serialize(requestBody);
            HttpContent content = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

            // Send a POST request to the payment verification API endpoint.
            HttpResponseMessage response = await _httpClient.PostAsync(_paymentVerificationAPI, content);

            // Check if the request was successful.
            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response JSON to extract the verification date.
                var responseData = JsonSerializer.Deserialize<Dictionary<string, string>>(await response.Content.ReadAsStreamAsync())!;
                string verificationDateUnixString = responseData["verify"];

                // Convert the verification date from Unix timestamp to DateTime and update the payment record.
                DateTime verificationDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(verificationDateUnixString)).DateTime;

                outcome.Status = true;
                outcome.Time = verificationDate;

                // verification was successful and the verification date is returned.
                return outcome;
            }
            else
            {
                // If the request was not successful, deserialize the error response and handle it.
                var errorResponse = JsonSerializer.Deserialize<IDPayErrorResponse>(await response.Content.ReadAsStreamAsync());

                throw PaymentExceptionManager(response.StatusCode, errorResponse!);
            }
        }

        /// <summary>
        /// checks whether an email is valid or not using Regex.
        /// </summary>
        /// <param name="email"> the email to be validated</param>
        /// <returns> true if match false if input null, empty or does not match the pattern. </returns>
        private static bool IsEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            // The Regex pattern for evaluating an email.
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, pattern);
        }

        /// <summary>
        /// checks whether a phone number is valid or not using Regex.
        /// </summary>
        /// <param name="phoneNumber">the number to be evaluated</param>
        /// <returns> true if match false if input null, empty or does not match the pattern. </returns>
        private static bool IsPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return false;
            // The Regex pattern for evaluating a iranian phone number.
            string pattern = @"^(0|\+98)?([ -]?)9[1-4][0-9]{8}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }

        /// <summary>
        /// Extracts the first number found in a given string.
        /// </summary>
        /// <param name="input">The input string from which to extract the number.</param>
        /// <returns>The extracted number as an integer.</returns>
        static int ExtractNumberFromString(string input)
        {
            // Regular expression pattern to match one or more digits
            string pattern = @"\d+";
            // Match the pattern in the input string
            Match match = Regex.Match(input, pattern);
            // Parse and return the matched value as an integer
            return int.Parse(match.Value);
        }

        /// <summary>
        /// Handles exceptions based on the HTTP status code and the inner error response.
        /// </summary>
        /// <param name="response">The HTTP status code of the payment operation.</param>
        /// <param name="innerResponse">The detailed error response from the payment service.</param>
        /// <exception cref="AmountLessThanMinimumException">Thrown when the amount is less than the minimum acceptable value.</exception>
        /// <exception cref="AmountExceedsMaximumException">Thrown when the amount exceeds the maximum acceptable value.</exception>
        /// <exception cref="AmountExceedsLimitException">Thrown when the amount exceeds the limit.</exception>
        /// <exception cref="CallbackDomainMismatchException">Thrown when the callback domain does not match.</exception>
        /// <exception cref="InvalidCallbackAddressException">Thrown when the callback address is invalid.</exception>
        /// <exception cref="UserBlockedException">Thrown when the user is blocked.</exception>
        /// <exception cref="ApiKeyNotFoundException">Thrown when the API key is not found.</exception>
        /// <exception cref="IpMismatchException">Thrown when there is an IP address mismatch.</exception>
        /// <exception cref="WebServiceNotApprovedException">Thrown when the web service is not approved.</exception>
        /// <exception cref="BankAccountNotApprovedException">Thrown when the bank account is not approved.</exception>
        /// <exception cref="BankAccountInactiveException">Thrown when the bank account is inactive.</exception>
        /// <exception cref="TransactionNotCreatedException">Thrown when the transaction was not created.</exception>
        /// <exception cref="UnexpectedErrorException">Thrown when an unexpected error occurs.</exception>
        private static Exception PaymentExceptionManager(HttpStatusCode response, IDPayErrorResponse innerResponse)
        {

            // Check if the response status is NotAcceptable (406)
            if (response == HttpStatusCode.NotAcceptable)
            {
                // Handle different error codes in the inner response
                switch (innerResponse!.Code)
                {
                    case 34:
                        // Extract minimum acceptable amount from the message
                        int minimumAcceptable = ExtractNumberFromString(innerResponse.Message);
                        throw new AmountLessThanMinimumException(minimumAcceptable);
                    case 35:
                        // Extract maximum acceptable amount from the message
                        int maximumAcceptable = ExtractNumberFromString(innerResponse.Message);
                        throw new AmountExceedsMaximumException(maximumAcceptable);
                    case 36:
                        throw new AmountExceedsLimitException();
                    case 38:
                        throw new CallbackDomainMismatchException();
                    case 39:
                        throw new InvalidCallbackAddressException();
                    default:
                        throw new UnexpectedErrorException();
                }
            }
            // Check if the response status is Forbidden (403)
            else if (response == HttpStatusCode.Forbidden)
            {
                // Handle different error codes in the inner response
                switch (innerResponse!.Code)
                {
                    case 11:
                        throw new UserBlockedException();
                    case 12:
                        throw new ApiKeyNotFoundException();
                    case 13:
                        // Regular expression pattern to match an IPv4 address
                        string pattern = @"\b(?:\d{1,3}\.){3}\d{1,3}\b";
                        // Extract the IP address from the message
                        string ipAddress = Regex.Match(innerResponse.Message, pattern).Value;
                        throw new IpMismatchException(ipAddress);
                    case 14:
                        throw new WebServiceNotApprovedException();
                    case 21:
                        throw new BankAccountNotApprovedException();
                    case 24:
                        throw new BankAccountInactiveException();
                    default:
                        throw new UnexpectedErrorException();
                }
            }
            // Check if the response status is MethodNotAllowed (405)
            else if (response == HttpStatusCode.MethodNotAllowed)
            {
                throw new TransactionNotCreatedException();
            }
            else
            {
                throw new UnexpectedErrorException();
            }
        }
    }

    /// <summary>
    /// Represents an object containing the info required to request a transaction from IDPay
    /// </summary>
    public class PaymentRequest(string orderId, int amount, string? description = null, string? name = null, string? phoneNumber = null, string? email = null)
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentRequest"/> class.
        /// </summary>
        /// <param name="orderId">The unique identifier for the order.</param>
        /// <param name="amount">The amount for the payment.</param>
        /// <param name="description">The description of the payment (optional).</param>
        /// <param name="name">The name of the payer (optional).</param>
        /// <param name="phoneNumber">The phone number of the payer (optional).</param>
        /// <param name="email">The email of the payer (optional).</param>

        /// <summary>
        /// Gets or sets the unique identifier for the order.
        /// </summary>
        public required string OrderId { get; set; } = orderId;

        /// <summary>
        /// Gets or sets the amount for the payment.
        /// </summary>
        public required int Amount { get; set; } = amount;

        /// <summary>
        /// Gets or sets the description of the payment.
        /// </summary>
        public string? Description { get; set; } = description;

        /// <summary>
        /// Gets or sets the name of the payer.
        /// </summary>
        public string? Name { get; set; } = name;

        /// <summary>
        /// Gets or sets the phone number of the payer.
        /// </summary>
        public string? PhoneNumber { get; set; } = phoneNumber;

        /// <summary>
        /// Gets or sets the email of the payer.
        /// </summary>
        public string? Email { get; set; } = email;
    }

    /// <summary>
    /// Represents an object containing the response for payment-creation if the request has not failed.
    /// </summary>
    /// <param name="transactionID">the Transaction ID Generated by IDPay.</param>
    /// <param name="paymentLink">the Link to Payment Webpage Generated by IDPay.</param>
    public class PaymentRequestResponse(string transactionID, string paymentLink)
    {
        /// <summary>
        /// Gets or Sets the Transaction ID Generated by IDPay.
        /// </summary>
        public string TransactionID { get; set; } = transactionID;
        /// <summary>
        /// Gets or Sets the Link to Payment Webpage.
        /// </summary>
        public string PaymentLink { get; set; } = paymentLink.Trim();

    }

    /// <summary>
    /// a class used to represent errors thrown by IDPay.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error Message.</param>
    public class IDPayErrorResponse(int code, string message)
    {
        /// <summary>
        /// Gets or Sets the Error Response Code
        /// </summary>
        public int Code { get; set; } = code;
        /// <summary>
        /// Gets or Sets the Error Message.
        /// </summary>
        public string Message { get; set; } = message;
    }

    /// <summary>
    /// Represents a successful payment creation message came from IDPay.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="Link"></param>
    public class PaymentCreationSuccessResponse(string id, string Link)
    {
        /// <summary>
        /// Gets or Sets Transaction ID.
        /// </summary>
        public string Id { get; set; } = id;
        /// <summary>
        /// Gets or Sets the Link to Payment page.
        /// </summary>
        public string Link { get; set; } = Link;

    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="status"></param>
    /// <param name="trackID"></param>
    /// <param name="transactionID"></param>
    /// <param name="orderID"></param>
    /// <param name="amount"></param>
    /// <param name="cardNo"></param>
    /// <param name="hashedCardNo"></param>
    /// <param name="transactionTime"></param>
    public class PaymentOutcomeValidationResponse(int status, int trackID, string transactionID, string orderID, int amount, string cardNo, string hashedCardNo, long transactionTime)
    {
        public required int Status { get; set; } = status;
        public required int TrackID { get; set; } = trackID;
        public required string TransactionID { get; set; } = transactionID;
        public required string OrderID { get; set; } = orderID;
        public required int Amount { get; set; } = amount;
        public required string CardNo { get; set; } = cardNo;
        public required string HashedCardNo { get; set; } = hashedCardNo;
        public required long TransactionTime { get; set; } = transactionTime;

    }
    /// <summary>
    /// Represents a payment verification's outcome sent by IDPay
    /// </summary>
    /// <param name="status"> true for success, false for failure in verification.</param>
    /// <param name="time"> time of verification, null if the verification fails.</param>
    public class PaymentVerificationOutcome(bool status, DateTime? time)
    {
        /// <summary>
        /// true for success, false for failure in verification.
        /// </summary>
        public bool Status { get; set; } = status;
        /// <summary>
        /// time of verification, null if the verification fails.
        /// </summary>
        public DateTime? Time { get; set; } = time;
    }
}

