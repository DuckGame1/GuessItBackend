using GuessItAPI.Interfaces;
using GuessItAPI.Jwt;
using GuessItAPI.Rooms;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.IdentityModel.Tokens;
using static GuessItAPI.Rooms.RoomInfo;

namespace GuessItAPI.Services
{
    public class RoomService
    {
        public readonly IRoomStore _roomstore;

        public RoomService(IRoomStore roomstore)
        {
            _roomstore = roomstore;
        }
        public RoomInfo CreateRoom(int hostId, string roomName, int maxPlayers) => _roomstore.Create(hostId, roomName, maxPlayers);
        public void SendHeartbeat(string roomId, int userId) => _roomstore.HeartBeat(roomId, userId);
        public List<RoomInfo> GetRooms(GameStatus? gameStatus = null, bool isFull = false, string? name = null, string? roomId = null)
        {
            var rooms = _roomstore.GetRooms().ToList();
            if (gameStatus != null)
                rooms = rooms.Where(r => r.Status == gameStatus).ToList();
            if(!isFull)
                rooms = rooms.Where(r => r.CurrentPlayersConnection.Count < r.MaxPlayers).ToList();
            if (!string.IsNullOrWhiteSpace(name))
                rooms = rooms.Where(r => r.RoomName == name).ToList();
            if(!string.IsNullOrWhiteSpace(roomId))
                rooms = rooms.Where(r => r.RoomId == roomId).ToList();
            return rooms;
        }
        private bool TryGetHostCheckedRoom(string roomId, int userId, out RoomInfo room)
        {
            if (_roomstore.TryGet(roomId, out room)) return false;
            if (room.HostId != userId) return false;
            return true;
        }
        public bool SetPassword(string roomId, int userId, string password)
        {
            if (!TryGetHostCheckedRoom(roomId, userId, out RoomInfo room))
                return false;
            room.RoomPasswordHash = PasswordHasher.Generate(password);
            return true;
        }
        public bool SetName(string roomId, int userId, string name)
        {
            if (!TryGetHostCheckedRoom(roomId, userId, out RoomInfo room))
                return false;
            room.RoomName = name;
            return true;
        }
        public bool ChangeHost(string roomId, int userId, int newHostId)
        {
            if (!TryGetHostCheckedRoom(roomId, userId, out RoomInfo room))
                return false;
            room.HostId = newHostId;
            return true;
        }
        public bool SetCategory(string roomId, int userId, int categoryId)
        {
            if (!TryGetHostCheckedRoom(roomId, userId, out RoomInfo room))
                return false;
            room.CategoryId = categoryId;
            return true;
        }
        public bool SetJoinCode(string roomId, int userId, string joinCode)
        {
            if (!TryGetHostCheckedRoom(roomId, userId, out RoomInfo room))
                return false;
            room.JoinCode = joinCode;
            return true;
        }
        public (string, string) JoinRoom(string roomId, int userId, string password)
        {
            if (TryGetHostCheckedRoom(roomId, userId, out RoomInfo room))
                return default!;
            if (room.CurrentPlayersConnection.Count < room.MaxPlayers ||
                room.CurrentPlayersConnection.TryGetValue(userId, out _))
            {
                room.CurrentPlayersConnection[userId] = DateTime.UtcNow;
                return (room.RoomId, room.JoinCode);
            }
            return default!;
        }
        public bool LeaveRoom(string roomId, int userId)
        {
            if (TryGetHostCheckedRoom(roomId, userId, out RoomInfo room))
                if(room.CurrentPlayersConnection.Count != 1)
                    return false;
            room.CurrentPlayersConnection.TryRemove(userId, out _);
            room.TeamAssignment.TryRemove(userId, out _);
            return true;
        }
        public bool ChangeTeam(string roomId, int userId, Teams team)
        {
            _roomstore.TryGet(roomId, out RoomInfo room);
            if (!room.CurrentPlayersConnection.TryGetValue(userId, out _))
                return false;
            room.TeamAssignment[userId] = team;
            return true;
        }
        public bool SetGameStatus(string roomId, int userId, GameStatus gameStatus)
        {
            if (TryGetHostCheckedRoom(roomId, userId, out RoomInfo room))
                return false;
            room.Status = gameStatus;
            return true;
        }
    }
}
