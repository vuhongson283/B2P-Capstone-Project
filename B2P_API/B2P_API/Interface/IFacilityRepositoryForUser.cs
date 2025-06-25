using B2P_API.Models;

namespace B2P_API.Interface
{
    public interface IFacilityRepositoryForUser
    {
        Task<List<Facility>?> GetAllFacilitiesByPlayer();
    }
}
