using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class BookingDetail
{
    public int BookingDetailId { get; set; }

    public int BookingId { get; set; }

    public int CourtId { get; set; }

    public int TimeSlotId { get; set; }

    public int StatusId { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public DateTime CheckInDate { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Court Court { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;

    public virtual TimeSlot TimeSlot { get; set; } = null!;
}
