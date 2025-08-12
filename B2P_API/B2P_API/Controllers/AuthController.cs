using B2P_API.DTOs;
using B2P_API.DTOs.Account;
using B2P_API.DTOs.AuthDTOs;
using B2P_API.Response;
using B2P_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")] // ✅ Force tất cả endpoint return JSON
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly AccountManagementService _accService;

        public AuthController(AuthService authService, AccountManagementService accService)
        {
            _authService = authService;
            _accService = accService;
        }

        /// <summary>
        /// Gửi OTP đến email hoặc số điện thoại
        /// </summary>
        /// <param name="request">Thông tin gửi OTP</param>
        /// <returns>Kết quả gửi OTP</returns>
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid input data",
                    Status = 400,
                    Data = ModelState
                });
            }

            var result = await _authService.SendOtpAsync(request);
            return StatusCode(result.Status, result);
        }

        /// <summary>
        /// Xác thực OTP và đăng nhập/đăng ký
        /// </summary>
        /// <param name="request">Thông tin xác thực OTP</param>
        /// <returns>JWT tokens và thông tin user</returns>
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid input data",
                    Status = 400,
                    Data = ModelState
                });
            }

            var result = await _authService.VerifyOtpAndLoginAsync(request);
            return StatusCode(result.Status, result);
        }

        /// <summary>
        /// Làm mới access token bằng refresh token
        /// </summary>
        /// <param name="request">Refresh token</param>
        /// <returns>JWT tokens mới</returns>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid input data",
                    Status = 400,
                    Data = ModelState
                });
            }

            var result = await _authService.RefreshTokenAsync(request);
            return StatusCode(result.Status, result);
        }

        /// <summary>
        /// Đăng xuất khỏi một device hoặc tất cả devices
        /// </summary>
        /// <param name="request">Thông tin logout</param>
        /// <returns>Kết quả logout</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid input data",
                    Status = 400,
                    Data = ModelState
                });
            }

            // Lấy access token từ Authorization header nếu không có trong body
            if (string.IsNullOrEmpty(request.AccessToken))
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    request.AccessToken = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            var result = await _authService.LogoutAsync(request);
            return StatusCode(result.Status, result);
        }

        /// <summary>
        /// Lấy thông tin profile của user hiện tại
        /// </summary>
        /// <returns>Thông tin user</returns>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            // Lấy userId từ JWT claims
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid token claims",
                    Status = 401,
                    Data = null
                });
            }

            var result = await _accService.GetAccountByIdAsync(userId);
            return StatusCode(result.Status, result);
        }

        /// <summary>
        /// Lấy thông tin user theo ID (Admin only)
        /// </summary>
        /// <param name="userId">ID của user</param>
        /// <returns>Thông tin user</returns>
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUserById(int userId)
        {
            // TODO: Có thể thêm role check ở đây
            // var userRole = User.FindFirst("roleId")?.Value;
            // if (userRole != "2") // Admin role
            // {
            //     return Forbid();
            // }

            var result = await _accService.GetAccountByIdAsync(userId);
            return StatusCode(result.Status, result);
        }

        /// <summary>
        /// Health check cho Auth service
        /// </summary>
        /// <returns>Trạng thái service</returns>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Service = "AuthController",
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            });
        }

        /// <summary>
        /// Validate token (utility endpoint)
        /// </summary>
        /// <returns>Token validation result</returns>
        [HttpGet("validate-token")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            var emailClaim = User.FindFirst("email")?.Value;
            var phoneClaim = User.FindFirst("phone")?.Value;
            var roleIdClaim = User.FindFirst("roleId")?.Value;
            var fullNameClaim = User.FindFirst("fullName")?.Value;

            return Ok(new
            {
                Success = true,
                Message = "Token is valid",
                Data = new
                {
                    UserId = userIdClaim,
                    Email = emailClaim,
                    Phone = phoneClaim,
                    FullName = fullNameClaim,
                    RoleId = roleIdClaim,
                    ValidatedAt = DateTime.UtcNow
                }
            });
        }
        /// <summary>
        /// Đăng nhập bằng Google OAuth - Gửi OTP để xác thực
        /// </summary>
        /// <param name="request">Google ID token từ frontend</param>
        /// <returns>Thông tin OTP session</returns>
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Dữ liệu request không hợp lệ",
                        Status = 400,
                        Data = ModelState
                    });
                }

                if (string.IsNullOrEmpty(request.GoogleToken))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Google token là bắt buộc",
                        Status = 400,
                        Data = null
                    });
                }

                Console.WriteLine($"🔑 Nhận Google login request");

                var result = await _authService.GoogleLoginAsync(request);

                Console.WriteLine($"🎯 Google login result: Success = {result.Success}, Status = {result.Status}");

                return StatusCode(result.Status, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi Google login controller: {ex.Message}");

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi server trong quá trình Google login",
                    Status = 500,
                    Data = null
                });
            }
        }
    }
}