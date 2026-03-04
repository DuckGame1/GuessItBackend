
using GuessItAPI.Interfaces;
using GuessItAPI.Rooms;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GuessItAPI.Services
{
    public sealed class RoomStore : IRoomStore
    {
        private readonly ConcurrentDictionary<string, RoomInfo> _rooms = new();

        public RoomInfo Create(int hostId, string roomName, int maxPlayers)
        {
            string roomId = Guid.NewGuid().ToString("N");
            RoomInfo room = new RoomInfo
            {
                RoomId = roomId,
                HostId = hostId,
                RoomName = roomName,
                MaxPlayers = maxPlayers
            };

            room.CurrentPlayersConnection[hostId] = DateTime.UtcNow;
            foreach (var roomInfo in _rooms.Values)
                if(roomInfo.HostId == hostId)
                    throw new Exception($"Room with this host ({hostId}) already extists");
            if(!_rooms.TryAdd(roomId, room))
                throw new Exception("Failed to create room");
            
            return room;
        }
        public IReadOnlyCollection<RoomInfo> GetRooms() => _rooms.Values.ToArray();
        public bool TryGet(string roomId, out RoomInfo room) => _rooms.TryGetValue(roomId, out room!);
        public void HeartBeat(string roomId, int hostId)
        {
            if (_rooms.TryGetValue(roomId, out var room)) return;
            if (room!.HostId != hostId) return;

            room.LastHeartbeat = DateTime.UtcNow;
            room.CurrentPlayersConnection[hostId] = DateTime.UtcNow;
        }
        public int CleanupExpired(TimeSpan ttl)
        {
            DateTime targetTime = DateTime.UtcNow - ttl;
            int removed = 0;

            foreach(var room in _rooms)
            {
                if(room.Value.LastHeartbeat < targetTime)
                {
                    if(_rooms.TryRemove(room.Key, out _))
                        removed++;
                }
            }
            return removed;
        }
    }
}
