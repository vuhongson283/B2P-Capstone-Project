using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class Status
{
    public int StatusId { get; set; }

    public string? StatusName { get; set; }

    public string? StatusDescription { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Court> Courts { get; set; } = new List<Court>();

    public virtual ICollection<Facility> Facilities { get; set; } = new List<Facility>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
