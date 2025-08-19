using B2P_API.Models;
using B2P_API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;


namespace B2P_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PaymentController : ControllerBase
    {
        private readonly ZaloPayService _zaloPayService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ZaloPayService zaloPayService, ILogger<PaymentController> logger)
        {
            _zaloPayService = zaloPayService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo đơn hàng thanh toán ZaloPay
        /// </summary>
        /// <param name="request">Thông tin đơn hàng</param>
        /// <returns>Kết quả tạo đơn hàng</returns>
        [HttpPost("create-order")]
        [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new PaymentResult
                {
                    Success = false,
                    Message = string.Join(", ", errors),
                    ErrorCode = 400
                });
            }

            try
            {
                // Set default URLs nếu không có
                request.CallbackUrl = "https://webhook.site/5a2001a7-9956-49b8-a0d7-17a059f82d76";
                request.RedirectUrl = "https://localhost:7202/swagger/index.html";

                _logger.LogInformation($"Creating order for amount: {request.Amount}");

                var result = await _zaloPayService.CreateOrderAsync(request);

                var statusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateOrder");
                return StatusCode(StatusCodes.Status500InternalServerError, new PaymentResult
                {
                    Success = false,
                    Message = "Có lỗi không mong muốn xảy ra",
                    ErrorCode = 500
                });
            }
        }

        /// <summary>
        /// Callback từ ZaloPay sau khi thanh toán
        /// </summary>
        [HttpPost("callback")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> Callback([FromForm] CallbackRequest request)
        {
            try
            {
                _logger.LogInformation($"ZaloPay callback received. Data: {request.Data}");

                var verifyResult = await _zaloPayService.VerifyCallback(request.Data, request.Mac);

                /*if (!verifyResult.Success)
                {
                    _logger.LogWarning("Invalid callback MAC");
                    return Ok(new { return_code = -1, return_message = "mac not equal" });
                }*/

                if (verifyResult.Data is CallbackData callbackData)
                {
                   

                    // Nếu cần xử lý business logic phụ, có thể gọi thêm tại đây
                    // await ProcessPaymentSuccess(callbackData);

                    return Ok(new { return_code = 1, return_message = "success" });
                }

                return Ok(new { return_code = 0, return_message = "error processing data" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ZaloPay callback");
                return Ok(new { return_code = 0, return_message = "error" });
            }
        }


        /// <summary>
        /// Kiểm tra trạng thái đơn hàng
        /// </summary>
        /// <param name="request">Thông tin truy vấn đơn hàng</param>
        /// <returns>Trạng thái đơn hàng</returns>
        [HttpPost("query-order")]
        [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QueryOrder([FromBody] QueryOrderRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new PaymentResult
                {
                    Success = false,
                    Message = string.Join(", ", errors),
                    ErrorCode = 400
                });
            }

            try
            {
                _logger.LogInformation($"Querying order: {request.AppTransId}");

                var result = await _zaloPayService.QueryOrderAsync(request.AppTransId);

                var statusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in QueryOrder");
                return StatusCode(StatusCodes.Status500InternalServerError, new PaymentResult
                {
                    Success = false,
                    Message = "Có lỗi không mong muốn xảy ra",
                    ErrorCode = 500
                });
            }
        }

        /// <summary>
        /// Test endpoint để kiểm tra API hoạt động
        /// </summary>
        /// <returns>Trạng thái API</returns>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTimeOffset.UtcNow,
                service = "ZaloPay Payment API"
            });
        }

        /// <summary>
        /// Success page redirect từ ZaloPay (optional)
        /// </summary>
        [HttpGet("success")]
        public IActionResult PaymentSuccess()
        {
            // Có thể redirect đến frontend hoặc trả về thông báo
            return Ok(new
            {
                message = "Thanh toán thành công!",
                timestamp = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Cancel page redirect từ ZaloPay (optional)  
        /// </summary>
        [HttpGet("cancel")]
        public IActionResult PaymentCancel()
        {
            return Ok(new
            {
                message = "Thanh toán đã bị hủy",
                timestamp = DateTimeOffset.UtcNow
            });
        }
    }
}