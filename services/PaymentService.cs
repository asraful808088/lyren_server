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
}