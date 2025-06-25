using Azure;
using B2P_API.DTOs.UserDTO;
using B2P_API.Interface;
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
        private readonly ISMSService _sMSService;
        public UserController(UserService userService, ISMSService sMSService)
        {
            _userService = userService;
            _sMSService = sMSService;
        }

        [HttpPost("forgot-password-by-email")]
        public async Task<IActionResult> ForgotPasswordByEmail([FromBody] ForgotPasswordRequestByEmailDto request)
        {
            var response = await _userService.SendPasswordResetOtpByEmailAsync(request);

            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }

            return Ok(response);
        }

        [HttpPost("reset-password-by-email")]
        public async Task<IActionResult> ResetPasswordByEmail([FromBody] VerifyOtpDtoByEmail request)
        {
            var response = await _userService.VerifyOtpAndResetPasswordByEmailAsync(request);

            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }

            return Ok(response);
        }

        [HttpPost("resend-otp-by-email")]
        public async Task<IActionResult> ResendOtpByEmail([FromBody] ResendOtpDtoByEmail? request)
        {
            var response = await _userService.ResendPasswordResetOtpByEmailAsync(request);

            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }

            return Ok(response);
        }

        [HttpPost("forgot-password-by-sms")]
        public async Task<IActionResult> ForgotPasswordBySms([FromBody] ForgotPasswordRequestBySmsDto request)
        {
            var response = await _userService.SendPasswordResetOtpBySMSAsync(request);

            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }

            return Ok(response);
        }

        [HttpPost("reset-password-by-sms")]
        public async Task<IActionResult> ResetPasswordBySms([FromBody] VerifyOtpBySmsDto request)
        {
            var response = await _userService.VerifyOtpAndResetPasswordBySMSAsync(request);

            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }

            return Ok(response);
        }

        [HttpPost("resend-otp-by-sms")]
        public async Task<IActionResult> ResendOtpBySms([FromBody] ResendOtpBySmsDto? request)
        {
            var response = await _userService.ResendPasswordResetOtpBySMSAsync(request);

            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }

            return Ok(response);
        }


        [HttpGet("get-user-by-id")]
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
            var response = await _userService.ChangePasswordAsync(request);
            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }
            return Ok(response);
        }



    }
}
