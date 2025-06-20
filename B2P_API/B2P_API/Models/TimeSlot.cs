using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class TimeSlot
{
    public int TimeSlotId { get; set; }

    public int? FacilityId { get; set; }

    public int StatusId { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Facility? Facility { get; set; }

    public virtual Status Status { get; set; } = null!;
}
