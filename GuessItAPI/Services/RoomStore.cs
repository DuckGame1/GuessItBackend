
using GuessItAPI.Interfaces;
using GuessItAPI.Jwt;
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
                MaxPlayers = maxPlayers,
                RoomPasswordHash = PasswordHasher.Generate("")
            };

            room.CurrentPlayersConnection[hostId] = DateTime.UtcNow;
            foreach (var roomInfo in _rooms.Values)
                if(roomInfo.HostId == hostId)
                    throw new Exception($"Room with this host ({hostId}) already extists");
            if(!_rooms.TryAdd(roomId, room))
                throw new Exception("Failed to create room");
            
            return room;
        }
        public bool DeleteRoom(string roomId, out string result)
        {
            if(!_rooms.TryRemove(roomId, out var _))
            {
                result = "Room not found";
                return false;
            }
            result = "Room successfully deleted";
            return true;
        }
        public bool DeleteRoom(int hostId, out string result)
        {
            string roomId = string.Empty;
            foreach (var room in _rooms)
            {
                if(room.Value.HostId == hostId)
                {
                    roomId = room.Key;
                    break;
                }
            }
            if(roomId == string.Empty)
            {
                result = "Room with that host not found";
                return false;
            }
            return DeleteRoom(roomId, out result);
        }
        public IReadOnlyCollection<RoomInfo> GetRooms() => _rooms.Values.ToArray();
        public bool TryGet(string roomId, out RoomInfo room) => _rooms.TryGetValue(roomId, out room!);
        public void HeartBeat(string roomId, int hostId)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;
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
