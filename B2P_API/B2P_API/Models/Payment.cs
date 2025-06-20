using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? BookingId { get; set; }

    public int StatusId { get; set; }

    public decimal? Amount { get; set; }

    public DateTime? TimeStamp { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual Status Status { get; set; } = null!;
}
