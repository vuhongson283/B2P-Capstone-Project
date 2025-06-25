using B2P_API.DTOs;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Response;

namespace B2P_API.Services
{
    public class CourtServices
    {
        private readonly SportBookingDbContext _context;
        private readonly CourtRepository _repository;

        public CourtServices(SportBookingDbContext context, CourtRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        public async Task<ApiResponse<PagedResponse<CourtDTO>>> GetAllCourts(int pageNumber, int pageSize,
            string? search, int? status, int? categoryId)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0 || pageSize > 10) pageSize = 10;

            var paginatedResult = await _repository.GetAllCourts(
                pageNumber, pageSize, search, status, categoryId
            );

            var response = new PagedResponse<CourtDTO>
            {
                CurrentPage = paginatedResult.CurrentPage,
                ItemsPerPage = paginatedResult.ItemsPerPage,
                TotalItems = paginatedResult.TotalItems,
                TotalPages = paginatedResult.TotalPages,
                Items = (paginatedResult.Items)
            };

            return new ApiResponse<PagedResponse<CourtDTO>>
            {
                Success = true,
                Message = "Account retrieved with pagination successfully.",
                Status = 200,
                Data = response
            };
        }

        public async Task<ApiResponse<CourtDetailDTO>> GetCourtDetail(int courtId)
        {
            var court = await _repository.GetCourtDetail(courtId);

            return new ApiResponse<CourtDetailDTO>
            {
                Success = true,
                Message = "Get Court detail successful",
                Status = 200,
                Data = court
            };
        }

        public async Task<ApiResponse<Court>> CreateCourtAsync(CreateCourt request)
        {
            try
            {
                await _repository.CreateCourt(request);

                return new ApiResponse<Court>
                {
                    Success = true,
                    Message = "Court has been added!",
                    Status = 201,
                    Data = new Court
                    {
                        FacilityId = request.FacilityId,
                        StatusId = 1,
                        CourtName = request.CourtName,
                        CategoryId = request.CategoryId,
                        PricePerHour = request.PricePerHour
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Court>
                {
                    Success = false,
                    Message = "An error occurred: " + ex.Message,
                    Status = 500,
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<object>> UpdateCourt(UpdateCourtRequest request)
        {
            try
            {
                var court = await _repository.UpdateCourt(request);
                if (court == false)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Court not found.",
                        Status = 404,
                        Data = null
                    };
                }

                if (!court)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Fail to update court.",
                        Status = 500,
                        Data = null
                    };
                }

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Court updated successfully",
                    Status = 200,
                    Data = court
                };

            }
            catch(Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during update: " + ex.Message,
                    Status = 500,
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteCourt(int courtId)
        {
            try
            {
                if (await _repository.DeleteCourt(courtId) == false)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Court not found.",
                        Status = 404,
                        Data = false
                    };
                }

                await _repository.DeleteCourt(courtId);
                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Court deleted successfully",
                    Status = 201,
                    Data = true
                };
            }
            catch(Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred during delete: " + ex.Message,
                    Status = 500,
                    Data = false
                };
            }
        }

        public async Task<ApiResponse<object>> LockCourt(int courtId, int statusId)
        {
            try
            {
                var court = await _repository.LockCourt(courtId, statusId);
                if(court == false)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Court not found.",
                        Status = 404,
                        Data = null
                    };
                }

                if (!court)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Fail to lock court.",
                        Status = 500,
                        Data = null
                    };
                }

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Court locked successfully",
                    Status = 200,
                    Data = court
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during lock: " + ex.Message,
                    Status = 500,
                    Data = null
                };
            }

        }
    }
}
