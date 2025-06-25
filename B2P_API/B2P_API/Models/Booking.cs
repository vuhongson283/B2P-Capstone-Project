using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int? UserId { get; set; }

    public int? CourtId { get; set; }

    public int? TimeSlotId { get; set; }

    public int StatusId { get; set; }

    public decimal? TotalPrice { get; set; }

    public bool? IsDayOff { get; set; }

    public DateTime OrderDate { get; set; }

    public virtual Court? Court { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual Status Status { get; set; } = null!;

    public virtual TimeSlot? TimeSlot { get; set; }

    public virtual User? User { get; set; }
}
