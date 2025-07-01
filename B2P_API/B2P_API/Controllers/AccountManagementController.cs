using B2P_API.DTOs.Account;
using B2P_API.DTOs.CourtCategoryDTO;
using B2P_API.Interface;
using B2P_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountManagementController : ControllerBase
	{
		private readonly AccountManagementService _accountManagementService;
        private readonly IExcelExportService _excelExportService;

        public AccountManagementController(AccountManagementService accountManageService, IExcelExportService excelExportService)
		{
			_accountManagementService = accountManageService;
			_excelExportService = excelExportService;
        }
		[HttpPost("account-list")]
		public async Task<IActionResult> GetAccountList([FromBody] GetListAccountRequest request)
		{
			var response = await _accountManagementService.GetAllAccountsAsync(request);
			return StatusCode(response.Status, response);
		}
		[HttpGet("get-user/{userId}")]
		public async Task<IActionResult> GetAccountById([FromRoute] int userId)
		{
			var response = await _accountManagementService.GetAccountByIdAsync(userId);
			return StatusCode(response.Status, response);
		}

		[HttpPut("{userId}/ban")]
		public async Task<IActionResult> BanUser([FromRoute] int userId)
		{
			var response = await _accountManagementService.BanUserAsync(userId);
			return StatusCode(response.Status, response);
		}
		[HttpPut("{userId}/unban")]
		public async Task<IActionResult> UnBanUser([FromRoute] int userId)
		{
			var response = await _accountManagementService.UnBanUserAsync(userId);
			return StatusCode(response.Status, response);
		}
		[HttpDelete("{userId}")]
		public async Task<IActionResult> DeleteUser([FromRoute] int userId)
		{
			var resp = await _accountManagementService.DeleteUserAsync(userId);
			return StatusCode(resp.Status, resp);
		}

        [HttpPost("export-account-list")]
        public async Task<IActionResult> ExportAccountToExcel([FromBody] GetListAccountRequest request)
        {
            var accountsResponse = await _accountManagementService.GetAllAccountsAsync(request);
            if (!accountsResponse.Success)
            {
                return StatusCode(accountsResponse.Status, accountsResponse.Message);
            }

			var accounts = accountsResponse.Data.Items.ToList();
            var excelResponse = await _excelExportService.ExportToExcelAsync<GetListAccountResponse>(
    accounts,
    "Accounts");

            if (!excelResponse.Success)
            {
                return StatusCode(excelResponse.Status, excelResponse.Message);
            }

            var fileName = $"Accounts{DateTime.Now:yyyy-MM-dd}.xlsx";

            return File(excelResponse.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
