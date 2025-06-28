using System.ComponentModel.DataAnnotations;

namespace B2P_API.DTOs.FacilityDTOs
{
    public class CreateFacilityRequest
    {
        [Required]
        public string FacilityName { get; set; } = null!;

        public string? Location { get; set; }

        public string? Contact { get; set; }

        public int? UserId { get; set; }

        [Required]
        public int StatusId { get; set; }

        [Range(0, 23)]
        public int OpenHour { get; set; }

        [Range(1, 24)]
        public int CloseHour { get; set; }

        [Range(1, 180)]
        public int SlotDuration { get; set; } // phút
    }
}
