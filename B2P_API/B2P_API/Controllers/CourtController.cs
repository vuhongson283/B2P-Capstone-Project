using B2P_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourtController : ControllerBase
    {
        private readonly CourtCategoryService _courtCategoryService;
        public CourtController(CourtCategoryService courtCategoryService)
        {
            _courtCategoryService = courtCategoryService;
        }

        [HttpGet("get-all-court-categories")]
        public async Task<IActionResult> GetAllCourtCategories()
        {
            var response = await _courtCategoryService.GetAllCourtCategoriesAsync();
            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }
            return Ok(response);
        }
    }
}
