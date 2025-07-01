using B2P_API.DTOs.SliderDTOs;
using B2P_API.Response;

namespace B2P_API.Interface
{
	public interface ISliderManagementRepository
	{
		Task<PagedResponse<GetListSliderResponse>> GetAllSlidersAsync(int pageNumber,int pageSize,string? search,int? statusId);
		Task<ApiResponse<GetSliderByIdResponse>> GetSliderByIdAsync(int slideId);
		Task<ApiResponse<string>> CreateSliderAsync(CreateSliderRequest request);
		Task<ApiResponse<string>> UpdateSliderAsync(int slideId, UpdateSliderRequest request);
		Task<ApiResponse<string>> ActiveSliderAsync(int slideId);
		Task<ApiResponse<string>> UnActiveSliderAsync(int slideId);
		Task<PagedResponse<GetActiveSliderResponse>> GetAllSlidersByStatusAsync(int pageNumber, int pageSize);
		Task<ApiResponse<string>> DeleteSliderAsync(int slideId);
	}
}
