using B2P_API.DTOs.CourtManagementDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Response;
using Google.Apis.Drive.v3.Data;

namespace B2P_API.Services
{
    public class CourtServices
    {
        private readonly ICourtRepository _repository;

        public CourtServices(ICourtRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<PagedResponse<CourtDTO>>> GetAllCourts(CourtRequestDTO req)
        {
            if (req.PageNumber <= 0) req.PageNumber = 1;
            if (req.PageSize <= 0 || req.PageSize > 10) req.PageSize = 10;

            var paginatedResult = await _repository.GetAllCourts(req);

            // Kiểm tra xem có bất kỳ sân nào thuộc facilityId không
            if (paginatedResult.TotalItems == 0)
            {
                // Kiểm tra xem có phải là tìm kiếm theo facilityId không
                if (!string.IsNullOrEmpty(req.Search) || req.Status.HasValue || req.CategoryId.HasValue)
                {
                    // Nếu có thêm điều kiện tìm kiếm (Search, Status, CategoryId) nhưng không có kết quả
                    return new ApiResponse<PagedResponse<CourtDTO>>
                    {
                        Success = false,
                        Message = "Không có kết quả tìm kiếm phù hợp.",
                        Status = 200,
                        Data = null
                    };
                }
                else
                {
                    // Nếu chỉ tìm theo facilityId mà không có sân nào
                    return new ApiResponse<PagedResponse<CourtDTO>>
                    {
                        Success = false,
                        Message = "Cơ sở này không tồn tại sân nào trong hệ thống.",
                        Status = 200,
                        Data = null
                    };
                }
            }

            var response = new PagedResponse<CourtDTO>
            {
                CurrentPage = paginatedResult.CurrentPage,
                ItemsPerPage = paginatedResult.ItemsPerPage,
                TotalItems = paginatedResult.TotalItems,
                TotalPages = paginatedResult.TotalPages,
                Items = paginatedResult.Items
            };

            return new ApiResponse<PagedResponse<CourtDTO>>
            {
                Success = true,
                Message = paginatedResult.Items.Any()
                    ? "Sân đã được lấy dữ liệu với phân trang thành công."
                    : "Không có kết quả tìm kiếm phù hợp.",
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
                Message = "Lấy thông tin chi tiết sân thành công",
                Status = 200,
                Data = court
            };
        }

        public async Task<ApiResponse<Court>> CreateCourtAsync(CreateCourt request)
        {
            if (request.FacilityId == null)
                return new ApiResponse<Court> { Success = false, Message = "FacilityId là bắt buộc.", Status = 400 };
            if (string.IsNullOrWhiteSpace(request.CourtName))
                return new ApiResponse<Court> { Success = false, Message = "CourtName là bắt buộc và không được phép là khoảng trắng.", Status = 400 };
            if (request.CategoryId == null)
                return new ApiResponse<Court> { Success = false, Message = "CategoryId là bắt buộc.", Status = 400 };
            if (request.PricePerHour == null || request.PricePerHour <= 0)
                return new ApiResponse<Court> { Success = false, Message = "PricePerHour phải lớn hơn 0.", Status = 400 };

            try
            {
                await _repository.CreateCourt(request);

                return new ApiResponse<Court>
                {
                    Success = true,
                    Message = "Sân đã được thêm vào thành công!",
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
                    Message = "Đã xảy ra lỗi: " + ex.Message,
                    Status = 500,
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<object>> UpdateCourt(UpdateCourtRequest request, int userId)
        {
            bool check = _repository.CheckCourtOwner(userId, request.CourtId);
            if(!check)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Tài khoản không thể cập nhật sân này vì không phải tài khoản chủ sân.",
                    Status = 500
                };
            }

            if (request.CourtName != null && string.IsNullOrWhiteSpace(request.CourtName))
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "CourtName không thể chỉ là khoảng trắng.",
                    Status = 400
                };
            }
            if (request.PricePerHour.HasValue)
            {
                if (request.PricePerHour <= 0)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "PricePerHour phải lớn hơn 0.",
                        Status = 400
                    };
                }
            }
            try
            {
                var court = await _repository.UpdateCourt(request);
                if (court == false)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không tìm thấy sân.",
                        Status = 404,
                        Data = null
                    };
                }

                if (!court)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Cập nhật sân thất bại.",
                        Status = 500,
                        Data = null
                    };
                }

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Cập nhật sân thành công.",
                    Status = 200,
                    Data = court
                };

            }
            catch(Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi trong quá trình cập nhật: " + ex.Message,
                    Status = 500,
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteCourt(int userId, int courtId)
        {
            bool check = _repository.CheckCourtOwner(userId, courtId);
            if (!check)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Tài khoản không thể xóa sân này vì không phải tài khoản chủ sân.",
                    Status = 500
                };
            }

            try
            {
                if (await _repository.DeleteCourt(courtId) == false)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy sân.",
                        Status = 404,
                        Data = false
                    };
                }

                await _repository.DeleteCourt(courtId);
                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Xóa sân thành công!",
                    Status = 201,
                    Data = true
                };
            }
            catch(Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi trong quá trình xóa: " + ex.Message,
                    Status = 500,
                    Data = false
                };
            }
        }

        public async Task<ApiResponse<object>> LockCourt(int userId, int courtId, int statusId)
        {
            bool check = _repository.CheckCourtOwner(userId, courtId);
            if (!check)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Tài khoản không thể khóa sân này vì không phải tài khoản chủ sân.",
                    Status = 500
                };
            }

            try
            {
                var court = await _repository.LockCourt(courtId, statusId);
                if(court == false)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không tìm thấy sân.",
                        Status = 404,
                        Data = null
                    };
                }

                if (!court)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Khóa sân thất bại.",
                        Status = 500,
                        Data = null
                    };
                }

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Khóa sân thành công",
                    Status = 200,
                    Data = court
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi trong quá trình khóa: " + ex.Message,
                    Status = 500,
                    Data = null
                };
            }

        }
    }
}
