using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class Image
{
    public int ImageId { get; set; }

    public int? FacilityId { get; set; }

    public int? BlogId { get; set; }

    public int? UserId { get; set; }

    public int? SlideId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int? Order { get; set; }

    public string? Caption { get; set; }

    public virtual Blog? Blog { get; set; }

    public virtual Facility? Facility { get; set; }

    public virtual Slider? Slide { get; set; }

    public virtual User? User { get; set; }
}
