using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class Court
{
    public int CourtId { get; set; }

    public int? FacilityId { get; set; }

    public int? StatusId { get; set; }

    public string? CourtName { get; set; }

    public int? CategoryId { get; set; }

    public decimal? PricePerHour { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual CourtCategory? Category { get; set; }

    public virtual Facility? Facility { get; set; }

    public virtual Status? Status { get; set; }
}
