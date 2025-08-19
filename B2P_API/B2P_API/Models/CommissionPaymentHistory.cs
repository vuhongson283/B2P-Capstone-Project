using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class CommissionPaymentHistory
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaidAt { get; set; }

    public int StatusId { get; set; }

    public string? Note { get; set; }

    public virtual Status Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
