using System;
using System.Collections.Generic;

namespace B2P_API.Models;

public partial class Comment
{
    public int CommentId { get; set; }

    public int? BlogId { get; set; }

    public int? UserId { get; set; }

    public string? Content { get; set; }

    public DateTime? PostAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? ParentCommentId { get; set; }

    public virtual Blog? Blog { get; set; }

    public virtual ICollection<Comment> InverseParentComment { get; set; } = new List<Comment>();

    public virtual Comment? ParentComment { get; set; }

    public virtual User? User { get; set; }
}
