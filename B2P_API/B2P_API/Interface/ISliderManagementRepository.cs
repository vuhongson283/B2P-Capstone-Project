using B2P_API.DTOs.SliderDTOs;
using B2P_API.Models;

namespace B2P_API.Interface
{
	public interface ISliderManagementRepository
	{
		Task<List<Slider>> GetAllSlidersAsync(int pageNumber, int pageSize, string? search, int? statusId);
		Task<int> GetTotalSlidersAsync(string? search, int? statusId);
		Task<Slider?> GetSliderByIdAsync(int slideId);
		Task<Slider> CreateSliderAsync(Slider slider);
		Task<Slider?> UpdateSliderAsync(int slideId, Slider slider);
		Task<bool> ActiveSliderAsync(int slideId);
		Task<bool> UnActiveSliderAsync(int slideId);
		Task<List<Slider>> GetAllSlidersByStatusAsync(int pageNumber, int pageSize, int statusId);
		Task<bool> DeleteSliderAsync(int slideId);
	}
}
