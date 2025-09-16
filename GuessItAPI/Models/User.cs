using System;
using System.Collections.Generic;

namespace GuessItAPI.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public virtual ICollection<CardsCategory> CardsCategories { get; set; } = new List<CardsCategory>();

    public virtual ICollection<UserDatum> UserData { get; set; } = new List<UserDatum>();

    public virtual ICollection<UserSubscribe> UserSubscribes { get; set; } = new List<UserSubscribe>();
}
