using Microsoft.AspNetCore.Mvc;
using B2P_API.DTOs.SliderDTOs;
using B2P_API.Response;
using B2P_API.Services;
using System.Threading.Tasks;

namespace B2P_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SliderManagementController : ControllerBase
	{
		private readonly SliderManagementService _service;

		public SliderManagementController(SliderManagementService service)
		{
			_service = service;
		}

		
		[HttpPost("slider-list")]
		public async Task<IActionResult> GetAllSliders([FromBody] GetListSliderRequest request)
		{
			var response = await _service.GetAllSlidersAsync(request);
			return StatusCode(response.Status, response);
		}

	
		[HttpGet("get-slider/{slideId}")]
		public async Task<IActionResult> GetSliderById([FromRoute] int slideId)
		{
			var response = await _service.GetSliderByIdAsync(slideId);
			return StatusCode(response.Status, response);
		}

		
		[HttpPost("create-slider")]
		public async Task<IActionResult> CreateSlider([FromBody] CreateSliderRequest request)
		{
			var response = await _service.CreateSliderAsync(request);
			return StatusCode(response.Status, response);
		}

		
		[HttpPut("{slideId}")]
		public async Task<IActionResult> UpdateSlider([FromRoute] int slideId, [FromBody] UpdateSliderRequest request)
		{
			var response = await _service.UpdateSliderAsync(slideId, request);
			return StatusCode(response.Status, response);
		}

		
		[HttpDelete("{slideId}")]
		public async Task<IActionResult> DeleteSlider([FromRoute] int slideId)
		{
			var response = await _service.DeleteSliderAsync(slideId);
			return StatusCode(response.Status, response);
		}

	
		[HttpPut("{slideId}/activate")]
		public async Task<IActionResult> ActivateSlider([FromRoute] int slideId)
		{
			var response = await _service.ActiveSliderAsync(slideId);
			return StatusCode(response.Status, response);
		}

	
		[HttpPut("{slideId}/deactivate")]
		public async Task<IActionResult> DeactivateSlider([FromRoute] int slideId)
		{
			var response = await _service.UnActiveSliderAsync(slideId);
			return StatusCode(response.Status, response);
		}

		
		[HttpGet("get-all-active-sliders/{pageNumber}/{pageSize}")]
		public async Task<IActionResult> GetActiveSliders([FromRoute] int pageNumber, [FromRoute] int pageSize)
		{
			var paged = await _service.GetAllSlidersByStatusAsync(pageNumber, pageSize);
			return Ok(paged);
		}
	}
}
