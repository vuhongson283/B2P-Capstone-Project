using System;
using System.Linq;
using System.Threading.Tasks;
using B2P_API.DTOs.SliderDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using Google.Apis.Drive.v3.Data;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repository
{
	public class SliderManagementRepository : ISliderManagementRepository
	{
		private readonly SportBookingDbContext _context;

		public SliderManagementRepository(SportBookingDbContext context)
		{
			_context = context;
		}

		public async Task<PagedResponse<GetListSliderResponse>> GetAllSlidersAsync(
			int pageNumber,
			int pageSize,
			string? search,
			int? statusId)
		{
			var query = _context.Sliders
				.Include(s => s.Status)
				.Include(s => s.Images)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(search))
			{
				query = query.Where(s =>
					(s.SlideDescription ?? string.Empty)
						.Contains(search, StringComparison.OrdinalIgnoreCase) ||
					(s.SlideUrl ?? string.Empty)
						.Contains(search, StringComparison.OrdinalIgnoreCase));
			}

			if (statusId.HasValue)
			{
				query = query.Where(s => s.StatusId == statusId.Value);
			}

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var sliders = await query
				.OrderBy(s => s.SlideId)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var items = sliders.Select(s => new GetListSliderResponse
			{
				SlideId = s.SlideId,
				SlideDescription = s.SlideDescription,
				SlideUrl = s.SlideUrl,
				StatusName = s.Status?.StatusName,
				ImageUrl = s.Images
					.OrderBy(img => img.Order)
					.FirstOrDefault()?.ImageUrl
			}).ToList();

			return new PagedResponse<GetListSliderResponse>
			{
				CurrentPage = pageNumber,
				ItemsPerPage = pageSize,
				TotalItems = totalItems,
				TotalPages = totalPages,
				Items = items
			};
		}

		public async Task<ApiResponse<GetSliderByIdResponse>> GetSliderByIdAsync(int slideId)
		{
			var slider = await _context.Sliders
				.Include(s => s.Status)
				.Include(s => s.Images)
				.AsNoTracking()
				.FirstOrDefaultAsync(s => s.SlideId == slideId);

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

			var imageUrl = slider.Images
				.OrderBy(img => img.Order)
				.FirstOrDefault()?.ImageUrl;

			var dto = new GetSliderByIdResponse
			{
				SlideId = slider.SlideId,
				SlideUrl = slider.SlideUrl,
				SlideDescription = slider.SlideDescription,
				StatusId = slider.StatusId,
				StatusName = slider.Status?.StatusName,
				ImageUrl = imageUrl
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

			_context.Sliders.Add(slider);
			await _context.SaveChangesAsync();

			return new ApiResponse<string>
			{
				Success = true,
				Message = "Tạo slider thành công.",
				Status = 201,
				Data = slider.SlideId.ToString()
			};
		}

		public async Task<ApiResponse<string>> UpdateSliderAsync(int slideId, UpdateSliderRequest request)
		{
			var slider = await _context.Sliders.FindAsync(slideId);
			if (slider == null)
			{
				return new ApiResponse<string>
				{
					Success = false,
					Message = "Không tìm thấy slider.",
					Status = 404,
					Data = null
				};
			}

			slider.SlideUrl = request.SlideUrl;
			slider.SlideDescription = request.SlideDescription;

			_context.Sliders.Update(slider);
			await _context.SaveChangesAsync();

			return new ApiResponse<string>
			{
				Success = true,
				Message = "Cập nhật slider thành công.",
				Status = 200,
				Data = slider.SlideId.ToString()
			};
		}

		public async Task<ApiResponse<string>> DeleteSliderAsync(int slideId)
		{
			var slider = await _context.Sliders
				.Include(s => s.Images)
				.FirstOrDefaultAsync(s => s.SlideId == slideId);

			if (slider == null)
			{
				return new ApiResponse<string>
				{
					Success = false,
					Message = "Không tìm thấy slider.",
					Status = 404,
					Data = null
				};
			}

			if (slider.Images.Any())
				_context.Images.RemoveRange(slider.Images);

			_context.Sliders.Remove(slider);
			await _context.SaveChangesAsync();

			return new ApiResponse<string>
			{
				Success = true,
				Message = "Xóa slider thành công.",
				Status = 200,
				Data = slideId.ToString()
			};
		}

		public async Task<ApiResponse<string>> ActiveSliderAsync(int slideId)
		{
			var slider = await _context.Sliders.FindAsync(slideId);
			if (slider == null)
				return new ApiResponse<string>
				{
					Success = false,
					Message = "Không tìm thấy slider.",
					Status = 404,
					Data = null
				};
			if (slider.StatusId == 1)
				return new ApiResponse<string>
				{
					Success = false,
					Message = "Slider đang được bật rồi.",
					Status = 404,
					Data = null
				};

			slider.StatusId = 1;
			_context.Sliders.Update(slider);
			await _context.SaveChangesAsync();

			return new ApiResponse<string>
			{
				Success = true,
				Message = "Bật Slider thành công",
				Status = 200,
				Data = slideId.ToString()
			};
		}

		public async Task<ApiResponse<string>> UnActiveSliderAsync(int slideId)
		{
			var slider = await _context.Sliders.FindAsync(slideId);
			if (slider == null)
				return new ApiResponse<string>
				{
					Success = false,
					Message = "Không tìm thấy slider.",
					Status = 404,
					Data = null
				};
			if (slider.StatusId == 2)
				return new ApiResponse<string>
				{
					Success = false,
					Message = "Slider đang được tắt rồi.",
					Status = 404,
					Data = null
				};

			slider.StatusId = 2;
			_context.Sliders.Update(slider);
			await _context.SaveChangesAsync();

			return new ApiResponse<string>
			{
				Success = true,
				Message = "Tắt Slider thành công",
				Status = 200,
				Data = slideId.ToString()
			};
		}

		public async Task<PagedResponse<GetActiveSliderResponse>> GetAllSlidersByStatusAsync(int pageNumber, int pageSize)
		{
			// Lấy sliders có status = 1
			var query = _context.Sliders
				.Include(s => s.Images)
				.Where(s => s.StatusId == 1)
				.AsQueryable();

			// Tổng số bản ghi
			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			// Phân trang và lấy dữ liệu
			var sliders = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// Map sang DTO chỉ lấy SlideUrl và ImageUrl
			var items = sliders.Select(s => new GetActiveSliderResponse
			{
				SlideUrl = s.SlideUrl,
				ImageUrl = s.Images.OrderBy(img => img.Order).FirstOrDefault()?.ImageUrl
			}).ToList();

			return new PagedResponse<GetActiveSliderResponse>
			{
				CurrentPage = pageNumber,
				ItemsPerPage = pageSize,
				TotalItems = totalItems,
				TotalPages = totalPages,
				Items = items
			};
		}

	}
}

