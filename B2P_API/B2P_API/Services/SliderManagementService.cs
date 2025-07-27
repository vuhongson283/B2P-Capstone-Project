using B2P_API.DTOs.SliderDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;

namespace B2P_API.Services
{
	public class SliderManagementService
	{
		private readonly ISliderManagementRepository _repo;

		public SliderManagementService(ISliderManagementRepository repo)
		{
			_repo = repo;
		}

		public async Task<ApiResponse<PagedResponse<GetListSliderResponse>>> GetAllSlidersAsync(GetListSliderRequest request)
		{
			try
			{
				var sliders = await _repo.GetAllSlidersAsync(
					request.PageNumber,
					request.PageSize,
					request.Search,
					request.StatusId
				);

				var totalItems = await _repo.GetTotalSlidersAsync(
					request.Search,
					request.StatusId
				);

				var items = sliders.Select(s => new GetListSliderResponse
				{
					SlideId = s.SlideId,
					SlideDescription = s.SlideDescription,
					SlideUrl = s.SlideUrl,
					StatusName = s.Status?.StatusName,
					ImageUrl = s.Images?.OrderBy(img => img.Order).FirstOrDefault()?.ImageUrl
				}).ToList();

				var paged = new PagedResponse<GetListSliderResponse>
				{
					CurrentPage = request.PageNumber,
					ItemsPerPage = request.PageSize,
					TotalItems = totalItems,
					TotalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize),
					Items = items
				};

				return new ApiResponse<PagedResponse<GetListSliderResponse>>
				{
					Success = true,
					Message = "Lấy danh sách slider thành công.",
					Status = 200,
					Data = paged
				};
			}
			catch (Exception ex)
			{
				return new ApiResponse<PagedResponse<GetListSliderResponse>>
				{
					Success = false,
					Message = $"Lỗi hệ thống: {ex.Message}",
					Status = 500,
					Data = null
				};
			}
		}

		public async Task<ApiResponse<GetSliderByIdResponse>> GetSliderByIdAsync(int slideId)
		{
			var slider = await _repo.GetSliderByIdAsync(slideId);
			if (slider == null)
			{
				return new ApiResponse<GetSliderByIdResponse>
				{
					Success = false,
					Message = "Không tìm thấy slider.",
					Status = 404,
					Data = null
				};
			}

			var dto = new GetSliderByIdResponse
			{
				SlideId = slider.SlideId,
				SlideUrl = slider.SlideUrl,
				SlideDescription = slider.SlideDescription,
				StatusId = slider.StatusId,
				StatusName = slider.Status?.StatusName,
				ImageUrl = slider.Images?.OrderBy(img => img.Order).FirstOrDefault()?.ImageUrl,
				ImageId = slider.Images?.OrderBy(img => img.Order).FirstOrDefault()?.ImageId
			};

			return new ApiResponse<GetSliderByIdResponse>
			{
				Success = true,
				Message = "Lấy chi tiết slider thành công.",
				Status = 200,
				Data = dto
			};
		}

		public async Task<ApiResponse<string>> CreateSliderAsync(CreateSliderRequest request)
		{
			var slider = new Slider
			{
				SlideUrl = request.SlideUrl,
				SlideDescription = request.SlideDescription,
				StatusId = 1
			};

			var created = await _repo.CreateSliderAsync(slider);

			return new ApiResponse<string>
			{
				Success = true,
				Message = "Tạo slider thành công.",
				Status = 201,
				Data = created.SlideId.ToString()
			};
		}

		public async Task<ApiResponse<string>> UpdateSliderAsync(int slideId, UpdateSliderRequest request)
		{
			var updateData = new Slider
			{
				SlideUrl = request.SlideUrl,
				SlideDescription = request.SlideDescription
			};

			var updated = await _repo.UpdateSliderAsync(slideId, updateData);
			if (updated == null)
			{
				return new ApiResponse<string>
				{
					Success = false,
					Message = "Không tìm thấy slider.",
					Status = 404,
					Data = null
				};
			}

			return new ApiResponse<string>
			{
				Success = true,
				Message = "Cập nhật slider thành công.",
				Status = 200,
				Data = updated.SlideId.ToString()
			};
		}

		public async Task<ApiResponse<string>> ActiveSliderAsync(int slideId)
		{
			var result = await _repo.ActiveSliderAsync(slideId);
			if (!result)
			{
				return new ApiResponse<string>
				{
					Success = false,
					Message = "Không thể bật slider. Có thể slider không tồn tại hoặc đã được bật trước đó.",
					Status = 400,
					Data = null
				};
			}

			return new ApiResponse<string>
			{
				Success = true,
				Message = "Bật slider thành công.",
				Status = 200,
				Data = slideId.ToString()
			};
		}

		public async Task<ApiResponse<string>> UnActiveSliderAsync(int slideId)
		{
			var result = await _repo.UnActiveSliderAsync(slideId);
			if (!result)
			{
				return new ApiResponse<string>
				{
					Success = false,
					Message = "Không thể tắt slider. Có thể slider không tồn tại hoặc đã được tắt trước đó.",
					Status = 400,
					Data = null
				};
			}

			return new ApiResponse<string>
			{
				Success = true,
				Message = "Tắt slider thành công.",
				Status = 200,
				Data = slideId.ToString()
			};
		}

		public async Task<ApiResponse<PagedResponse<GetActiveSliderResponse>>> GetAllSlidersByStatusAsync(int pageNumber, int pageSize)
		{
			var sliders = await _repo.GetAllSlidersByStatusAsync(pageNumber, pageSize, 1);

			var totalItems = sliders.Count;
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var items = sliders.Select(s => new GetActiveSliderResponse
			{
				SlideUrl = s.SlideUrl,
				ImageUrl = s.Images?.OrderBy(img => img.Order).FirstOrDefault()?.ImageUrl
			}).ToList();

			var paged = new PagedResponse<GetActiveSliderResponse>
			{
				CurrentPage = pageNumber,
				ItemsPerPage = pageSize,
				TotalItems = totalItems,
				TotalPages = totalPages,
				Items = items
			};

			return new ApiResponse<PagedResponse<GetActiveSliderResponse>>
			{
				Success = true,
				Message = "Lấy danh sách slider đang bật thành công.",
				Status = 200,
				Data = paged
			};
		}

		public async Task<ApiResponse<string>> DeleteSliderAsync(int slideId)
		{
			var result = await _repo.DeleteSliderAsync(slideId);
			if (!result)
			{
				return new ApiResponse<string>
				{
					Success = false,
					Message = "Xóa slider thất bại. Slider không tồn tại.",
					Status = 404,
					Data = null
				};
			}

			return new ApiResponse<string>
			{
				Success = true,
				Message = "Xóa slider thành công.",
				Status = 200,
				Data = slideId.ToString()
			};
		}
	}
}
