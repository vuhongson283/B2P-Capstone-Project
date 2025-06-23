using B2P_API.DTOs.FacilityDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;

namespace B2P_API.Services
{
    public class FacilityService
    {
        private readonly IFacilityRepository _facilityRepo;

        public FacilityService(IFacilityRepository facilityRepo)
        {
            _facilityRepo = facilityRepo;
        }
        public async Task<ApiResponse<PagedResponse<SearchFacilityResponse>>> SearchFacilities(SearchFormRequest request, int pageNumber = 1, int pageSize = 10)
        {
            var facilities = await _facilityRepo.SearchFacilities();
            var activeFacilities = facilities?.Where(f => f.StatusId == 1).ToList();

            if (activeFacilities == null || activeFacilities.Count == 0)
            {
                return new ApiResponse<PagedResponse<SearchFacilityResponse>>()
                {
                    Success = false,
                    Message = "No active facilities found.",
                    Status = 404,
                    Data = null
                };
            }

            // Apply filters
            var filteredFacilities = activeFacilities.AsQueryable();

            // Filter by name
            if (!string.IsNullOrEmpty(request.Name))
            {
                filteredFacilities = filteredFacilities.Where(f =>
                    f.FacilityName.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by type
            if (request.Type.HasValue)
            {
                filteredFacilities = filteredFacilities.Where(f => f.Courts.Any(x => x.CategoryId == request.Type.Value));
            }

            // Filter by city (in Location)
            if (!string.IsNullOrEmpty(request.City))
            {
                filteredFacilities = filteredFacilities.Where(f =>
                    !string.IsNullOrEmpty(f.Location) &&
                    f.Location.Contains(request.City, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by ward (in Location)
            if (!string.IsNullOrEmpty(request.Ward))
            {
                filteredFacilities = filteredFacilities.Where(f =>
                    !string.IsNullOrEmpty(f.Location) &&
                    f.Location.Contains(request.Ward, StringComparison.OrdinalIgnoreCase));
            }

            var filteredList = filteredFacilities.ToList();

            // Check if no results after filtering
            if (filteredList.Count == 0)
            {
                return new ApiResponse<PagedResponse<SearchFacilityResponse>>()
                {
                    Success = false,
                    Message = "No facilities found matching the search criteria.",
                    Status = 404,
                    Data = null
                };
            }

            // Get all TimeSlot from filtered facilities
            var allTimeSlots = filteredList
                .SelectMany(f => f.TimeSlots ?? new List<TimeSlot>())
                .Where(ts => ts.StartTime.HasValue && ts.EndTime.HasValue)
                .ToList();

            TimeOnly? minStartTime = null;
            TimeOnly? maxEndTime = null;

            if (allTimeSlots.Any())
            {
                minStartTime = RoundTimeToMinute(allTimeSlots.Min(ts => ts.StartTime.Value));
                maxEndTime = RoundTimeToMinute(allTimeSlots.Max(ts => ts.EndTime.Value));
            }

            var results = filteredList.Select(f => new SearchFacilityResponse
            {
                FacilityId = f.FacilityId,
                FacilityName = f.FacilityName,
                Location = f.Location,
                OpenTime = $"{minStartTime?.ToString("HH:mm")} - {maxEndTime?.ToString("HH:mm")}",
            }).ToList();

            // Pagination
            var totalItems = results.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Ensure pageNumber is valid
            pageNumber = Math.Max(1, Math.Min(pageNumber, totalPages));

            var pagedItems = results
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pagedResponse = new PagedResponse<SearchFacilityResponse>
            {
                CurrentPage = pageNumber,
                ItemsPerPage = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = pagedItems 
            };

            return new ApiResponse<PagedResponse<SearchFacilityResponse>>()
            {
                Success = true,
                Message = $"Found {totalItems} facilities matching search criteria.",
                Status = 200,
                Data = pagedResponse
            };
        }

        private TimeOnly RoundTimeToMinute(TimeOnly time)
        {
            if (time.Second >= 30)
            {
                time = time.AddMinutes(1);
            }
            return new TimeOnly(time.Hour, time.Minute);
        }


    }
}
