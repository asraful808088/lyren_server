using Microsoft.AspNetCore.Mvc;
using NexTrade.Services;

namespace NexTradePayment.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;

    public PaymentController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }



    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        try
        {
            var result = await _paymentService.CreatePaymentAsync(
                request.Email,
                request.Password,
                request.Amount,
                request.Platform);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }



    [HttpPost("ewallet")]
    public async Task<IActionResult> EWalletPayment([FromBody] EWalletPaymentRequest request)
    {
        try
        {
            var result = await _paymentService.CreateEWalletPaymentAsync(request.Amount);

            return Content(result, "application/json");
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }
}



public class PaymentRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Platform { get; set; } = "nextrade";
}



public class EWalletPaymentRequest
{
    public decimal Amount { get; set; }
}
