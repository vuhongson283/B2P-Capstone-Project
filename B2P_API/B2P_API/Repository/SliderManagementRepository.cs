using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Utils;
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

		public async Task<List<Slider>> GetAllSlidersAsync(
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
					(s.SlideDescription ?? "").Contains(search) ||
					(s.SlideUrl ?? "").Contains(search));
			}

			if (statusId.HasValue)
			{
				query = query.Where(s => s.StatusId == statusId.Value);
			}

			return await query
				.OrderBy(s => s.SlideId)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();
		}

		public async Task<int> GetTotalSlidersAsync(string? search, int? statusId)
		{
			var query = _context.Sliders.AsQueryable();

			if (!string.IsNullOrWhiteSpace(search))
			{
				query = query.Where(s =>
					(s.SlideDescription ?? "").Contains(search) ||
					(s.SlideUrl ?? "").Contains(search));
			}

			if (statusId.HasValue)
			{
				query = query.Where(s => s.StatusId == statusId.Value);
			}

			return await query.CountAsync();
		}

		public async Task<Slider?> GetSliderByIdAsync(int slideId)
		{
			return await _context.Sliders
				.Include(s => s.Status)
				.Include(s => s.Images)
				.FirstOrDefaultAsync(s => s.SlideId == slideId);
		}

		public async Task<Slider> CreateSliderAsync(Slider slider)
		{
			_context.Sliders.Add(slider);
			await _context.SaveChangesAsync();
			return slider;
		}

		public async Task<Slider?> UpdateSliderAsync(int slideId, Slider slider)
		{
			var existing = await _context.Sliders.FindAsync(slideId);
			if (existing == null)
				return null;

			existing.SlideUrl = slider.SlideUrl;
			existing.SlideDescription = slider.SlideDescription;
			_context.Sliders.Update(existing);
			await _context.SaveChangesAsync();

			return existing;
		}

		public async Task<bool> ActiveSliderAsync(int slideId)
		{
			var slider = await _context.Sliders.FindAsync(slideId);
			if (slider == null)
				return false;

			if (slider.StatusId == 1)
				return false;

			slider.StatusId = 1;
			_context.Sliders.Update(slider);
			await _context.SaveChangesAsync();

			return true;
		}

		public async Task<bool> UnActiveSliderAsync(int slideId)
		{
			var slider = await _context.Sliders.FindAsync(slideId);
			if (slider == null)
				return false;

			if (slider.StatusId == 2)
				return false;

			slider.StatusId = 2;
			_context.Sliders.Update(slider);
			await _context.SaveChangesAsync();

			return true;
		}

		public async Task<List<Slider>> GetAllSlidersByStatusAsync(int pageNumber, int pageSize, int statusId)
		{
			return await _context.Sliders
				.Include(s => s.Images)
				.Where(s => s.StatusId == statusId)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();
		}

		public async Task<bool> DeleteSliderAsync(int slideId)
		{
			var slider = await _context.Sliders
				.Include(s => s.Images)
				.FirstOrDefaultAsync(s => s.SlideId == slideId);

			if (slider == null)
				return false;

			if (slider.Images.Any())
				_context.Images.RemoveRange(slider.Images);

			_context.Sliders.Remove(slider);
			await _context.SaveChangesAsync();

			return true;
		}
	}
}
