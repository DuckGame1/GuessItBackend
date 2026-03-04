using GuessItAPI.Rooms;
using GuessItAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static GuessItAPI.Rooms.RoomInfo;

namespace GuessItAPI.Controllers
{
    /// <summary>
    /// Управление игровыми комнатами: создание, подключение, настройки комнаты и действия игроков.
    /// </summary>
    [ApiController, Route("room")]
    public class RoomController : ControllerBase
    {
        private readonly RoomService _roomService;
        private readonly UserService _userService;

        public RoomController(RoomService roomService, UserService userService)
        {
            _roomService = roomService;
            _userService = userService;
        }

        /// <summary>
        /// Создаёт новую комнату. Текущий пользователь становится хостом.
        /// </summary>
        /// <param name="roomName">Название комнаты.</param>
        /// <param name="maxPlayers">Максимальное количество игроков в комнате.</param>
        /// <returns>Созданная комната (<see cref="RoomInfo"/>).</returns>
        /// <response code="200">Комната успешно создана.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("createroom"), Authorize]
        public IActionResult CreateRoom(string roomName, int maxPlayers)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            int hostId = _userService.GetByUsername(username).Id;
            RoomInfo room = _roomService.CreateRoom(hostId, roomName, maxPlayers);
            return Ok(room);
        }

