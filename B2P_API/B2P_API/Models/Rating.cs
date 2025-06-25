using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class Rating
{
    public int RatingId { get; set; }

    public int? BookingId { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreateAt { get; set; }

    public int? Stars { get; set; }

    public virtual Booking? Booking { get; set; }
}
