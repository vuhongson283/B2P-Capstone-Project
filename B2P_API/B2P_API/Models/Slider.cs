using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class Slider
{
    public int SlideId { get; set; }

    public string? SlideDescription { get; set; }

    public string? SlideUrl { get; set; }

    public int? StatusId { get; set; }

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual Status? Status { get; set; }
}
