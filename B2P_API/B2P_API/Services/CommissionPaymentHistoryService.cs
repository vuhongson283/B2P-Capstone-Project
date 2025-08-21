using B2P_API.DTOs;
using B2P_API.DTOs.CommissionPaymentHistoryDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repositories;
using B2P_API.Response;

namespace B2P_API.Services
{
    public class CommissionPaymentHistoryService
    {
        private readonly ICommissionPaymentHistoryRepository _repo;
        private readonly IAccountManagementRepository _accRepo;

        public CommissionPaymentHistoryService(
            ICommissionPaymentHistoryRepository repo,
            IAccountManagementRepository accRepo)
        {
            _repo = repo;
            _accRepo = accRepo;
          
        }

        public async Task<ApiResponse<IEnumerable<CommissionPaymentHistoryDto>>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            var result = data.Select(x => new CommissionPaymentHistoryDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Month = x.Month,
                Year = x.Year,
                Amount = x.Amount,
                PaidAt = x.PaidAt,
                StatusId = x.StatusId,
                Note = x.Note
            });

            return new ApiResponse<IEnumerable<CommissionPaymentHistoryDto>>
            {
                Success = true,
                Status = 200,
                Message = "Lấy danh sách thành công",
                Data = result
            };
        }

        public async Task<ApiResponse<IEnumerable<object>>> GetByUserIdAsync(int userId)
        {
            var user = await _accRepo.GetByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<IEnumerable<object>>
                {
                    Success = false,
                    Status = 404,
                    Message = "Người dùng không tồn tại"
                };
            }

            var data = await _repo.GetByUserIdAsync(userId);
            var result = data.Select(x => new CommissionPaymentHistoryDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Month = x.Month,
                Year = x.Year,
                Amount = x.Amount,
                PaidAt = x.PaidAt,
                StatusId = x.StatusId,
                Note = x.Note
            });

            return new ApiResponse<IEnumerable<object>>
            {
                Success = true,
                Status = 200,
                Message = "Lấy dữ liệu theo UserId thành công",
                Data = result
            };
        }

        public async Task<ApiResponse<CommissionPaymentHistoryDto>> CreateAsync(CommissionPaymentHistoryCreateDto dto)
        {
            // ✅ Validate
            var user = await _accRepo.GetByIdAsync(dto.UserId);
            if (user == null)
            {
                return new ApiResponse<CommissionPaymentHistoryDto>
                {
                    Success = false,
                    Status = 400,
                    Message = "UserId không hợp lệ"
                };
            }

            if (dto.Month < 1 || dto.Month > 12)
            {
                return new ApiResponse<CommissionPaymentHistoryDto>
                {
                    Success = false,
                    Status = 400,
                    Message = "Tháng phải từ 1 đến 12"
                };
            }

            if (dto.Year < 2020)
            {
                return new ApiResponse<CommissionPaymentHistoryDto>
                {
                    Success = false,
                    Status = 400,
                    Message = "Năm không hợp lệ"
                };
            }

            if (dto.Amount <= 0)
            {
                return new ApiResponse<CommissionPaymentHistoryDto>
                {
                    Success = false,
                    Status = 400,
                    Message = "Số tiền phải lớn hơn 0"
                };
            }

            

            // ✅ Lưu
            var entity = new CommissionPaymentHistory
            {
                UserId = dto.UserId,
                Month = dto.Month,
                Year = dto.Year,
                Amount = dto.Amount,
                StatusId = dto.StatusId,
                Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
                PaidAt = null
            };

            await _repo.AddAsync(entity);
            await _repo.SaveChangesAsync();

            var result = new CommissionPaymentHistoryDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Month = entity.Month,
                Year = entity.Year,
                Amount = entity.Amount,
                PaidAt = entity.PaidAt,
                StatusId = entity.StatusId,
                Note = entity.Note
            };

            return new ApiResponse<CommissionPaymentHistoryDto>
            {
                Success = true,
                Status = 201,
                Message = "Tạo mới thành công",
                Data = result
            };
        }

        public virtual async Task<ApiResponse<object>> UpdateAsync(int id, CommissionPaymentHistoryUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Status = 404,
                    Message = "Không tìm thấy bản ghi"
                };
            }

           

            // ✅ Update
            entity.StatusId = dto.StatusId;
            entity.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return new ApiResponse<object>
            {
                Success = true,
                Status = 200,
                Message = "Cập nhật thành công",
                Data = entity
            };
        }
    }
}
