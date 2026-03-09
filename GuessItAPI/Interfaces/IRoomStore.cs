using GuessItAPI.Rooms;

namespace GuessItAPI.Interfaces
{
    public interface IRoomStore
    {
        RoomInfo Create(int hostId, string roomName, int maxPlayers);
        bool TryGet(string roomId, out RoomInfo room);
        IReadOnlyCollection<RoomInfo> GetRooms();
        void HeartBeat(string roomId, int hostId);
        int CleanupExpired(TimeSpan ttl);
        bool DeleteRoom(string roomId, out string result);
        bool DeleteRoom(int hostId, out string result);
    }
}
