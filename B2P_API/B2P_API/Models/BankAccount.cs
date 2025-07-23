using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class BankAccount
{
    public int BankAccountId { get; set; }

    public int UserId { get; set; }

    public string AccountNumber { get; set; } = null!;

    public int BankTypeId { get; set; }

    public string? AccountHolder { get; set; }

    public virtual BankType BankType { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
