using B2P_API.Models;

namespace B2P_API.Interface
{
    public interface IFacilityRepository
    {
        Task<List<Facility>?> GetAllFacilitiesByPlayer();
    }
}
