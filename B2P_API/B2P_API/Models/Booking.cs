using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int? UserId { get; set; }

    public int StatusId { get; set; }

    public decimal? TotalPrice { get; set; }

    public bool? IsDayOff { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual Status Status { get; set; } = null!;

    public virtual User? User { get; set; }
}
