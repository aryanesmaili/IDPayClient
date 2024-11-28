# IDPay Payment Gateway Client Library

## Overview

This C# library provides a comprehensive client for interacting with the IDPay payment gateway, simplifying payment processing, transaction management, and financial operations for .NET applications.

## Disclaimer

This library is an independent implementation and is not officially supported by IDPay. Always refer to the official IDPay documentation for the most up-to-date API specifications.

## Features

- Full support for IDPay payment gateway API endpoints
- Comprehensive transaction management
- Flexible configuration options
- Robust error handling
- Support for both production and sandbox environments

### Supported Operations

- Request new payment transactions
- Verify transactions
- Check transaction status
- Retrieve transaction lists
- Configurable for test and production modes

## Installation

Install the library via NuGet Package Manager:

```bash
dotnet add package IDPayClient
```

## Configuration

### Creating an IDPay Configuration

```csharp
var config = new IDPayConfig
{
    ApiKey = "your_idpay_api_key",
    IsTest = false  // Set to true for sandbox environment
};
```

### Initializing the Client

```csharp
var httpClient = new HttpClient();
var idPayClient = new IDPayClient(config, httpClient);
```
## Dependency Injection in ASP.NET Core

### Configuration in Program.cs (.NET 8+)

```csharp
using IDPayClass;

var builder = WebApplication.CreateBuilder(args);

// Configure IDPay settings from configuration
builder.Services.Configure<IDPayConfig>(
    builder.Configuration.GetSection("IDPay"));

// Register IDPayClient with dependency injection
builder.Services.AddHttpClient<IDPayClient>((serviceProvider, client) => 
{
    var config = serviceProvider.GetRequiredService<IOptions<IDPayConfig>>().Value;
    return new IDPayClient(config, client);
});

var app = builder.Build();
```

### Consuming in a Service or Controller

```csharp
public class PaymentService
{
    private readonly IDPayClient _idPayClient;

    public PaymentService(IDPayClient idPayClient)
    {
        _idPayClient = idPayClient;
    }

    public async Task<CreatePaymentResponse> ProcessPayment()
    {
        var paymentRequest = new CreatePaymentRequest
        {
            OrderId = "some_order_id",
            Amount = 50000,
            CallbackAddress = "https://yourapp.com/callback",
            Name = "John Doe",
            Email = "john@example.com"
        };
        try 
        {
            return await _idPayClient.RequestPaymentAsync(paymentRequest);
        }
        catch (Exception ex)
        {
            // Handle exception here
            throw;
        }
    }
}

// Or in a minimal API controller
app.MapPost("/payment", async (IDPayClient idPayClient) =>
{
    var paymentRequest = new PaymentRequest
    {
        OrderId = "some_order_id",
        Amount = 50000,
        CallbackAddress = "https://yourapp.com/callback"
    };

    return await idPayClient.RequestPaymentAsync(paymentRequest);
});
```

### Recommended Configuration in appsettings.json

```json
{
  "IDPay": {
    "ApiKey": "your_idpay_api_key",
    "IsTest": true,
    "RequestNewTransactionAPI": "https://api.idpay.ir/v1.1/payment",
    "PaymentVerificationAPI": "https://api.idpay.ir/v1.1/payment/verify",
    "PaymentInquiryAPI" : "https://api.idpay.ir/v1.1/payment/inquiry",
    "TransactionsListAPI" : "https://api.idpay.ir/v1.1/payment/transactions"
  }
}
```

### Environment-Specific Configuration

For different environments, you can use:

#### appsettings.Development.json
```json
{
  "IDPay": {
    "IsTest": true,
    "ApiKey": "your_api_key"
  }
}
```

#### appsettings.Production.json
```json
{
  "IDPay": {
    "IsTest": false,
    "ApiKey": "your_api_key"
  }
}
```

### Best Practices

- Use `IOptions<IDPayConfig>` for configuration
- Utilize environment-specific settings
- Protect sensitive information using user secrets or secure vaults
- Use HttpClient factory for proper connection management

## Usage Examples

### Requesting a Payment

```csharp
var paymentRequest = new PaymentRequest
{
    OrderId = "unique_order_123",
    Amount = 50000,  // Amount in Rials
    CallbackAddress = "https://yourwebsite.com/payment/callback",
    Description = "Product purchase",
    Name = "John Doe",
    Email = "john.doe@example.com",
    PhoneNumber = "09123456789"
};

var paymentResponse = await idPayClient.RequestPaymentAsync(paymentRequest);

if (paymentResponse.Success)
{
    // Redirect user to paymentResponse.PaymentLink
    Console.WriteLine($"Payment Link: {paymentResponse.PaymentLink}");
}
else
{
    Console.WriteLine($"Error: {paymentResponse.Message}");
}
```

### Verifying a Transaction

```csharp
var verifyRequest = new VerifyTransactionRequest
{
    TransactionID = "idpay_transaction_id",
    OrderID = "your_order_id"
};

var verificationResponse = await idPayClient.VerifyTransactionAsync(verifyRequest);
```

### Checking Transaction Status

```csharp
var inquiryRequest = new TransactionInquiryRequest
{
    TransactionID = "idpay_transaction_id",
    OrderID = "your_order_id"
};

var statusResponse = await idPayClient.CheckTransactionStatus(inquiryRequest);
```

## Configuration Options

The `IDPayConfig` class allows you to customize:
- API Key
- Environment (Test/Production)
- Custom API Endpoint URLs

## Error Handling

The library throws `ArgumentException` for invalid input parameters and returns detailed error information through response objects.

## Security

- Validates input parameters
- Supports API key authentication
- Trims and sanitizes input data
- Supports both test and production environments

## Dependencies

- .NET 8.0 or later

## Contributing

Contributions are welcome! Please submit pull requests or open issues on the project repository.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

