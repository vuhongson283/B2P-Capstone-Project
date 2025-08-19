using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class PaymentMethod
{
    public int PaymentMethodId { get; set; }

    public string Description { get; set; } = null!;

    public virtual ICollection<MerchantPayment> MerchantPayments { get; set; } = new List<MerchantPayment>();
}
