namespace NexTradePayment.Models;

public class PaymentRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public List<OrderItem> OrderItems { get; set; } = new();
    public string ShippingAddress { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
    
    public string Platform => "NexTrade";
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class ValidateRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}