using B2P_API.DTOs.FacilityDTOs;
using B2P_API.Models;

namespace B2P_API.Interface
{
	public interface IFacilityRepository
	{
		
		Task<List<Facility>> GetAllAsync();

		Task<Facility?> GetByIdAsync(int id);

	
		Task<Facility> CreateFacilityAsync(Facility facility);

	
		Task<bool> DeleteAsync(int id);

		Task<List<Facility>> GetByUserIdAsync(int userId);

		Task<bool> UpdateAsync(Facility facility);

		Task<List<Facility>?> GetAllFacilitiesByPlayer();
	}
}
