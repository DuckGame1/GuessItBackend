using GuessItAPI.Models;
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
        /// <param name="roomName">Название комнаты (до 63 символов).</param>
        /// <param name="maxPlayers">Максимальное количество игроков в комнате.</param>
        /// <returns>Созданная комната.</returns>
        /// <response code="200">Комната успешно создана.</response>
        /// <response code="400">Некорректные данные или пользователь не найден.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("createroom"), Authorize]
        public async Task<IActionResult> CreateRoom(string roomName, int maxPlayers)
        {
            if (roomName.Length > 63)
                return BadRequest("Too long name");

            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            RoomInfo room = _roomService.CreateRoom(user.UserId, roomName, maxPlayers);
            return Ok(room);
        }

        /// <summary>
        /// Отправляет heartbeat текущего пользователя в комнате (поддержание активности).
        /// </summary>
        /// <param name="roomid">ID комнаты.</param>
        /// <returns>OK, если heartbeat принят.</returns>
        /// <response code="200">Heartbeat принят.</response>
        /// <response code="400">Пользователь не найден.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpGet, Route("{roomid}/heartbeat"), Authorize]
        public async Task<IActionResult> HeartBeat(string roomid)
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            _roomService.SendHeartbeat(roomid, user.UserId);
            return Ok();
        }

        /// <summary>
        /// Возвращает список комнат с фильтрами.
        /// </summary>
        /// <param name="status">Фильтр по статусу игры/комнаты (необязательно).</param>
        /// <param name="isFull">Фильтр по заполненности (необязательно).</param>
        /// <param name="name">Фильтр по названию комнаты (необязательно).</param>
        /// <param name="roomId">Фильтр по ID комнаты (необязательно).</param>
        /// <returns>Список комнат, удовлетворяющий фильтрам.</returns>
        /// <response code="200">Список комнат успешно получен.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpGet, Route("getrooms"), Authorize]
        public IActionResult GetRooms(GameStatus? status = null, bool? isFull = null, string? name = null, string? roomId = null)
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
        /// <response code="400">Пользователь не найден или пользователь не является хостом.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("setpassword"), Authorize]
        public async Task<IActionResult> SetPassword(string roomId, string newPassword)
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            bool status = _roomService.SetPassword(roomId, user.UserId, newPassword);
            if (status)
                return Ok();

            return BadRequest("User is not host");
        }

        /// <summary>
        /// Изменяет название комнаты (только хост).
        /// </summary>
        /// <param name="roomId">ID комнаты.</param>
        /// <param name="name">Новое название комнаты (до 63 символов).</param>
        /// <returns>OK если название изменено.</returns>
        /// <response code="200">Название изменено.</response>
        /// <response code="400">Некорректные данные, пользователь не найден или пользователь не является хостом.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("setname"), Authorize]
        public async Task<IActionResult> SetName(string roomId, string name)
        {
            if (name.Length > 63)
                return BadRequest("Too long name");

            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            bool status = _roomService.SetName(roomId, user.UserId, name);
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
        /// <response code="400">Пользователь не найден или пользователь не является хостом.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("changehost"), Authorize]
        public async Task<IActionResult> ChangeHost(string roomId, int newHostId)
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            bool status = _roomService.ChangeHost(roomId, user.UserId, newHostId);
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
        /// <response code="400">Пользователь не найден или пользователь не является хостом.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("setcategory"), Authorize]
        public async Task<IActionResult> SetCategory(string roomId, int categoryId)
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            bool status = _roomService.SetCategory(roomId, user.UserId, categoryId);
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
        /// <response code="400">Пользователь не найден или пользователь не является хостом.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("setjoincode"), Authorize]
        public async Task<IActionResult> SetJoinCode(string roomId, string joinCode)
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            bool status = _roomService.SetJoinCode(roomId, user.UserId, joinCode);
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
        /// <response code="400">Пользователь не найден или операция невозможна.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("leaveroom"), Authorize]
        public async Task<IActionResult> LeaveRoom(string roomId)
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            bool status = _roomService.LeaveRoom(roomId, user.UserId);
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
        /// <response code="400">Пользователь не найден или операция невозможна.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("changeteam"), Authorize]
        public async Task<IActionResult> ChangeTeam(string roomId, Teams team)
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            bool status = _roomService.ChangeTeam(roomId, user.UserId, team);
            if (status)
                return Ok();

            return BadRequest("Unable to change team");
        }

        /// <summary>
        /// Меняет команду пользователя в комнате.
        /// </summary>
        /// <param name="roomId">ID комнаты.</param>
        /// <param name="userId">Пользователь, который переходит в команду.</param>
        /// <param name="team">Команда, в которую переходит пользователь.</param>
        /// <returns>OK если команда изменена.</returns>
        /// <response code="200">Команда изменена.</response>
        /// <response code="400">Пользователь не найден или операция невозможна.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("hostchangeteam"), Authorize]
        public IActionResult HostChangeTeam(string roomId, int userId, Teams team)
        {
            bool status = _roomService.HostChangeTeam(roomId, userId, team, out string result);
            if (status)
                return Ok(result);
            return BadRequest(result);
        }

        /// <summary>
        /// Устанавливает статус игры/комнаты (обычно только хост).
        /// </summary>
        /// <param name="roomId">ID комнаты.</param>
        /// <param name="gameStatus">Новый статус игры.</param>
        /// <returns>OK если статус изменён.</returns>
        /// <response code="200">Статус игры изменён.</response>
        /// <response code="400">Пользователь не найден или пользователь не является хостом.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("setgamestatus"), Authorize]
        public async Task<IActionResult> SetGameStatus(string roomId, GameStatus gameStatus)
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            bool status = _roomService.SetGameStatus(roomId, user.UserId, gameStatus);
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
        /// <response code="400">Пользователь не найден, комната заполнена/не существует или пароль неверный.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("joinroom"), Authorize]
        public async Task<IActionResult> JoinRoom(string roomId, string password = "")
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            JoinModel? joinModel = _roomService.JoinRoom(roomId, user.UserId, password, out string result);
            if (joinModel == null)
                return BadRequest(result);
            return Ok(joinModel);
        }
        /// <summary>
        /// Присоединяет текущего пользователя к комнате по коду подключения (joinCode) и паролю.
        /// </summary>
        /// <param name="joinCode">Код подключения к комнате (join code), выданный хостом/Relay.</param>
        /// <param name="password">Пароль комнаты (если установлен). По умолчанию пустая строка.</param>
        /// <returns>
        /// Модель входа (<see cref="JoinModel"/>) при успешном подключении.
        /// </returns>
        /// <response code="200">Пользователь успешно присоединился к комнате.</response>
        /// <response code="400">Пользователь не найден, код неверный, пароль неверный, комната не найдена/заполнена или вход невозможен (см. текст ошибки).</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost, Route("joinroomwithcode"), Authorize]
        public async Task<IActionResult> JoinRoomWithCode(string joinCode, string password = "")
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            JoinModel? joinModel = _roomService.JoinRoomWithCode(joinCode, user.UserId, password, out string result);
            if (joinModel == null)
                return BadRequest(result);

            return Ok(joinModel);
        }

        /// <summary>
        /// Привязывает Unity PlayerId (unityPlayerId) к текущему пользователю в указанной комнате.
        /// </summary>
        /// <remarks>
        /// Используется для сопоставления пользователя из API (UserId) с идентификатором игрока внутри Unity-сессии.
        /// Текущий пользователь определяется по JWT/claims.
        /// </remarks>
        /// <param name="roomId">ID комнаты, в которой нужно назначить PlayerId.</param>
        /// <param name="unityPlayerId">Идентификатор игрока в Unity (например, локальный ID/слот игрока).</param>
        /// <returns>Строковый результат/лог операции.</returns>
        /// <response code="200">PlayerId успешно назначен — возвращается текст результата.</response>
        /// <response code="400">Невозможно назначить PlayerId (например, комнаты нет, пользователь не в комнате, PlayerId занят и т.п.) — возвращается текст ошибки.</response>
        /// <response code="401">Пользователь не авторизован или не найден (claims/пользователь отсутствует).</response>
        [HttpPost, Route("assignplayerid"), Authorize]
        public async Task<IActionResult> AssignPlayerId(string roomId, ulong unityPlayerId)
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return Unauthorized("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return Unauthorized("User not found");

            bool status = _roomService.AssignPlayerId(roomId, user.UserId, unityPlayerId, out string result);
            if (!status)
                return BadRequest(result);

            return Ok(result);
        }
        /// <summary>
        /// Удаляет комнату по ID (доступно только администратору).
        /// </summary>
        /// <param name="roomId">ID комнаты, которую нужно удалить.</param>
        /// <returns>Текстовый лог результата операции.</returns>
        /// <response code="200">Комната удалена (или операция завершилась успешно) — возвращается лог.</response>
        /// <response code="400">Ошибка удаления / комната не найдена / операция не выполнена — возвращается лог.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="403">Недостаточно прав (требуется роль Admin).</response>
        [HttpDelete, Route("deleteroom"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoom(string roomId)
        {
            bool result = _roomService.DeleteRoom(roomId, out string log);
            if (result) return Ok(log);
            return BadRequest(log);
        }

        /// <summary>
        /// Удаляет комнату, в которой текущий пользователь является хостом/владельцем.
        /// </summary>
        /// <remarks>
        /// Метод определяет текущего пользователя по JWT/claims и удаляет комнату, привязанную к его UserId.
        /// </remarks>
        /// <returns>Текстовый лог результата операции.</returns>
        /// <response code="200">Комната пользователя удалена — возвращается лог.</response>
        /// <response code="400">Пользователь не найден, пользователь не хост/нет комнаты или удаление не удалось — возвращается лог/сообщение.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpDelete, Route("deletemyroom"), Authorize]
        public async Task<IActionResult> DeleteMyRoom()
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("User not found");

            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest("User not found");

            bool result = _roomService.DeleteRoom(user.UserId, out string log);
            if (result) return Ok(log);
            return BadRequest(log);
        }
    }
}