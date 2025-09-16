using System;
using System.Collections.Generic;

namespace GuessItAPI.Models;

public partial class UserDatum
{
    public int AvatarId { get; set; }

    public int UserId { get; set; }

    public byte[] Avatar { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
