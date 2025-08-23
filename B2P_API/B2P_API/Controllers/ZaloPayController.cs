using B2P_API.DTOs.CommissionPaymentHistoryDTOs;
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
        private readonly BookingService _bookingService;
        private readonly CommissionPaymentHistoryService _commissionService;

        public PaymentController(ZaloPayService zaloPayService, 
            ILogger<PaymentController> logger, 
            CommissionPaymentHistoryService commissionService, 
            BookingService bookingService)
        {
            _zaloPayService = zaloPayService;
            _logger = logger;
            _bookingService = bookingService;
            _commissionService = commissionService;
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
                request.CallbackUrl = "https://api.book2play.site/api/Payment/callback";
                request.RedirectUrl = "https://book2play.site/";

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
        public async Task<IActionResult> Callback([FromBody] CallbackRequest request)
        {
            try
            {
                _logger.LogInformation($"ZaloPay callback received. Data raw: {request.Data}");

                // ===========================
                // 🔒 PHẦN KIỂM TRA MAC (tùy chọn)
                // ===========================
                // Uncomment nếu cần verify MAC signature
                /*
                try
                {
                    _logger.LogInformation($"Received MAC: {request.Mac}");
                    _logger.LogInformation($"Using Key1: {_config.Key1}");

                    var computedMac = CreateHmacSha256(request.Data, _config.Key1);
                    _logger.LogInformation($"Computed MAC: {computedMac}");

                    var isValid = computedMac.Equals(request.Mac, StringComparison.OrdinalIgnoreCase);
                    if (!isValid)
                    {
                        _logger.LogWarning($"Invalid MAC. Received: {request.Mac}, Computed: {computedMac}");
                        return Ok(new { return_code = 0, return_message = "Invalid MAC signature" });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating MAC.");
                    return Ok(new { return_code = 0, return_message = "Error validating MAC" });
                }
                */

                // ===========================
                // 📦 PARSE DỮ LIỆU CALLBACK
                // ===========================
                var callbackData = JsonSerializer.Deserialize<CallbackData>(request.Data);
                var transId = callbackData.zp_trans_id;
                var appTransId = callbackData.app_trans_id;
                var amount = callbackData.amount;

                _logger.LogInformation("Parsed callbackData: {@CallbackData}", callbackData);
                _logger.LogInformation($"✅ Payment success. TransId: {transId}, AppTransId: {appTransId}, Amount: {amount}");

                // ===========================
                // 🛠️ XỬ LÝ EMBED_DATA
                // ===========================
                if (!string.IsNullOrEmpty(callbackData.embed_data))
                {
                    try
                    {
                        var embedData = JsonSerializer.Deserialize<Dictionary<string, object>>(callbackData.embed_data);

                        // ✅ Trường hợp Booking
                        if (embedData.TryGetValue("bookingid", out var bookingIdObj))
                        {
                            if (bookingIdObj != null && int.TryParse(bookingIdObj.ToString(), out var bookingId))
                            {
                                _logger.LogInformation($"Booking ID extracted: {bookingId}");

                                await _bookingService.MarkBookingPaidAsync(bookingId, transId.ToString());
                                _logger.LogInformation("MarkBookingPaidAsync executed successfully.");
                            }
                            else
                            {
                                _logger.LogWarning("Booking ID is not a valid integer.");
                            }
                        }
                        // ✅ Trường hợp có forMonth và forYear
                        else if (embedData.TryGetValue("forMonth", out var forMonthObj) &&
                                 embedData.TryGetValue("forYear", out var forYearObj))
                        {
                            var forMonth = forMonthObj?.ToString();
                            var forYear = forYearObj?.ToString();
                            CommissionPaymentHistoryCreateDto cms = new CommissionPaymentHistoryCreateDto();
                            cms.Amount = callbackData.amount;
                            cms.UserId = int.Parse(callbackData.app_user);
                            cms.Month = int.Parse(forMonth);
                            cms.Year = int.Parse(forYear);
                            cms.StatusId = 10;
                            await _commissionService.CreateAsync(cms);

                            _logger.LogInformation($"ForMonth: {forMonth}, ForYear: {forYear} extracted from embed_data");
                        }
                        else
                        {
                            _logger.LogWarning("No valid data found in embed_data. Expected: bookingid OR (forMonth + forYear)");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse embed_data or process booking/commission ID");
                    }
                }

                return Ok(new { return_code = 1, return_message = "success" });
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