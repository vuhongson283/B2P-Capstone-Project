using B2P_API.Models;

namespace B2P_API.Interface
{
    public interface ICourtCategoryRepository
    {
        Task<List<CourtCategory>?> GetAllCourtCategoriesAsync();
        Task<CourtCategory?> GetCourtCategoryByIdAsync(int? id);
        Task<bool> AddCourtCategoryAsync(CourtCategory courtCategory);
        Task<bool> UpdateCourtCategoryAsync(CourtCategory courtCategory);
        Task<bool> DeleteCourtCategoryAsync(int? id);
    }
}
