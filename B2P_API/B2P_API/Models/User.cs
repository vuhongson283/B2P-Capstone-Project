using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class User
{
    public int UserId { get; set; }

    public int StatusId { get; set; }

    public string? Password { get; set; }

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public bool? IsMale { get; set; }

    public int RoleId { get; set; }

    public DateTime? CreateAt { get; set; }

    public string? Address { get; set; }

    public DateOnly? Dob { get; set; }

    public string FullName { get; set; } = null!;

    public virtual BankAccount? BankAccount { get; set; }

    public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<CommissionPaymentHistory> CommissionPaymentHistories { get; set; } = new List<CommissionPaymentHistory>();

    public virtual ICollection<Facility> Facilities { get; set; } = new List<Facility>();

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual ICollection<MerchantPayment> MerchantPayments { get; set; } = new List<MerchantPayment>();

    public virtual Role Role { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;

    public virtual ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();
}
