using Azure;
using B2P_API.DTOs.CourtCategoryDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Utils;

namespace B2P_API.Services
{
    public class CourtCategoryService
    {
        private readonly ICourtCategoryRepository _categoryRepo;

        public CourtCategoryService(ICourtCategoryRepository categoryRepo)
        {
            _categoryRepo = categoryRepo;
        }

        public async Task<ApiResponse<List<CourtCategoryResponse>?>> GetAllCourtCategoriesAsync()
        {
            try
            {
                var categories = await _categoryRepo.GetAllCourtCategoriesAsync();
                if (categories == null || categories.Count == 0)
                {
                    return new ApiResponse<List<CourtCategoryResponse>?>
                    {
                        Data = null,
                        Message = "Không tìm thấy danh sách kiểu sân",
                        Success = false,
                        Status = 404
                    };
                }
                var response = categories.Select(c => new CourtCategoryResponse
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                }).ToList();

                return new ApiResponse<List<CourtCategoryResponse>?>
                {
                    Data = response,
                    Message = "Tải danh sách kiểu sân thành công",
                    Success = true,
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<CourtCategoryResponse>?>
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
