using B2P_API.DTOs.Account;
using B2P_API.DTOs.CourtCategoryDTO;
using B2P_API.Interface;
using B2P_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace B2P_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountManagementController : ControllerBase
	{
		private readonly AccountManagementService _accountManagementService;

        public AccountManagementController(AccountManagementService accountManageService)
		{
			_accountManagementService = accountManageService;
        }
		[HttpPost("account-list")]
		[Authorize(Roles = "1")]
        public async Task<IActionResult> GetAccountList([FromBody] GetListAccountRequest request)
		{
			if (request == null)
			{
				return BadRequest(new
				{
					Success = false,
					Message = "Request không được để trống.",
					Status = 400,
					Data = (object)null
				});
			}

			var response = await _accountManagementService.GetAllAccountsAsync(request);
			return StatusCode(response.Status, response);
		}

		[HttpGet("get-user/{userId}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> GetAccountById([FromRoute] int userId)
		{
			var response = await _accountManagementService.GetAccountByIdAsync(userId);
			return StatusCode(response.Status, response);
		}

		[HttpPut("{userId}/ban")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> BanUser([FromRoute] int userId)
		{
			var response = await _accountManagementService.BanUserAsync(userId);
			return StatusCode(response.Status, response);
		}
		[HttpPut("{userId}/unban")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> UnBanUser([FromRoute] int userId)
		{
			var response = await _accountManagementService.UnBanUserAsync(userId);
			return StatusCode(response.Status, response);
		}
		[HttpDelete("{userId}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> DeleteUser([FromRoute] int userId)
		{
			var resp = await _accountManagementService.DeleteUserAsync(userId);
			return StatusCode(resp.Status, resp);
		}

    }
}
