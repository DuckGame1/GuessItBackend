using System;
using System.Collections.Generic;

namespace GuessItAPI.Models;

public partial class Card
{
    public int CardId { get; set; }

    public int CardCategoryId { get; set; }

    public string? CardName { get; set; }

    public byte[] CardImage { get; set; } = null!;

    public byte[]? CardImagePreview { get; set; }

    public virtual CardsCategory CardCategory { get; set; } = null!;
}
