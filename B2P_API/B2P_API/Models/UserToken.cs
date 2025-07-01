using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class UserToken
{
    public int UserTokenId { get; set; }

    public int? UserId { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public virtual User? User { get; set; }
}
