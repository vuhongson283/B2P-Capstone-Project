using Azure;
using B2P_API.DTOs.UserDTO;
using B2P_API.Models;
using B2P_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("forgot-password-by-email")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestByEmailDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _userService.SendPasswordResetOtpAsync(request);

            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }

            return Ok(response);
        }

        [HttpPost("reset-password-by-email")]
        public async Task<IActionResult> ResetPassword([FromBody] VerifyOtpDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _userService.VerifyOtpAndResetPasswordAsync(request);

            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }

            return Ok(response);
        }

        [HttpPost("resend-otp-by-email")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _userService.ResendPasswordResetOtpAsync(request.Email);

            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }

            return Ok(response);
        }
    

        [HttpGet("get-user")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var response = await _userService.GetUserByIdAsync(userId);
            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }

            return Ok(response);
        }

        [HttpPut("update-user")]
        public async Task<IActionResult> UpdateUser(int userId,[FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await _userService.UpdateUserAsync(userId,request);
            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }
            return Ok(response);
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await _userService.ChangePasswordAsync(request);
            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }
            return Ok(response);
        }
    }
}
