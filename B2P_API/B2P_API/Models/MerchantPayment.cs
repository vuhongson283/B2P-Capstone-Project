using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class MerchantPayment
{
    public int MerchantPaymentId { get; set; }

    public int UserId { get; set; }

    public int PaymentMethodId { get; set; }

    public string PaymentKey { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int StatusId { get; set; }

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
