using Microsoft.AspNetCore.Mvc;
using B2P_API.Services;
using B2P_API.DTOs;
using B2P_API.Models;
using B2P_API.Response;

namespace B2P_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlogController : ControllerBase
{
    private readonly BlogService _blogService;

    public BlogController(BlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] BlogQueryParameters query)
    {
        var response = await _blogService.GetPagedAsync(query);
        return StatusCode(response.Status, response);
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _blogService.GetByIdAsync(id);
        return StatusCode(response.Status, response);
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBlogDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Dữ liệu không hợp lệ.",
                Status = 400,
                Data = null
            });
        }

        var response = await _blogService.CreateAsync(dto);
        return StatusCode(response.Status, response);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBlogDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Dữ liệu không hợp lệ.",
                Status = 400,
                Data = null
            });
        }

        var response = await _blogService.UpdateAsync(id, dto);
        return StatusCode(response.Status, response);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int userId)
    {
        var response = await _blogService.DeleteAsync(id, userId);
        return StatusCode(response.Status, response);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(
    int userId,
    [FromQuery] int page = 1,
    [FromQuery] int size = 10,
    [FromQuery] string sortBy = "postAt",
    [FromQuery] string sortDir = "desc")
    {
        var result = await _blogService.GetByUserIdAsync(userId, page, size, sortBy, sortDir);
        return StatusCode(result.Status, result);
    }


}
