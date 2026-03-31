using GuessItAPI.Interfaces;
using GuessItAPI.Jwt;
using GuessItAPI.Models;
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
        public List<RoomResponseModel> GetRooms(GameStatus? gameStatus = null, bool? isFull = false, string? name = null, string? roomId = null, int? hostId = 0)
        {
            var rooms = _roomstore.GetRooms().ToList();
            var roomReturns = new List<RoomResponseModel>();

            if (gameStatus != null)
                rooms = rooms.Where(r => r.Status == gameStatus).ToList();
            if(isFull == false)
                rooms = rooms.Where(r => r.CurrentPlayersConnection.Count < r.MaxPlayers).ToList();
            else if (isFull == true)
                rooms = rooms.Where(r => r.CurrentPlayersConnection.Count >= r.MaxPlayers).ToList();
            if (!string.IsNullOrWhiteSpace(name))
                rooms = rooms.Where(r => r.RoomName.ToLower().Contains(name.ToLower())).ToList();
            if(!string.IsNullOrWhiteSpace(roomId))
                rooms = rooms.Where(r => r.RoomId == roomId).ToList();
            if (hostId.HasValue && hostId != 0)
                rooms = rooms.Where(r => r.HostId == hostId).ToList();


            rooms.ForEach(r =>
            {
                roomReturns.Add(new RoomResponseModel
                {
                    RoomId = r.RoomId,
                    RoomName = r.RoomName,
                    HostId = r.HostId,
                    CategoryId = r.CategoryId,
                    MaxPlayers = r.MaxPlayers,
                    RoomPasswordHash = r.RoomPasswordHash,
                    JoinCode = r.JoinCode,
                    LastHeartbeat = r.LastHeartbeat,
                    CurrentPlayersConnection = r.CurrentPlayersConnection.ToGeneric(),
                    PlayersIdAssignment = r.PlayersIdAssignment.ToGeneric(),
                    TeamAssignment = r.TeamAssignment.ToGeneric(),
                    Status = r.Status
                });
            });

            return roomReturns;
        }
        private bool? TryGetHostCheckedRoom(string roomId, int userId, out RoomInfo room)
        {
            if (!_roomstore.TryGet(roomId, out room)) return null;
            if (room.HostId != userId) return false;
            return true;
        }
        public bool SetPassword(string roomId, int userId, string password)
        {
            if (TryGetHostCheckedRoom(roomId, userId, out RoomInfo room) != true)
                return false;
            room.RoomPasswordHash = PasswordHasher.Generate(password);
            return true;
        }
        public bool SetName(string roomId, int userId, string name)
        {
            if (TryGetHostCheckedRoom(roomId, userId, out RoomInfo room) != true)
                return false;
            room.RoomName = name;
            return true;
        }
        public bool ChangeHost(string roomId, int userId, int newHostId)
        {
            if (TryGetHostCheckedRoom(roomId, userId, out RoomInfo room) != true)
                return false;
            room.HostId = newHostId;
            return true;
        }
        public bool SetCategory(string roomId, int userId, int categoryId)
        {
            if (TryGetHostCheckedRoom(roomId, userId, out RoomInfo room) != true)
                return false;
            room.CategoryId = categoryId;
            return true;
        }
        public bool SetJoinCode(string roomId, int userId, string joinCode)
        {
            if (TryGetHostCheckedRoom(roomId, userId, out RoomInfo room) != true)
                return false;
            room.JoinCode = joinCode;
            return true;
        }
        public JoinModel? JoinRoom(string roomId, int userId, string password, out string result)
        {
            bool? roomResult = TryGetHostCheckedRoom(roomId, userId, out RoomInfo room);
            if (roomResult == null)
            {
                result = "Room doesn't exist";
                return null;
            }
            if (roomResult == true)
            {
                result = "Host can't join room";
                return null;
            }
            if (string.IsNullOrEmpty(room.RoomPasswordHash) && string.IsNullOrEmpty(password))
            {
                return AddUserToRoom(room, userId, out result);
            }
            if (string.IsNullOrEmpty(password) || !PasswordHasher.Verify(password, room.RoomPasswordHash))
            {
                result = "Incorrect password";
                return null;
            }
            return AddUserToRoom(room, userId, out result);
        }
        private JoinModel AddUserToRoom(RoomInfo room, int userId, out string result)
        {
            if (room.CurrentPlayersConnection.Count < room.MaxPlayers ||
                room.CurrentPlayersConnection.TryGetValue(userId, out _))
            {
                room.CurrentPlayersConnection[userId] = DateTime.UtcNow;
                result = "You have joined room";
                return new JoinModel { JoinCode = room.JoinCode, RoomId = room.RoomId };
            }
            result = "Room is full";
            return null;
        }
        public JoinModel? JoinRoomWithCode(string joinCode, int userId, string password, out string result)
        {
            RoomInfo? room = _roomstore.GetRooms().SingleOrDefault(r => r.JoinCode == joinCode);
            if(room == null)
            {
                result = "Room doesn't exist";
                return null;
            }
            return JoinRoom(room.RoomId, userId, password, out result);
        }
        public bool AssignPlayerId(string roomId, int userId, int unityPlayerId, out string result)
        {
            bool? roomResult = TryGetHostCheckedRoom(roomId, userId, out RoomInfo room);
            if (roomResult == null)
            {
                result = "Room doesn't exist";
                return false;
            }
            room.PlayersIdAssignment[userId] = unityPlayerId;
            result = $"Successfully assigned unity player id {unityPlayerId} to user {userId}";
            return true;
        }
        public bool LeaveRoom(string roomId, int userId)
        {
            if (TryGetHostCheckedRoom(roomId, userId, out RoomInfo room) != false)
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
        public bool HostChangeTeam(string roomId, int hostId, int userId, Teams team, out string result)
        {
            bool? roomStatus = TryGetHostCheckedRoom(roomId, hostId, out RoomInfo room);
            if (roomStatus == null)
            {
                result = $"Unable to find room whith id {roomId}";
                return false;
            }
            if(roomStatus == false)
            {
                result = $"You are not host";
                return false;
            }
            if (!room.CurrentPlayersConnection.TryGetValue(userId, out _))
            {
                result = $"Unable to find player whith id {userId}";
                return false;
            }
            room.TeamAssignment[userId] = team;
            result = "Successfully changed team";
            return true;
        }
        public bool SetGameStatus(string roomId, int userId, GameStatus gameStatus)
        {
            if (TryGetHostCheckedRoom(roomId, userId, out RoomInfo room) != true)
                return false;
            room.Status = gameStatus;
            return true;
        }
        public bool DeleteRoom(string roomId, out string result) => _roomstore.DeleteRoom(roomId, out result);
        public bool DeleteRoom(int hostId, out string result) => _roomstore.DeleteRoom(hostId, out result);
    }
}
