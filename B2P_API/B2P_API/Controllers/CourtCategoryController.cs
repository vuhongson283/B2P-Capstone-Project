using B2P_API.DTOs.CourtCategoryDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourtCategoryController : ControllerBase
    {
        private readonly CourtCategoryService _courtCategoryService;
        public CourtCategoryController(CourtCategoryService courtCategoryService)
        {
            _courtCategoryService = courtCategoryService;
        }

        [HttpGet("get-all-court-categories")]
        public async Task<IActionResult> GetAllCourtCategories(string? search, int pageNumber = 1, int pageSize = 10)
        {
            var response = await _courtCategoryService.GetAllCourtCategoriesAsync(search, pageNumber, pageSize);
            return StatusCode(response.Status, response);
        }

        [HttpPost("add-court-category")]
        public async Task<IActionResult> AddCourtCategory([FromBody] CourtCategoryAddRequest request)
        {
            var response = await _courtCategoryService.AddCourtCategoryAsync(request);
            return StatusCode(response.Status, response);

        }
        [HttpPut("update-court-category")]
        public async Task<IActionResult> UpdateCourtCategory([FromBody] CourtCategoryUpdateRequest request)
        {
            var response = await _courtCategoryService.UpdateCourtCategoryAsync(request);
            return StatusCode(response.Status, response);

        }

        [HttpGet("get-court-category-by-id")]
        public async Task<IActionResult> GetCourtCategoryById(int categoryId)
        {
            var response = await _courtCategoryService.GetCourtCategoryByIdAsync(categoryId);
            return StatusCode(response.Status, response);

        }

        [HttpDelete("delete-court-category")]
        public async Task<IActionResult> DeleteCourtCategory(int categoryId)
        {
            var response = await _courtCategoryService.DeleteCourtCategoryAsync(categoryId);
            return StatusCode(response.Status, response);
        }
    }

}
