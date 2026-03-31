using GuessItAPI.Rooms;
using Microsoft.EntityFrameworkCore.Query;
using System.Collections.Concurrent;

namespace GuessItAPI.Models
{
    public class RoomResponseModel
    {
        public string RoomId { get; init; } = default!;
        public string RoomName { get; set; } = default!;
        public int HostId { get; set; }
        public int CategoryId { get; set; }
        public int MaxPlayers { get; init; }
        public string RoomPasswordHash { get; set; }
        public string JoinCode { get; set; }
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

        public List<GenericDictionary<int, DateTime>> CurrentPlayersConnection { get; set; }
        public List<GenericDictionary<int, int>> PlayersIdAssignment { get; set; }
        public List<GenericDictionary<int, RoomInfo.Teams>> TeamAssignment { get; set; }

        public RoomInfo.GameStatus Status { get; set; }
    }
    public class GenericDictionary<K, V>
    {
        public K Key { get; set; }
        public V Value { get; set; }
    }
    public static class DictionaryConvertion
    {
        public static List<GenericDictionary<K, V>> ToGeneric<K, V>(this ConcurrentDictionary<K, V> dict)
        {
            var list = new List<GenericDictionary<K, V>>();
            foreach (var kv in dict)
            {
                list.Add(new GenericDictionary<K, V> { Key = kv.Key, Value = kv.Value });
            }
            return list;
        }
        public static List<GenericDictionary<K, V>> ToGeneric<K, V>(this Dictionary<K, V> dict)
        {
            var list = new List<GenericDictionary<K, V>>();
            foreach (var kv in dict)
            {
                list.Add(new GenericDictionary<K, V> { Key = kv.Key, Value = kv.Value });
            }
            return list;
        }
    }
}
