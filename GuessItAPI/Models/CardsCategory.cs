using System;
using System.Collections.Generic;

namespace GuessItAPI.Models;

public partial class CardsCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? CategoryDescription { get; set; }

    public byte[]? CategoryImage { get; set; }

    public int CategoryOwnerId { get; set; }

    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();

    public virtual User CategoryOwner { get; set; } = null!;

    public virtual ICollection<UserSubscribe> UserSubscribes { get; set; } = new List<UserSubscribe>();
}
