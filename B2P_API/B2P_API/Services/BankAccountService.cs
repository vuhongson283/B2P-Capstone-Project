using B2P_API.DTOs.BankTypeDTOs;
using B2P_API.DTOs.FacilityDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Utils;

namespace B2P_API.Services
{
    public class BankAccountService
    {
        private readonly IBankAccountRepository _bankAccountRepository;
        public BankAccountService(IBankAccountRepository bankAccountRepository)
        {
            _bankAccountRepository = bankAccountRepository;
        }

        public async Task<ApiResponse<PagedResponse<BankTypeResponse>>> GetAllBankTypeAsync(string name, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var bankTypes = await _bankAccountRepository.GetAllBankTypeAysnc();

                if (bankTypes == null || bankTypes.Count == 0)
                {
                    return new ApiResponse<PagedResponse<BankTypeResponse>>
                    {
                        Success = false,
                        Status = 404,
                        Message = "Không có danh sách ngân hàng",
                        Data = null
                    };
                }

                var banks = bankTypes.AsQueryable();
                if (!string.IsNullOrEmpty(name))
                {
                    banks = banks.Where(b =>
                        b.BankName.Contains(name, StringComparison.OrdinalIgnoreCase));
                }
                var result = banks.ToList();
                if (result.Count == 0)
                {
                    return new ApiResponse<PagedResponse<BankTypeResponse>>
                    {
                        Success = false,
                        Status = 404,
                        Message = "Không tìm thấy ngân hàng nào khớp với tên đã tìm kiếm",
                        Data = null
                    };
                }

                // Map the result to BankTypeResponse
                var results = result.Select(b => new BankTypeResponse
                {
                    BankTypeId = b.BankTypeId,
                    BankName = b.BankName,
                    Description = b.Description
                }).ToList();

                var totalItems = results.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                if (pageNumber < 1 || pageNumber > totalPages)
                {
                    return new ApiResponse<PagedResponse<BankTypeResponse>>
                    {
                        Data = null,
                        Message = $"Số trang không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                var pagedItems = results
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResponse = new PagedResponse<BankTypeResponse>
                {
                    CurrentPage = pageNumber,
                    ItemsPerPage = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = pagedItems
                };

                return new ApiResponse<PagedResponse<BankTypeResponse>>
                {
                    Success = true,
                    Message = $"Found {totalItems} facilities matching search criteria.",
                    Status = 200,
                    Data = pagedResponse
                };
            }
            catch (Exception ex)
            {

                return new ApiResponse<PagedResponse<BankTypeResponse>>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }
    }
}

