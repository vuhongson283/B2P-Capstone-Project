using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class CourtCategory
{
    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public virtual ICollection<Court> Courts { get; set; } = new List<Court>();
}
