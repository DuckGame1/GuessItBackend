using System.Collections.Concurrent;
using static GuessItAPI.Rooms.RoomInfo;

namespace GuessItAPI.Rooms
{
    public class RoomInfo
    {
        public string RoomId { get; init; } = default!;
        public string RoomName { get; set; } = default!;
        public int HostId { get; set; }
        public int CategoryId { get; set; }
        public int MaxPlayers { get; init; }
        public string RoomPasswordHash { get; set; }
        public string JoinCode { get; set; }
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

        public ConcurrentDictionary<int, DateTime> CurrentPlayersConnection { get; } = new();
        public ConcurrentDictionary<int, Teams> TeamAssignment { get; } = new();

        public enum GameStatus { Open, InGame, Closed }
        public enum Teams { Team1, Team2 }

        public GameStatus Status { get; set; }

    }
}
