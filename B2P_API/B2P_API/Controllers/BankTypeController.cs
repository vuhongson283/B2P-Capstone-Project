using B2P_API.DTOs.BankTypeDTOs;
using B2P_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankTypeController : ControllerBase
    {
        private readonly BankAccountService _bankAccountService;

        public BankTypeController(BankAccountService bankAccountService)
        {
            _bankAccountService = bankAccountService;
        }

        [HttpGet("get-all-bank-type")]
        public async Task<IActionResult> GetAllBankType(string? search, int pageNumber = 1, int pageSize = 10)
        {
            var response = await _bankAccountService.GetAllBankTypeAsync(search, pageNumber, pageSize);
            return StatusCode(response.Status, response);
        }

        [HttpPost("create-new-bank-type")]
        public async Task<IActionResult> CreateNewBankType([FromBody] CreateBankTypeRequest request)
        {
            var response = await _bankAccountService.CreateBankTypeAsync(request);
            return StatusCode(response.Status, response);
        }

        [HttpPut("update-bank-type/{id}")]
        public async Task<IActionResult> UpdateBankType(int id, [FromBody] UpdateBankTypeRequest request)
        {
            var response = await _bankAccountService.UpdateBankTypeAsync(id, request);
            return StatusCode(response.Status, response);
        }

        [HttpDelete("delete-bank-type/{id}")]
        public async Task<IActionResult> DeleteBankType(int id)
        {
            var response = await _bankAccountService.DeleteBankTypeAsync(id);
            return StatusCode(response.Status, response);
        }
    }
}
