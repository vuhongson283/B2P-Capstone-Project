using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class BankType
{
    public int BankTypeId { get; set; }

    public string BankName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
}
