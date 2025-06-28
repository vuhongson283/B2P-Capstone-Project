using B2P_API.DTOs.FacilityDTOs;
using B2P_API.DTOs.ImageDTOs;
using B2P_API.DTOs.StatuDTOs;
using B2P_API.DTOs.FacilityDTO;     // chứa SearchFacilityResponse, SearchFormRequest
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace B2P_API.Services
{
	public class FacilityService : IFacilityService
	{
		private readonly IFacilityRepository _repo;
		public FacilityService(IFacilityRepository repo)
		{
			_repo = repo;
		}

		// 1) Cho chủ cơ sở: list all facilities by user
		public async Task<ApiResponse<PagedResponse<FacilityWithCourtCountDto>>> GetFacilitiesByUserAsync(
			int userId, string? facilityName = null, int? statusId = null)
		{
			if (userId <= 0)
				return new ApiResponse<PagedResponse<FacilityWithCourtCountDto>>
				{
					Success = false,
					Message = "UserId không hợp lệ",
					Status = 400,
					Data = new PagedResponse<FacilityWithCourtCountDto>
					{
						CurrentPage = 1,
						ItemsPerPage = 10,
						Items = new List<FacilityWithCourtCountDto>(),
						TotalItems = 0,
						TotalPages = 0,
					}
				};

			try
			{
				var list = await _repo.GetByUserIdAsync(userId);
				var filtered = list.AsEnumerable();

				if (!string.IsNullOrEmpty(facilityName))
					filtered = filtered.Where(f =>
						f.FacilityName.Contains(facilityName, StringComparison.OrdinalIgnoreCase));
				if (statusId.HasValue)
					filtered = filtered.Where(f => f.StatusId == statusId.Value);

				var mapped = filtered.Select(f => new FacilityWithCourtCountDto
				{
					FacilityId = f.FacilityId,
					FacilityName = f.FacilityName,
					CourtCount = f.Courts?.Count ?? 0,
					Status = f.Status == null ? null : new StatusDto
					{
						StatusId = f.Status.StatusId,
						StatusName = f.Status.StatusName,
						StatusDescription = f.Status.StatusDescription
					},
					Images = f.Images?.Select(i => new ImageDto
					{
						ImageId = i.ImageId,
						ImageUrl = i.ImageUrl,
						Order = i.Order,
						Caption = i.Caption
					}).ToList()
				}).ToList();

				int total = mapped.Count;
				int perPage = 10;
				int pages = (int)Math.Ceiling((double)total / perPage);
				var items = mapped.Take(perPage).ToList();

				return new ApiResponse<PagedResponse<FacilityWithCourtCountDto>>
				{
					Success = true,
					Message = "Tải dữ liệu cơ sở thành công",
					Status = 200,
					Data = new PagedResponse<FacilityWithCourtCountDto>
					{
						CurrentPage = 1,
						ItemsPerPage = perPage,
						Items = items,
						TotalItems = total,
						TotalPages = pages
					}
				};
			}
			catch (Exception ex)
			{
				return new ApiResponse<PagedResponse<FacilityWithCourtCountDto>>
				{
					Success = false,
					Message = MessagesCodes.MSG_06 + ex.Message,
					Status = 500,
					Data = new PagedResponse<FacilityWithCourtCountDto>
					{
						CurrentPage = 1,
						ItemsPerPage = 10,
						Items = new List<FacilityWithCourtCountDto>(),
						TotalItems = 0,
						TotalPages = 0
					}
				};
			}
		}

		// 2) Cho người chơi: search facilities
		public async Task<ApiResponse<PagedResponse<SearchFacilityResponse>>> GetAllFacilitiesByPlayerAsync(
			SearchFormRequest request, int pageNumber = 1, int pageSize = 10)
		{
			try
			{
				var all = await _repo.GetAllFacilitiesByPlayer();
				var active = all?.Where(f => f.StatusId == 1).ToList()
							 ?? new List<Facility>();

				if (!active.Any())
					return new ApiResponse<PagedResponse<SearchFacilityResponse>>
					{
						Success = false,
						Message = MessagesCodes.MSG_44,
						Status = 404,
						Data = null
					};

				var q = active.AsQueryable();
				if (!string.IsNullOrEmpty(request.Name))
					q = q.Where(f =>
						f.FacilityName.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
				if (request.Type != null && request.Type.Any())
					q = q.Where(f =>
						f.Courts.Any(c => request.Type.Contains((int)c.CategoryId)));
				if (!string.IsNullOrEmpty(request.City))
					q = q.Where(f =>
						!string.IsNullOrEmpty(f.Location) &&
						f.Location.Contains(request.City, StringComparison.OrdinalIgnoreCase));
				if (!string.IsNullOrEmpty(request.Ward))
					q = q.Where(f =>
						!string.IsNullOrEmpty(f.Location) &&
						f.Location.Contains(request.Ward, StringComparison.OrdinalIgnoreCase));

				var list = q.ToList();
				if (!list.Any())
					return new ApiResponse<PagedResponse<SearchFacilityResponse>>
					{
						Success = false,
						Message = "No facilities found matching the search criteria.",
						Status = 404,
						Data = null
					};

				var dto = list.Select(f =>
				{
					var price = GetMinPriceForSearchedCategories(f, request.Type);
					var slots = f.TimeSlots?.Where(ts => ts.StartTime.HasValue && ts.EndTime.HasValue).ToList()
								?? new List<TimeSlot>();

					TimeOnly? minT = null, maxT = null;
					if (slots.Any())
					{
						minT = RoundTimeToMinute(slots.Min(ts => ts.StartTime.Value));
						maxT = RoundTimeToMinute(slots.Max(ts => ts.EndTime.Value));
					}

					var minD = f.TimeSlots?.Where(ts => ts.Discount.HasValue).Any() == true
						? f.TimeSlots.Where(ts => ts.Discount.HasValue).Min(ts => ts.Discount.Value)
						: 0;
					var maxD = f.TimeSlots?.Where(ts => ts.Discount.HasValue).Any() == true
						? f.TimeSlots.Where(ts => ts.Discount.HasValue).Max(ts => ts.Discount.Value)
						: 0;

					return new SearchFacilityResponse
					{
						FacilityId = f.FacilityId,
						FacilityName = f.FacilityName,
						Location = !string.IsNullOrEmpty(f.Location) && f.Location.Contains("$$")
							? f.Location.Substring(f.Location.IndexOf("$$") + 2)
							: f.Location ?? string.Empty,
						OpenTime = (minT.HasValue && maxT.HasValue)
							? minT.Value.ToString("HH:mm") + " - " + maxT.Value.ToString("HH:mm")
							: "Chưa có lịch",
						FirstImage = f.Images?.OrderBy(i => i.Order).FirstOrDefault()?.ImageUrl,
						AverageRating = CalculateAverageRating(f),
						PricePerHour = price,
						MinPrice = price * minD,
						MaxPrice = price * maxD
					};
				}).ToList();

				// sort
				if (request.Order == 1) dto = dto.OrderBy(r => r.PricePerHour).ToList();
				else if (request.Order == 2) dto = dto.OrderByDescending(r => r.PricePerHour).ToList();
				else if (request.Order == 3) dto = dto.OrderByDescending(r => r.AverageRating).ToList();

				int total = dto.Count;
				int pages = (int)Math.Ceiling((double)total / pageSize);
				pageNumber = Math.Clamp(pageNumber, 1, pages);
				var page = dto.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToL *
