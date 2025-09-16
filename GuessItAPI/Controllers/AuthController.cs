using GuessItAPI.Jwt;
using GuessItAPI.Models;
using GuessItAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace MainShelkonAPI.Controllers
{
    /// <summary>
    /// Контроллер для управления авторизацией и профилем пользователя.
    /// </summary>
    [ApiController, Route("giapi/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;

        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        private void SetToken(string token)
        {
            HttpContext.Response.Cookies.Append("good_cookie", token, new CookieOptions
            {
                SameSite = SameSiteMode.None,
                Secure = true
            });
        }

        /// <summary>
        /// Регистрация нового пользователя.
        /// </summary>
        /// <param name="auth">Данные пользователя для регистрации.</param>
        [HttpPost, Route("register")]
        public async Task<IActionResult> Register(AuthModel auth)
        {
            if (int.TryParse(auth.Username, out int id) || auth.Username.Length > 63 ||
                !_userService.Checker(auth.Username) || auth.Password.IsNullOrEmpty())
                return BadRequest();

            await _userService.Register(auth.Username, auth.Password, "user");
            return Ok();
        }

        /// <summary>
        /// Проверка доступности имени пользователя.
        /// </summary>
        /// <param name="username">Имя пользователя для проверки.</param>
        [HttpPost, Route("checkusername")]
        public IActionResult UsernameChecker(string username)
        {
            if (int.TryParse(username, out int id) || !_userService.Checker(username) || username.Length > 63)
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// Удаление пользователя по имени (только для администраторов).
        /// </summary>
        /// <param name="username">Имя пользователя для удаления.</param>
        [HttpDelete, Route("delete/{username}"), Authorize(Roles = "admin")]
        public async Task<IActionResult> Deleter(string username)
        {
            if (!_userService.Checker(username))
            {
                await _userService.Remover(username);
                if (HttpContext.User.FindFirst(ClaimTypes.Name)!.Value == username)
                    SetToken("");
                return Ok();
            }
            return BadRequest();
        }

        /// <summary>
        /// Удаление текущего пользователя.
        /// </summary>
        [HttpDelete, Route("delete"), Authorize]
        public async Task<IActionResult> Deleter()
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;

            if (!_userService.Checker(username))
            {
                await _userService.Remover(username);
                SetToken("");
                return Ok();
            }
            return BadRequest();
        }

        /// <summary>
        /// Вход в систему.
        /// </summary>
        /// <param name="auth">Данные для авторизации.</param>
        [HttpPost, Route("login")]
        public async Task<IActionResult> Login(AuthModel auth)
        {
            if (!await _userService.CheckAuth(auth.Username, auth.Password))
                return BadRequest();

            string token = await _userService.Login(auth.Username);
            SetToken(token);
            return Ok(token);
        }

        /// <summary>
        /// Вход в систему от имени пользователя(только для администратора).
        /// </summary>
        /// <param name="username">Данные для авторизации.</param>
        [HttpGet, Route("login"), Authorize(Roles = "admin")]
        public async Task<IActionResult> Login(string username)
        {
            string token = await _userService.Login(username);
            SetToken(token);
            return Ok(token);
        }

        /// <summary>
        /// Получение информации о пользователе по имени или ID.
        /// </summary>
        /// <param name="auth">Имя пользователя или ID.</param>
        [HttpGet, Route("getinfo/{auth}")]
        public async Task<IResult?> GetInfo(string auth)
        {
            User? user;
            if (int.TryParse(auth, out int id))
                user = await _userService.GetById(id);
            else
                user = await _userService.GetByUsername(auth);
            if (user == null)
                return null;
            user.PasswordHash = string.Empty;
            return Results.Json(user);

        }

        /// <summary>
        /// Получение информации о текущем пользователе.
        /// </summary>
        [HttpGet, Route("getinfo"), Authorize]
        public async Task<IResult> GetInfo()
        {
            string? username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
                return Results.BadRequest();
            Console.WriteLine(username);
            User? user = await _userService.GetByUsername(username)!;
            if (user == null)
                return Results.BadRequest();
            user.PasswordHash = string.Empty;
            return Results.Json(user);
        }

        /// <summary>
        /// Загрузка аватара для текущего пользователя.
        /// </summary>
        [HttpPut, Route("putavatar"), Authorize, RequestSizeLimit(15_000_000)]
        public async Task<IActionResult> PutAvatar()
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            User? user = await _userService.GetByUsername(username)!;
            if (user == null)
                return BadRequest();

            var file = Request.Form.Files[0];

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);

                await _userService.PutAvatar(user, memoryStream.ToArray());

                return Ok();
            }
        }

        /// <summary>
        /// Получение аватара пользователя по ID.
        /// </summary>
        /// <param name="id">ID пользователя.</param>
        [HttpGet, Route("getavatar/{id}"), Authorize]
        public async Task<IResult> GetAvatar(int id)
        {
            User? user = await _userService.GetById(id)!;
            if (user == null)
                return Results.BadRequest();

            byte[]? userAvatar = await _userService.GetAvatar(user);

            if (userAvatar == null)
                userAvatar = await _userService.GetAvatar(new User { UserId = 0});

            return Results.File(userAvatar!);
        }

        /// <summary>
        /// Получение аватара текущего пользователя.
        /// </summary>
        [HttpGet, Route("getavatar"), Authorize]
        public async Task<IResult> GetAvatar()
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            User? user = await _userService.GetByUsername(username)!;
            if (user == null)
                return Results.BadRequest();

            byte[]? userAvatar = await _userService.GetAvatar(user);

            if (userAvatar == null)
                userAvatar = await _userService.GetAvatar(new User { UserId = 0 });

            return Results.File(userAvatar!);
        }

        /// <summary>
        /// Удаление аватара текущего пользователя.
        /// </summary>
        [HttpDelete, Route("deleteavatar"), Authorize]
        public async Task<IActionResult> DeleteAvatar()
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            User? user = await _userService.GetByUsername(username)!;
            if (user == null)
                return BadRequest();

            await _userService.DeleteAvatar(user);
            return Ok();
        }

        /// <summary>
        /// Смена имени текущего пользователя
        /// </summary>
        /// <param name="newUsername">Новое имя пользователя.</param>
        [HttpPatch, Route("changeusername"), Authorize]
        public async Task<IActionResult> ChangeUsername(string newUsername)
        {

            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            if (!_userService.ChangeUsername(username, newUsername))
                return BadRequest();
            string token = await _userService.Login(newUsername);
            SetToken(token);
            return Ok(token);
        }

        /// <summary>
        /// Изменение пароля пользователя.
        /// </summary>
        /// <param name="oldPassword">Старый пароль пользователя.</param>
        /// <param name="newPassword">Новый пароль пользователя.</param>
        /// <returns>Статус операции. Возвращает <see cref="OkResult"/> при успешном изменении пароля, иначе <see cref="BadRequestResult"/>.</returns>
        [HttpPatch, Route("changepassword"), Authorize]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            if (!await _userService.CheckAuth(username, oldPassword))
                return BadRequest();
            if (await _userService.ChangePassword(username, newPassword))
                return Ok();
            return BadRequest();
        }

        /// <summary>
        /// Проверка корректности указанного пароля для текущего пользователя.
        /// </summary>
        /// <param name="password">Пароль для проверки.</param>
        /// <returns>Возвращает <see cref="OkResult"/>, если пароль корректен, иначе <see cref="BadRequestResult"/>.</returns>
        [HttpGet, Route("checkpassword"), Authorize]
        public async Task<IActionResult> CheckPassword(string password)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest();
            if (PasswordHasher.Verify(password, user.PasswordHash))
                return Ok();
            return BadRequest();
        }

        /// <summary>
        /// Проверка авторизации пользователя. Возвращает успешный статус, если пользователь авторизован.
        /// </summary>
        /// <returns>Статус успешной проверки авторизации.</returns>
        [HttpGet, Route("checkauth"), Authorize]
        public IActionResult CheckAuth() => Ok();

        /// <summary>
        /// Удаление текущей авторизации пользователя.
        /// </summary>
        /// <returns>Статус успешного удаления авторизации.</returns>
        [HttpDelete, Route("deleteauth"), Authorize]
        public IActionResult DeleteAuth()
        {
            SetToken("");
            return Ok();
        }

        /// <summary>
        /// Получение имени текущего пользователя.
        /// </summary>
        /// <returns>Имя текущего пользователя.</returns>
        [HttpGet, Route("getusername"), Authorize]
        public string GetUsername() => HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;

        /// <summary>
        /// Получение роли текущего пользователя.
        /// </summary>
        /// <returns>Роль текущего пользователя.</returns>
        [HttpGet, Route("getrole"), Authorize]
        public string GetRole() => HttpContext.User.FindFirst(ClaimTypes.Role)!.Value;

    }
}