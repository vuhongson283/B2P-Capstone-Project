using B2P_API.DTOs.PaymentDTOs;
using B2P_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _paymentService;

        public PaymentController(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("momo/create")]
        public async Task<IActionResult> CreateMomoPayment([FromBody] CreatePaymentDto dto)
        {
            var payUrl = await _paymentService.CreatePaymentAsync(dto.BookingId, dto.Amount);
            return Ok(new { payUrl });
        }

        // IPN callback từ MOMO gọi về
        [HttpPost("momo-ipn")]
        public async Task<IActionResult> MomoIpn([FromForm] MomoIpnModel model)
        {
            // resultCode==0 là thành công
            bool success = model.resultCode == 0;
            int paymentId = int.Parse(model.orderId);
            await _paymentService.HandleCallbackAsync(paymentId, success);
            return Ok();
        }
    }

    public class CreatePaymentDto
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
    }
}
