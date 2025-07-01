using System.Threading.Tasks;
using B2P_API.DTOs.SliderDTOs;
using B2P_API.Interface;
using B2P_API.Response;

namespace B2P_API.Services
{
	/// <summary>
	/// Service xử lý nghiệp vụ cho Slider
	/// </summary>
	public class SliderManagementService
	{
		private readonly ISliderManagementRepository _repo;

		public SliderManagementService(ISliderManagementRepository repo)
		{
			_repo = repo;
		}

		/// <summary>
		/// Lấy danh sách slider với phân trang, tìm kiếm và lọc.
		/// </summary>
		public async Task<ApiResponse<PagedResponse<GetListSliderResponse>>> GetAllSlidersAsync(GetListSliderRequest request)
		{
			try
			{
				var paged = await _repo.GetAllSlidersAsync(
					request.PageNumber,
					request.PageSize,
					request.Search,
					request.StatusId
				);

				return new ApiResponse<PagedResponse<GetListSliderResponse>>
				{
					Success = true,
					Message = "Lấy danh sách slider thành công.",
					Status = 200,
					Data = paged
				};
			}
			catch
			{
				return new ApiResponse<PagedResponse<GetListSliderResponse>>
				{
					Success = false,
					Message = "Lỗi khi lấy danh sách slider.",
					Status = 500,
					Data = null
				};
			}
		}

	
		public Task<ApiResponse<GetSliderByIdResponse>> GetSliderByIdAsync(int slideId)
			=> _repo.GetSliderByIdAsync(slideId);

	
		public Task<ApiResponse<string>> CreateSliderAsync(CreateSliderRequest request)
			=> _repo.CreateSliderAsync(request);

	
		public Task<ApiResponse<string>> UpdateSliderAsync(int slideId, UpdateSliderRequest request)
			=> _repo.UpdateSliderAsync(slideId, request);

		
		public Task<ApiResponse<string>> ActiveSliderAsync(int slideId)
		{
			return _repo.ActiveSliderAsync(slideId);
		}

		
		public Task<ApiResponse<string>> UnActiveSliderAsync(int slideId)
		{
			return _repo.UnActiveSliderAsync(slideId);
		}

	
		public Task<PagedResponse<GetActiveSliderResponse>> GetAllSlidersByStatusAsync(int pageNumber, int pageSize)
			=> _repo.GetAllSlidersByStatusAsync(pageNumber, pageSize);

		
		public Task<ApiResponse<string>> DeleteSliderAsync(int slideId)
			=> _repo.DeleteSliderAsync(slideId);
	}
}
