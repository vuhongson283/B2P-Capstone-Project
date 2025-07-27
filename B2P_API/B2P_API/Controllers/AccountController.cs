using B2P_API.DTOs.Account;
using B2P_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		private readonly AccountService _accountService;
		public AccountController(AccountService accountService)
		{
			_accountService = accountService;
		}
		[HttpPost("register-court-owner")]
		public async Task<IActionResult> Register([FromBody] RegisterAccountRequest request)
		{
			var result = await _accountService.RegisterAccountAsync(request);

			// Debug log
			Console.WriteLine($"Returning response: Status={result.Status}, Success={result.Success}, Message={result.Message}");

			return StatusCode(result.Status, result);
		}
	}
}
