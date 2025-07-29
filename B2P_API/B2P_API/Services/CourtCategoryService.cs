using Azure;
using B2P_API.DTOs.CourtCategoryDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Utils;
using B2P_API.DTOs.FacilityDTO;

namespace B2P_API.Services
{
    public class CourtCategoryService
    {
        private readonly ICourtCategoryRepository _categoryRepo;

        public CourtCategoryService(ICourtCategoryRepository categoryRepo)
        {
            _categoryRepo = categoryRepo;
        }

        public async Task<ApiResponse<PagedResponse<CourtCategoryResponse>?>> GetAllCourtCategoriesAsync(string search, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var categories = await _categoryRepo.GetAllCourtCategoriesAsync();
                if (categories == null || categories.Count == 0)
                {
                    return new ApiResponse<PagedResponse<CourtCategoryResponse>?>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_71,
                        Success = false,
                        Status = 404
                    };
                }

                // Apply search filter
                var filteredCategories = categories.AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filteredCategories = filteredCategories.Where(c =>
                        !string.IsNullOrEmpty(c.CategoryName) && c.CategoryName.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                var filteredList = filteredCategories.ToList();

                // Check if no results after filtering
                if (filteredList.Count == 0)
                {
                    return new ApiResponse<PagedResponse<CourtCategoryResponse>?>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_77,
                        Success = false,
                        Status = 404
                    };
                }

                // Map to response
                var response = filteredList.Select(c => new CourtCategoryResponse
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                }).ToList();

                // Apply pagination
                var totalItems = response.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                if (pageNumber < 1 || pageNumber > totalPages)
                {
                    return new ApiResponse<PagedResponse<CourtCategoryResponse>?>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_78,
                        Success = false,
                        Status = 400
                    };
                }

                var pagedItems = response
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResponse = new PagedResponse<CourtCategoryResponse>
                {
                    CurrentPage = pageNumber,
                    ItemsPerPage = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = pagedItems
                };

                return new ApiResponse<PagedResponse<CourtCategoryResponse>?>
                {
                    Data = pagedResponse,
                    Message = MessagesCodes.MSG_79 + $"Tìm thấy {totalItems} kết quả",
                    Success = true,
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResponse<CourtCategoryResponse>?>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }

        public async Task<ApiResponse<object>> AddCourtCategoryAsync(string cateName)
        {
            try
            {
                if (string.IsNullOrEmpty(cateName?.Trim()))
                {
                    return new ApiResponse<object>
                    {
                        Data = null!,
                        Message = MessagesCodes.MSG_74,
                        Success = false,
                        Status = 400
                    };
                }

                // NEW: Length validation
                if (cateName.Trim().Length > 100)
                {
                    return new ApiResponse<object>
                    {
                        Data = null!,
                        Message = MessagesCodes.MSG_76, // Assuming new message for length
                        Success = false,
                        Status = 400
                    };
                }

                var newCategory = new CourtCategory
                {
                    CategoryName = cateName.Trim(), // CHANGED: Added Trim()
                };
                var result = await _categoryRepo.AddCourtCategoryAsync(newCategory);

                return new ApiResponse<object>
                {
                    Data = null!,
                    Message = MessagesCodes.MSG_75,
                    Success = true,
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Data = null!,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }
        public async Task<ApiResponse<object>> UpdateCourtCategoryAsync(CourtCategoryUpdateRequest request)
        {
            try
            {
                if (!request.CategoryId.HasValue || request.CategoryId <= 0)
                {
                    return new ApiResponse<object>
                    {
                        Data = false,
                        Message = MessagesCodes.MSG_81,
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(request.CategoryName?.Trim()))
                {
                    return new ApiResponse<object>
                    {
                        Data = null!,
                        Message = MessagesCodes.MSG_82,
                        Success = false,
                        Status = 400
                    };
                }

                var existingCategory = await _categoryRepo.GetCourtCategoryByIdAsync(request.CategoryId);
                if (existingCategory == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null!,
                        Message = MessagesCodes.MSG_83,
                        Success = false,
                        Status = 404
                    };
                }
                existingCategory.CategoryName = request.CategoryName.Trim();
                await _categoryRepo.UpdateCourtCategoryAsync(existingCategory);
                    return new ApiResponse<object>
                    {
                        Data = null!,
                        Message = MessagesCodes.MSG_84,
                        Success = true,
                        Status = 200
                    };
               
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Data = null!,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }

        public async Task<ApiResponse<object>> DeleteCourtCategoryAsync(int? id)
        {
            try
            {
                if (id == null || id <= 0)
                {
                    return new ApiResponse<object>
                    {
                        Data = null!,
                        Message = MessagesCodes.MSG_81,
                        Success = false,
                        Status = 400
                    };
                }

                 await _categoryRepo.DeleteCourtCategoryAsync(id);

                    return new ApiResponse<object>
                    {
                        Data = null!,
                        Message = MessagesCodes.MSG_85,
                        Success = true,
                        Status = 200
                    };

            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Data = null!,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }
        public async Task<ApiResponse<CourtCategoryResponse?>> GetCourtCategoryByIdAsync(int id)
        {
            try
            {
                var category = await _categoryRepo.GetCourtCategoryByIdAsync(id);
                if (category == null)
                {
                    return new ApiResponse<CourtCategoryResponse?>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_73,
                        Success = false,
                        Status = 404
                    };
                }
                var response = new CourtCategoryResponse
                {
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName,
                };
                return new ApiResponse<CourtCategoryResponse?>
                {
                    Data = response,
                    Message = MessagesCodes.MSG_86,
                    Success = true,
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<CourtCategoryResponse?>
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

