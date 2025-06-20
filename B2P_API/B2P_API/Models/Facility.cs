using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class Facility
{
    public int FacilityId { get; set; }

    public int? UserId { get; set; }

    public int StatusId { get; set; }

    public string? Location { get; set; }

    public string? Contact { get; set; }

    public virtual ICollection<Court> Courts { get; set; } = new List<Court>();

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual Status Status { get; set; } = null!;

    public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();

    public virtual User? User { get; set; }
}
