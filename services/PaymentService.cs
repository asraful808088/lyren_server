using System.Text;
using System.Text.Json;

using System.Globalization;
using System.Text;
using System.Text.Json;
namespace NexTrade.Services;

public class PaymentService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public PaymentService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<string> CreatePaymentAsync(
        string email,
        string password,
        decimal amount,
        string platform)
    {
        switch (platform.ToLower())
        {
            case "nextrade":
                return await NexTradePayment(email, password, amount);
            default:
                throw new Exception("Unsupported payment platform.");
        }
    }

    private async Task<string> NexTradePayment(
        string email,
        string password,
        decimal amount)
    {
        // Read nested configuration
        var apiKey = _configuration["Payment:NexTrade:ApiKey"];
        var baseUrl = _configuration["Payment:NexTrade:BaseUrl"];
        
        Console.WriteLine($"API Key loaded: {!string.IsNullOrEmpty(apiKey)}");
        Console.WriteLine($"Base URL: {baseUrl}");
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("NexTrade API key is not configured");
        }

        var url = $"{baseUrl}/{apiKey}";
        Console.WriteLine($"Calling URL: {url}");

        var payload = new
        {
            email = email,
            pass = password,
            amount = amount
        };

        var json = JsonSerializer.Serialize(payload);
        Console.WriteLine($"Payload: {json}");
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);
        
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Status: {response.StatusCode}");
        Console.WriteLine($"Response Body: {responseBody}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"NexTrade payment failed: {responseBody}");
        }
        
        return responseBody;
    }



public async Task<string> CreateEWalletPaymentAsync(decimal amount)
{
    // These values should come from your EWallet settings
    var apiKey = _configuration["EWallet:ApiKey"];
    var baseUrl = _configuration["EWallet:BaseUrl"];
    var shopName = _configuration["EWallet:ShopName"];

    if (string.IsNullOrWhiteSpace(apiKey))
        throw new Exception("EWallet API Key is missing.");

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new Exception("EWallet BaseUrl is missing.");

    if (string.IsNullOrWhiteSpace(shopName))
        throw new Exception("EWallet ShopName is missing.");

    var payload = new
    {
        type = "payment",
        platform_name = shopName,
        base_url = baseUrl,
        api_key = apiKey,
        merchant_name = "Lyren Store",
        total_price = $"${amount.ToString("0.00", CultureInfo.InvariantCulture)}"
    };

    var json = JsonSerializer.Serialize(payload);

    var response = await _httpClient.PostAsync(
        "https://chain-hook-backend-evj9.vercel.app/api/users/clients/generate-token/",
        new StringContent(
            json,
            Encoding.UTF8,
            "application/json")
    );

    var body = await response.Content.ReadAsStringAsync();
   
    Console.WriteLine(body);
    if (!response.IsSuccessStatusCode)
        throw new Exception(body);

    return body;
}
}