        /// <summary>
        /// Heartbeat пользователя в комнате (поддержание соединения/активности).
        /// </summary>
        /// <param name="roomid">ID комнаты.</param>
        /// <returns>OK, если heartbeat принят.</returns>
        /// <response code="200">Heartbeat принят.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpGet, Route("{roomid}/heartbeat"), Authorize]
        public IActionResult HeartBeat(string roomid)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            int hostId = _userService.GetByUsername(username).Id;
            _roomService.SendHeartbeat(roomid, hostId);
            return Ok();
        }

        /// <summary>
        /// Возвращает список комнат с фильтрами.
        /// </summary>
        /// <param name="status">Статус игры/комнаты (например Lobby/InGame/Finished).</param>
        /// <param name="isFull">Фильтр по заполненности (true — только полные, false — только неполные/или как у тебя реализовано).</param>
        /// <param name="name">Фильтр по названию комнаты.</param>
        /// <param name="roomId">Фильтр по ID комнаты.</param>
        /// <returns>Список комнат, удовлетворяющий фильтрам.</returns>
        /// <response code="200">Список комнат успешно получен.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpGet, Route("GetRooms"), Authorize]
        public IActionResult GetRooms(GameStatus status, bool isFull, string name, string roomId)
        {
            return Ok(_roomService.GetRooms(status, isFull, name, roomId));
        }

        /// <summary>
        /// Устанавливает пароль комнаты (только хост).
        /// </summary>
        /// <param name="roomId">ID комнаты.</param>
        /// <param name="newPassword">Новый пароль комнаты.</param>
        /// <returns>OK если пароль установлен.</returns>
        /// <response code="200">Пароль установлен.</response>
        /// <response code="400">Текущий пользователь не является хостом.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("SetPassword"), Authorize]
        public IActionResult SetPassword(string roomId, string newPassword)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            int hostId = _userService.GetByUsername(username).Id;
            bool status = _roomService.SetPassword(roomId, hostId, newPassword);
            if (status)
                return Ok();
            return BadRequest("User is not host");
        }

        /// <summary>
        /// Изменяет название комнаты (только хост).
        /// </summary>
        /// <param name="roomId">ID комнаты.</param>
        /// <param name="name">Новое название комнаты.</param>
        /// <returns>OK если название изменено.</returns>
        /// <response code="200">Название изменено.</response>
        /// <response code="400">Текущий пользователь не является хостом.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("SetName"), Authorize]
        public IActionResult SetName(string roomId, string name)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            int hostId = _userService.GetByUsername(username).Id;
            bool status = _roomService.SetName(roomId, hostId, name);
            if (status)
                return Ok();
            return BadRequest("User is not host");
        }

        /// <summary>
        /// Передаёт права хоста другому игроку (только текущий хост).
        /// </summary>
        /// <param name="roomId">ID комнаты.</param>
        /// <param name="newHostId">ID пользователя, которому передаются права хоста.</param>
        /// <returns>OK если хост успешно изменён.</returns>
        /// <response code="200">Хост изменён.</response>
        /// <response code="400">Текущий пользователь не является хостом.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("ChangeHost"), Authorize]
        public IActionResult ChangeHost(string roomId, int newHostId)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            int hostId = _userService.GetByUsername(username).Id;
            bool status = _roomService.ChangeHost(roomId, hostId, newHostId);
            if (status)
                return Ok();
            return BadRequest("User is not host");
        }

        /// <summary>
        /// Устанавливает категорию/набор вопросов комнаты (только хост).
        /// </summary>
        /// <param name="roomId">ID комнаты.</param>
        /// <param name="categoryId">ID категории.</param>
        /// <returns>OK если категория установлена.</returns>
        /// <response code="200">Категория установлена.</response>
        /// <response code="400">Текущий пользователь не является хостом.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("SetCategory"), Authorize]
        public IActionResult SetCategory(string roomId, int categoryId)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            int hostId = _userService.GetByUsername(username).Id;
            bool status = _roomService.SetCategory(roomId, hostId, categoryId);
            if (status)
                return Ok();
            return BadRequest("User is not host");
        }

        /// <summary>
        /// Устанавливает код присоединения к комнате (только хост).
        /// </summary>
        /// <param name="roomId">ID комнаты.</param>
        /// <param name="joinCode">Новый код присоединения.</param>
        /// <returns>OK если код изменён.</returns>
        /// <response code="200">Код присоединения изменён.</response>
        /// <response code="400">Текущий пользователь не является хостом.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("SetJoinCode"), Authorize]
        public IActionResult SetJoinCode(string roomId, string joinCode)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            int hostId = _userService.GetByUsername(username).Id;
            bool status = _roomService.SetJoinCode(roomId, hostId, joinCode);
            if (status)
                return Ok();
            return BadRequest("User is not host");
        }

        /// <summary>
        /// Выход текущего пользователя из комнаты.
        /// </summary>
        /// <param name="roomId">ID комнаты.</param>
        /// <returns>OK если пользователь вышел из комнаты.</returns>
        /// <response code="200">Пользователь вышел из комнаты.</response>
        /// <response code="400">Невозможно выйти из комнаты (например, комнаты нет или пользователь не в комнате).</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("LeaveRoom"), Authorize]
        public IActionResult LeaveRoom(string roomId)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            int userId = _userService.GetByUsername(username).Id;
            bool status = _roomService.LeaveRoom(roomId, userId);
            if (status)
                return Ok();
            return BadRequest("Unable to leave room");
        }

        /// <summary>
        /// Меняет команду текущего пользователя в комнате.
        /// </summary>
        /// <param name="roomId">ID комнаты.</param>
        /// <param name="team">Команда, в которую переходит пользователь.</param>
        /// <returns>OK если команда изменена.</returns>
        /// <response code="200">Команда изменена.</response>
        /// <response code="400">Невозможно изменить команду (например, пользователь не в комнате).</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("ChangeTeam"), Authorize]
        public IActionResult ChangeTeam(string roomId, Teams team)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            int userId = _userService.GetByUsername(username).Id;
            bool status = _roomService.ChangeTeam(roomId, userId, team);
            if (status)
                return Ok();
            return BadRequest("Unable to change team");
        }

        /// <summary>
        /// Устанавливает статус игры/комнаты (обычно только хост).
        /// </summary>
        /// <param name="roomId">ID комнаты.</param>
        /// <param name="gameStatus">Новый статус игры.</param>
        /// <returns>OK если статус изменён.</returns>
        /// <response code="200">Статус игры изменён.</response>
        /// <response code="400">Текущий пользователь не является хостом.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("SetGameStatus"), Authorize]
        public IActionResult SetGameStatus(string roomId, GameStatus gameStatus)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            int userId = _userService.GetByUsername(username).Id;
            bool status = _roomService.SetGameStatus(roomId, userId, gameStatus);
            if (status)
                return Ok();
            return BadRequest("User is not host");
        }

        /// <summary>
        /// Присоединяет текущего пользователя к комнате по ID и паролю.
        /// </summary>
        /// <param name="roomId">ID комнаты.</param>
        /// <param name="password">Пароль комнаты (если установлен).</param>
        /// <returns>Кортеж (roomId, joinCode) при успешном входе.</returns>
        /// <response code="200">Пользователь присоединился к комнате.</response>
        /// <response code="400">Комната заполнена, не существует или пароль неверный.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("JoinRoom"), Authorize]
        public IActionResult JoinRoom(string roomId, string password)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            int userId = _userService.GetByUsername(username).Id;
            (string, string) roomIdAndCode = _roomService.JoinRoom(roomId, userId, password);
            if (string.IsNullOrWhiteSpace(roomIdAndCode.Item1) || string.IsNullOrWhiteSpace(roomIdAndCode.Item2))
                return BadRequest("Room full or doesnt exist");
            return Ok(roomIdAndCode);
        }
    }
}