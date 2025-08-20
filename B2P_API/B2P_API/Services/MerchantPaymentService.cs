using B2P_API.DTOs.MerchantPaymentDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;

namespace B2P_API.Services
{
    public class MerchantPaymentService
    {
        private readonly IMerchantPaymentRepository _repository;
        private readonly IAccountManagementRepository _accRepo;

        public MerchantPaymentService(IMerchantPaymentRepository repository, IAccountManagementRepository accRepo)
        {
            _repository = repository;
            _accRepo = accRepo;
        }

        public async Task<ApiResponse<IEnumerable<MerchantPaymentResponseDto>>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            var result = list.Select(mp => new MerchantPaymentResponseDto
            {
                MerchantPaymentId = mp.MerchantPaymentId,
                UserId = mp.UserId,
                PaymentMethodId = mp.PaymentMethodId,
                PaymentKey = mp.PaymentKey,
                StatusId = mp.StatusId,
                CreatedAt = mp.CreatedAt
            });

            return new ApiResponse<IEnumerable<MerchantPaymentResponseDto>>
            {
                Success = true,
                Message = "Get all successfully",
                Status = 200,
                Data = result
            };
        }

        public async Task<ApiResponse<MerchantPaymentResponseDto?>> GetByIdAsync(int id)
        {
            var mp = await _repository.GetByIdAsync(id);
            if (mp == null)
            {
                return new ApiResponse<MerchantPaymentResponseDto?>
                {
                    Success = false,
                    Message = "Not found",
                    Status = 404,
                    Data = null
                };
            }

            return new ApiResponse<MerchantPaymentResponseDto?>
            {
                Success = true,
                Message = "Get by id successfully",
                Status = 200,
                Data = new MerchantPaymentResponseDto
                {
                    MerchantPaymentId = mp.MerchantPaymentId,
                    UserId = mp.UserId,
                    PaymentMethodId = mp.PaymentMethodId,
                    PaymentKey = mp.PaymentKey,
                    StatusId = mp.StatusId,
                    CreatedAt = mp.CreatedAt
                }
            };
        }

        public async Task<ApiResponse<object>> CreateAsync(MerchantPaymentCreateDto dto)
        {
            // Validate UserId
            if (dto.UserId <= 0)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Status = 400,
                    Message = "UserId không tồn tại"
                };
            }

            // Validate PaymentMethodId
            if (dto.PaymentMethodId <= 0 || dto.PaymentMethodId > 4)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Status = 400,
                    Message = "PaymentMethod không tồn tại"
                };
            }

            // Validate PaymentKey
            if (string.IsNullOrWhiteSpace(dto.PaymentKey))
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Status = 400,
                    Message = "PaymentKey không được để trống"
                };
            }

            // Validate StatusId
            if (dto.StatusId <= 0)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Status = 400,
                    Message = "Status không tồn tại"
                };
            }

            // Kiểm tra user có tồn tại
            var user = await _accRepo.GetByIdAsync(dto.UserId);
            if (user == null)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Status = 404,
                    Message = "Người dùng không tồn tại"
                };
            }

            // Tạo entity
            var entity = new MerchantPayment
            {
                UserId = dto.UserId,
                PaymentMethodId = dto.PaymentMethodId,
                PaymentKey = dto.PaymentKey,
                StatusId = dto.StatusId,
                CreatedAt = DateTime.Now
            };

            var created = await _repository.AddAsync(entity);

            return new ApiResponse<object>
            {
                Success = true,
                Message = "Created successfully",
                Status = 201,
                Data = new MerchantPaymentResponseDto
                {
                    MerchantPaymentId = created.MerchantPaymentId,
                    UserId = created.UserId,
                    PaymentMethodId = created.PaymentMethodId,
                    PaymentKey = created.PaymentKey,
                    StatusId = created.StatusId,
                    CreatedAt = created.CreatedAt
                }
            };
        }


        public async Task<ApiResponse<bool>> UpdateAsync(int id, MerchantPaymentUpdateDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "MerchantPayment không tồn tại",
                    Status = 404,
                    Data = false
                };
            }

            // Update PaymentKey nếu có
            if (dto.PaymentKey != null)
            {
                if (string.IsNullOrWhiteSpace(dto.PaymentKey))
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Status = 400,
                        Message = "PaymentKey không họưp lệ",
                        Data = false
                    };
                }
                entity.PaymentKey = dto.PaymentKey;
            }

            // Update StatusId nếu có
            if (dto.StatusId != null)
            {
                if (dto.StatusId <= 0 || dto.StatusId > 10)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Status = 400,
                        Message = "StatusId phải từ 1 đến 10",
                        Data = false
                    };
                }
                entity.StatusId = dto.StatusId;
            }

            await _repository.UpdateAsync(entity);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Updated successfully",
                Status = 200,
                Data = true
            };
        }


        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Not found",
                    Status = 404,
                    Data = false
                };
            }

            await _repository.DeleteAsync(entity);
            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Deleted successfully",
                Status = 200,
                Data = true
            };
        }

        public async Task<ApiResponse<IEnumerable<MerchantPaymentResponseDto>>> GetByUserIdAsync(int userId)
        {
            var list = await _repository.GetByUserIdAsync(userId);
            var result = list.Select(mp => new MerchantPaymentResponseDto
            {
                MerchantPaymentId = mp.MerchantPaymentId,
                UserId = mp.UserId,
                PaymentMethodId = mp.PaymentMethodId,
                PaymentMethodName = mp.PaymentMethod.Description,
                PaymentKey = mp.PaymentKey,
                StatusId = mp.StatusId,
                StatusName = mp.Status.StatusName,
                CreatedAt = mp.CreatedAt
            });

            return new ApiResponse<IEnumerable<MerchantPaymentResponseDto>>
            {
                Success = true,
                Message = "Get by userId successfully",
                Status = 200,
                Data = result
            };
        }
    }
}
