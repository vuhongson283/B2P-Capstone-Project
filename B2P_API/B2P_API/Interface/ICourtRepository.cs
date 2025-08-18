using B2P_API.DTOs.CourtManagementDTO;
using B2P_API.Models;
using B2P_API.Response;

namespace B2P_API.Interface
{
    public interface ICourtRepository
    {
        Task<PagedResponse<Court>> GetAllCourts(CourtRequestDTO req);
        Task<Court> GetCourtDetail(int courtId);
        Task<Court> CreateCourt(CreateCourt court);
        Task<bool> UpdateCourt(UpdateCourtRequest court);
        Task<bool> DeleteCourt(int courtId);
        Task<bool> LockCourt(int courtId, int statusId);
        bool CheckCourtOwner(int userId, int courtId);
    }
}
