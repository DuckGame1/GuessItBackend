using System;
using System.Collections.Generic;

namespace GuessItAPI.Models;

public partial class UserSubscribe
{
    public int SubscribeId { get; set; }

    public int UserId { get; set; }

    public int CategoryId { get; set; }

    public virtual CardsCategory Category { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
